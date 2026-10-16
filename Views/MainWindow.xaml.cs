using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MementoMoriCodeRedeemer.Wpf.Models;
using MementoMoriCodeRedeemer.Wpf.Services;

namespace MementoMoriCodeRedeemer.Wpf.Views;

/// <summary>
/// 程式主視窗。
/// 這個類負責把介面操作串起來：維護玩家 ID 列表、讀取伺服器、呼叫兌換介面、更新進度與日誌。
/// 真正的網路訪問和本地檔案讀寫分別交給 <see cref="MementoMoriRedeemClient"/> 與 <see cref="PlayerRepository"/>。
///
/// 與舊版 WinForms 的對應關係：
///   · 自繪的 TransparentDataGridView 換成 DataGrid + 自定義行模板，選中效果依舊用水彩筆痕素材；
///   · BindingList 換成 ObservableCollection，繫結直接驅動介面重新整理；
///   · 系統 MessageBox 換成同一套皮膚的 MessageDialog；
///   · 原版右下角的 _statusLabel 整塊去掉：它顯示的每一條資訊在「執行日誌」裡都有對應記錄，
///     領取過程中還有進度條、行內「最近結果」實時同步，重複顯示反而讓介面更雜；
///   · 其餘業務流程、校驗規則、介面呼叫與儲存策略保持完全一致。
/// </summary>
public partial class MainWindow : Window
{
    // 點選 Logo/按鈕時開啟的官方虛寶碼頁面。
    private const string OfficialCodePageUrl = "https://mememori-game.com/tw/code/";

    // 玩家 ID 官方只接受數字；虛寶碼通常由半形英數字組成。
    private static readonly Regex SerialCodePattern = new("^[0-9a-zA-Z]+$", RegexOptions.Compiled);

    // 日誌緩衝上限：超過後只保留末尾部分，避免長時間執行後記憶體和重繪成本失控。
    private const int LogTrimThreshold = 200_000;
    private const int LogTrimKeep = 150_000;

    // DataGrid 直接繫結這個集合；PlayerRow 實現屬性變更通知，修改後單元格會自動重新整理。
    private readonly ObservableCollection<PlayerRow> _players = [];

    // 搜尋框使用的檢視。過濾隻影響顯示，不影響玩家庫裡實際儲存的資料。
    private readonly ICollectionView _playersView;

    private readonly PlayerRepository _playerRepository = new();
    private readonly MementoMoriRedeemClient _redeemClient = new();
    private readonly AppSettingsStore _appSettingsStore = new();
    private readonly PortraitLibrary _portraitLibrary = new();

    // 當前批次兌換任務的取消源。為空表示沒有任務在執行。
    private CancellationTokenSource? _redeemCancellation;

    // _busy 用來統一鎖定介面，防止兌換進行中修改伺服器、虛寶碼或玩家庫。
    private bool _busy;

    // 載入玩家庫時會批次填充集合；這個標記避免載入過程觸發儲存，造成不必要寫盤。
    private bool _loadingPlayers;

    // 視窗正在關閉時不再往日誌裡寫字，也不做介面更新。
    private bool _isClosing;

    private int _logLineCount;
    private int _logCharCount;

    public MainWindow()
    {
        InitializeComponent();
        ApplySavedWindowPlacement();

        _playersView = CollectionViewSource.GetDefaultView(_players);
        _playersView.Filter = MatchesSearch;
        PlayersGrid.ItemsSource = _playersView;
        _players.CollectionChanged += Players_CollectionChanged;

        LoadPlayers();
        UpdateStatistics();
        UpdateLogSummary();
        ShowRandomPortrait();

        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// 視窗首次顯示後再載入伺服器列表。
    /// 先把介面畫出來再發網路請求，啟動體驗更順。
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PlayersGrid.SelectedItem = null;
        RefreshEditorState();
        await LoadServersAsync();
    }

    /// <summary>
    /// 使用者關閉視窗時，如果批次兌換還在進行，先發出取消訊號；同時記住視窗尺寸。
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        _isClosing = true;
        _redeemCancellation?.Cancel();
        SaveWindowPlacement();
        base.OnClosing(e);
    }

    /// <summary>
    /// 視窗關閉後的資源清理。HttpClient 持有連線與定時器，需要顯式釋放。
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _redeemClient.Dispose();
        base.OnClosed(e);
    }

    // ==================================================================
    // 視窗尺寸記憶
    // ==================================================================

    /// <summary>
    /// 恢復上次關閉時儲存的視窗尺寸。
    /// 只恢復大小（以及是否最大化），位置仍按 WindowStartupLocation 居中，
    /// 避免上次的視窗座標落在已經拔掉的顯示器上找不到視窗。
    /// </summary>
    private void ApplySavedWindowPlacement()
    {
        var placement = _appSettingsStore.Load();
        if (placement is null)
        {
            return;
        }

        // 尺寸夾在「最小尺寸」與「當前螢幕工作區」之間：
        // 上次在大螢幕上拉得很大時，換到小螢幕上也能完整顯示。
        var workArea = SystemParameters.WorkArea;
        Width = Math.Clamp(placement.Width, MinWidth, Math.Max(MinWidth, workArea.Width));
        Height = Math.Clamp(placement.Height, MinHeight, Math.Max(MinHeight, workArea.Height));

        if (placement.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// 記住當前視窗尺寸，供下次啟動恢復。
    /// 最大化／最小化時取 RestoreBounds（還原後的大小），
    /// 這樣最大化的使用者下次開啟直接恢復最大化，取消最大化後的尺寸也沒有丟。
    /// </summary>
    private void SaveWindowPlacement()
    {
        var size = WindowState == WindowState.Normal
            ? new Size(ActualWidth, ActualHeight)
            : RestoreBounds.Size;

        if (double.IsNaN(size.Width) || double.IsNaN(size.Height) || size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        _appSettingsStore.Save(new WindowPlacement(size.Width, size.Height, WindowState == WindowState.Maximized));
    }

    // ==================================================================
    // 統計
    // ==================================================================

    /// <summary>
    /// 重新整理玩家庫卡頭部的計數徽章（總數 / 已勾選 / 已鎖定）。
    /// 這三個數字只在列表標題旁顯示一處，避免同一組數字在介面裡重複出現。
    /// </summary>
    private void UpdateStatistics()
    {
        var total = _players.Count;
        var selected = _players.Count(player => player.Selected);
        var locked = _players.Count(player => player.Locked);

        PlayerCountText.Text = total == 0
            ? "還沒有玩家ID"
            : BuildCountSummary(total, selected, locked);
    }

    /// <summary>
    /// 組裝列表標題旁的統計標籤。有搜尋條件時額外標出「篩選後還有幾位」，
    /// 避免出現「寫著 13 位、列表裡只有 5 行」的困惑。
    /// </summary>
    private string BuildCountSummary(int total, int selected, int locked)
    {
        var suffix = $"已勾選 {selected} · 已鎖定 {locked}";

        if (_playersView is null || string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            return $"共 {total} 位 · {suffix}";
        }

        var visible = _playersView.Cast<object>().Count();
        return $"篩選 {visible} / 共 {total} 位 · {suffix}";
    }

    private void PlayerIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshEditorState();
    }

    // ==================================================================
    // 伺服器列表
    // ==================================================================

    private async void RefreshServersButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadServersAsync();
    }

    /// <summary>
    /// 從官方 API載入伺服器列表並重新整理下拉框。
    /// 成功後優先選擇 Asia 或 ID 為 3 的伺服器；找不到時選擇第一項，保證使用者可以直接開始輸入虛寶碼。
    /// </summary>
    private async Task LoadServersAsync()
    {
        RefreshServersButton.IsEnabled = false;
        ServerComboBox.IsEnabled = false;

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var servers = await _redeemClient.GetServersAsync(cancellation.Token);

            var previous = (ServerComboBox.SelectedItem as ServerOption)?.Id;
            ServerComboBox.ItemsSource = servers;

            if (servers.Count > 0)
            {
                // 優先沿用使用者上次選的伺服器，其次是 Asia / ID 3，最後退回第一項。
                var preferred = servers.FirstOrDefault(server => server.Id == previous)
                    ?? servers.FirstOrDefault(server => server.Id == 3
                        || server.Name.Contains("Asia", StringComparison.OrdinalIgnoreCase));

                ServerComboBox.SelectedItem = preferred ?? servers[0];
            }

            AppendLog($"[伺服器] 已載入 {servers.Count} 個伺服器。");
        }
        catch (Exception ex)
        {
            AppendLog("[錯誤] 伺服器列表讀取失敗：" + ex.Message);
            MessageDialog.ShowError(this, "伺服器列表讀取失敗", ex.Message);
        }
        finally
        {
            ServerComboBox.IsEnabled = true;
            RefreshServersButton.IsEnabled = true;
        }
    }

    // ==================================================================
    // 玩家庫
    // ==================================================================

    /// <summary>
    /// 從本地玩家庫載入玩家列表到介面。
    /// 空 ID 會被跳過；匯入後預設 Selected=true，方便使用者一開啟程式就能直接批次領取。
    /// </summary>
    private void LoadPlayers()
    {
        _loadingPlayers = true;
        try
        {
            _players.Clear();
            foreach (var player in _playerRepository.Load())
            {
                if (string.IsNullOrWhiteSpace(player.PlayerId))
                {
                    continue;
                }

                _players.Add(new PlayerRow
                {
                    Locked = player.Locked,
                    Selected = true,
                    PlayerId = player.PlayerId,
                    Note = player.Note,
                });
            }

            AppendLog($"[玩家庫] 已載入 {_players.Count} 個玩家ID。");
        }
        catch (Exception ex)
        {
            AppendLog("[錯誤] " + ex.Message);
            MessageDialog.ShowError(this, "玩家庫讀取失敗", ex.Message);
        }
        finally
        {
            _loadingPlayers = false;
        }
    }

    /// <summary>
    /// 安全儲存玩家庫。這個方法吞掉並展示儲存異常，避免單次儲存失敗導致整個程式崩潰。
    /// </summary>
    private void SavePlayersSafe()
    {
        if (_loadingPlayers)
        {
            return;
        }

        try
        {
            _playerRepository.Save(_players
                .Where(player => !string.IsNullOrWhiteSpace(player.PlayerId))
                .Select(player => new PlayerProfile
                {
                    Locked = player.Locked,
                    PlayerId = player.PlayerId,
                    Note = player.Note,
                }));
        }
        catch (Exception ex)
        {
            AppendLog("[錯誤] " + ex.Message);
            MessageDialog.ShowError(this, "玩家庫儲存失敗", ex.Message);
        }
    }

    /// <summary>
    /// 新增玩家：校驗 ID 格式與重複後加入玩家庫並立即儲存。
    /// </summary>
    private void AddPlayerButton_Click(object sender, RoutedEventArgs e)
    {
        var playerId = PlayerIdTextBox.Text.Trim();
        if (!ValidatePlayerIdForEditor(playerId))
        {
            return;
        }

        if (_players.Any(player => string.Equals(player.PlayerId, playerId, StringComparison.Ordinal)))
        {
            MessageDialog.ShowInfo(this, "重複ID", "這個玩家ID已經在內容庫裡。");
            return;
        }

        // 先把搜尋條件清掉，否則新增的行可能被過濾掉，使用者會以為沒加成功。
        ClearSearch();

        var row = new PlayerRow
        {
            Selected = true,
            PlayerId = playerId,
            Note = NoteTextBox.Text,
        };

        _players.Add(row);
        SavePlayersSafe();
        SelectRow(row);
        AppendLog($"[玩家庫] 已新增 {playerId}。");
    }

    /// <summary>
    /// 修改當前玩家：鎖定行不允許修改，避免使用者把重要 ID 誤覆蓋。
    /// </summary>
    private void UpdatePlayerButton_Click(object sender, RoutedEventArgs e)
    {
        var row = GetCurrentRow();
        if (row is null)
        {
            MessageDialog.ShowInfo(this, "未選擇", "請先選擇一個玩家ID。");
            return;
        }

        if (row.Locked)
        {
            MessageDialog.ShowInfo(this, "已鎖定", "該行已鎖定，切換為解鎖後才能修改。");
            return;
        }

        var playerId = PlayerIdTextBox.Text.Trim();
        if (!ValidatePlayerIdForEditor(playerId))
        {
            return;
        }

        if (_players.Any(player => !ReferenceEquals(player, row)
            && string.Equals(player.PlayerId, playerId, StringComparison.Ordinal)))
        {
            MessageDialog.ShowInfo(this, "重複ID", "這個玩家ID已經在內容庫裡。");
            return;
        }

        row.PlayerId = playerId;
        row.Note = NoteTextBox.Text;
        SavePlayersSafe();
        AppendLog($"[玩家庫] 已修改 {playerId}。");
    }

    /// <summary>
    /// 刪除當前玩家：刪除前二次確認；鎖定行同樣不允許刪除。
    /// </summary>
    private void DeletePlayerButton_Click(object sender, RoutedEventArgs e)
    {
        var row = GetCurrentRow();
        if (row is null)
        {
            MessageDialog.ShowInfo(this, "未選擇", "請先選擇一個玩家ID。");
            return;
        }

        if (row.Locked)
        {
            MessageDialog.ShowInfo(this, "已鎖定", "該行已鎖定，切換為解鎖後才能刪除。");
            return;
        }

        if (!MessageDialog.Confirm(this, "確認刪除", $"刪除玩家ID {row.PlayerId}？此操作會立即寫入本地玩家庫。"))
        {
            return;
        }

        _players.Remove(row);
        SavePlayersSafe();
        AppendLog($"[玩家庫] 已刪除 {row.PlayerId}。");
    }

    /// <summary>
    /// 表格裡的鎖定圖示：點選切換該行鎖定狀態，並立即儲存。
    /// </summary>
    private void ToggleLock_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is not PlayerRow row)
        {
            return;
        }

        row.Locked = !row.Locked;
        SavePlayersSafe();
        RefreshEditorState();
    }

    /// <summary>
    /// 表格裡的選擇圖示：點選切換該行是否參與「領取勾選」。
    /// </summary>
    private void ToggleSelect_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is not PlayerRow row)
        {
            return;
        }

        row.Selected = !row.Selected;
    }

    /// <summary>
    /// 選中行變化時，把右側編輯區同步為當前行內容。
    /// </summary>
    private void PlayersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FillEditorFromCurrentRow();
        RefreshEditorState();
    }

    /// <summary>
    /// 雙擊資料行直接把焦點丟到玩家ID輸入框，方便快速修改。
    /// 點在行內圖示按鈕上時不處理，避免搶掉按鈕的點選。
    /// </summary>
    private void PlayersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_busy || FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (PlayersGrid.SelectedItem is PlayerRow)
        {
            PlayerIdTextBox.Focus();
            PlayerIdTextBox.SelectAll();
        }
    }

    /// <summary>
    /// 用當前選中行填充右側編輯區。沒有選中行時清空編輯區。
    /// </summary>
    private void FillEditorFromCurrentRow()
    {
        var row = GetCurrentRow();
        if (row is null)
        {
            PlayerIdTextBox.Clear();
            NoteTextBox.Clear();
        }
        else
        {
            PlayerIdTextBox.Text = row.PlayerId;
            NoteTextBox.Text = row.Note;
        }

        RefreshEditorState();
    }

    /// <summary>獲取當前選中的玩家行。</summary>
    private PlayerRow? GetCurrentRow()
    {
        return PlayersGrid.SelectedItem as PlayerRow;
    }

    /// <summary>
    /// 在表格中定位到指定玩家行。新增玩家後呼叫它，讓編輯區指向剛新增的資料。
    /// </summary>
    private void SelectRow(PlayerRow row)
    {
        if (!_playersView.Contains(row))
        {
            return;
        }

        PlayersGrid.SelectedItem = row;
        PlayersGrid.ScrollIntoView(row);
        FillEditorFromCurrentRow();
    }

    /// <summary>
    /// 重新整理右側編輯按鈕狀態。只有程式不忙且當前行未鎖定時，才允許修改或刪除。
    /// </summary>
    private void RefreshEditorState()
    {
        var canModifyCurrentRow = !_busy && GetCurrentRow() is { Locked: false };
        UpdatePlayerButton.IsEnabled = canModifyCurrentRow;
        DeletePlayerButton.IsEnabled = canModifyCurrentRow;
    }

    /// <summary>
    /// 校驗編輯區輸入的玩家 ID。只檢查必填與數字格式，重複檢查由新增/修改各自完成。
    /// </summary>
    private bool ValidatePlayerIdForEditor(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || !PlayerRow.PlayerIdPattern.IsMatch(playerId))
        {
            MessageDialog.ShowWarning(this, "引數錯誤", "玩家ID只能輸入半形數字。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 集合增刪時同步訂閱新行的屬性變更，並重新整理統計。
    /// </summary>
    private void Players_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PlayerRow row in e.OldItems)
            {
                row.PropertyChanged -= Player_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (PlayerRow row in e.NewItems)
            {
                row.PropertyChanged += Player_PropertyChanged;
            }
        }

        UpdateStatistics();
    }

    /// <summary>
    /// 行內任意影響統計的屬性變化都重新整理一次頂部計數。
    /// </summary>
    private void Player_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlayerRow.Selected)
            or nameof(PlayerRow.Locked)
            or nameof(PlayerRow.PlayerId)
            or nameof(PlayerRow.Note))
        {
            UpdateStatistics();
        }
    }

    // ==================================================================
    // 搜尋過濾
    // ==================================================================

    /// <summary>
    /// 搜尋框過濾條件：玩家ID、備註、最近結果任一命中即保留。
    /// 過濾隻影響列表顯示；「領取全部」依然處理整個玩家庫。
    /// </summary>
    private bool MatchesSearch(object item)
    {
        if (item is not PlayerRow row)
        {
            return false;
        }

        var query = SearchBox?.Text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return true;
        }

        return row.PlayerId.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Note.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.LastResult.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_playersView is null)
        {
            return;
        }

        _playersView.Refresh();
        UpdateStatistics();
    }

    private void ClearSearch()
    {
        if (!string.IsNullOrEmpty(SearchBox.Text))
        {
            SearchBox.Clear();
        }
    }

    // ==================================================================
    // 剪貼簿匯入匯出
    // ==================================================================

    /// <summary>
    /// 從剪貼簿匯入玩家 ID。
    /// 支援「ID + 備註」的表格/CSV 風格文字，也支援純 ID 列表；無效或重複 ID 會被跳過。
    /// </summary>
    private void ImportClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        string text;
        try
        {
            text = Clipboard.GetText();
        }
        catch (Exception ex)
        {
            MessageDialog.ShowWarning(this, "讀取剪貼簿失敗", ex.Message);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            MessageDialog.ShowInfo(this, "剪貼簿為空", "剪貼簿裡沒有可匯入的玩家ID。");
            return;
        }

        // 匯入前清空搜尋，保證新增的行一定可見。
        ClearSearch();

        var added = 0;
        var skipped = 0;
        foreach (var item in ParseClipboardPlayers(text))
        {
            if (!PlayerRow.PlayerIdPattern.IsMatch(item.PlayerId)
                || _players.Any(player => string.Equals(player.PlayerId, item.PlayerId, StringComparison.Ordinal)))
            {
                skipped++;
                continue;
            }

            _players.Add(new PlayerRow
            {
                Selected = true,
                PlayerId = item.PlayerId,
                Note = item.Note,
            });
            added++;
        }

        SavePlayersSafe();
        AppendLog($"[玩家庫] 剪貼簿匯入完成：新增 {added}，跳過 {skipped}。");

        if (added == 0)
        {
            MessageDialog.ShowInfo(this, "沒有新增",
                $"剪貼簿裡的 {skipped} 條內容全部被跳過：可能是格式不正確，或玩家ID已經存在。");
        }
    }

    /// <summary>
    /// 把當前玩家庫複製到剪貼簿。有備註時使用 Tab 分隔，方便貼上到表格軟體或再次匯入。
    /// </summary>
    private void ExportClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        var targets = _playersView.Cast<PlayerRow>().ToList();
        if (targets.Count == 0)
        {
            MessageDialog.ShowInfo(this, "沒有內容", "當前列表裡沒有可複製的玩家ID。");
            return;
        }

        var text = string.Join(Environment.NewLine, targets.Select(player =>
            string.IsNullOrWhiteSpace(player.Note)
                ? player.PlayerId
                : $"{player.PlayerId}\t{player.Note}"));

        if (!TrySetClipboardText(text))
        {
            MessageDialog.ShowWarning(this, "寫入剪貼簿失敗", "剪貼簿可能被其他程式佔用，請稍後重試。");
            return;
        }

        AppendLog($"[玩家庫] 已複製 {targets.Count} 條到剪貼簿。");
    }

    /// <summary>
    /// 剪貼簿可能被其他程序短暫佔用，這裡做幾次重試再放棄。
    /// </summary>
    private static bool TrySetClipboardText(string text)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(80);
            }
        }

        return false;
    }

    /// <summary>
    /// 解析剪貼簿裡的玩家文字。
    /// 優先按「每行一個玩家」的格式解析，支援 Tab、逗號、分號分隔備註；
    /// 如果整段文字沒有命中行格式，再退回到按空白字元掃描所有數字 token。
    /// </summary>
    private static IEnumerable<PlayerProfile> ParseClipboardPlayers(string text)
    {
        var found = false;
        foreach (var rawLine in text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(['\t', ',', ';'], 2, StringSplitOptions.TrimEntries);
            if (parts.Length > 0 && PlayerRow.PlayerIdPattern.IsMatch(parts[0]))
            {
                found = true;
                yield return new PlayerProfile
                {
                    PlayerId = parts[0],
                    Note = parts.Length > 1 ? parts[1] : string.Empty,
                };
            }
        }

        if (found)
        {
            yield break;
        }

        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (PlayerRow.PlayerIdPattern.IsMatch(token))
            {
                yield return new PlayerProfile { PlayerId = token };
            }
        }
    }

    // ==================================================================
    // 批次確認 / 兌換
    // ==================================================================

    private async void RedeemSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        await RedeemAsync(_players.Where(player => player.Selected).ToList());
    }

    private async void RedeemAllButton_Click(object sender, RoutedEventArgs e)
    {
        await RedeemAsync(_players.ToList());
    }

    /// <summary>
    /// 「取消」按鈕：禁用按鈕並通知當前批次任務儘快停止。
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        _redeemCancellation?.Cancel();
        AppendLog("[取消] 正在停止任務...");
    }

    /// <summary>
    /// 執行批次確認/兌換流程。
    /// 順序是：引數校驗、鎖定介面、逐個確認玩家、按設定決定是否提交兌換、更新進度、最後恢復介面。
    /// </summary>
    private async Task RedeemAsync(IReadOnlyList<PlayerRow> targets)
    {
        if (_busy)
        {
            return;
        }

        if (ServerComboBox.SelectedItem is not ServerOption server)
        {
            MessageDialog.ShowWarning(this, "引數錯誤", "請先選擇伺服器。");
            return;
        }

        var serialCode = SerialCodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(serialCode) || !SerialCodePattern.IsMatch(serialCode))
        {
            MessageDialog.ShowWarning(this, "引數錯誤", "虛寶碼只能包含半形英文字母與數字。");
            return;
        }

        var validTargets = targets
            .Where(player => !string.IsNullOrWhiteSpace(player.PlayerId))
            .ToList();
        if (validTargets.Count == 0)
        {
            MessageDialog.ShowWarning(this, "引數錯誤",
                targets.Count == 0 ? "玩家庫裡還沒有玩家ID。" : "沒有可領取的玩家ID。");
            return;
        }

        foreach (var row in validTargets)
        {
            // 先把目標行標記為等待，使用者能看到這次任務實際會處理哪些玩家。
            row.LastResult = "等待";
        }

        SetBusy(true);
        _redeemCancellation = new CancellationTokenSource();
        ProgressBar.Minimum = 0;
        ProgressBar.Maximum = validTargets.Count;
        UpdateProgress(0);
        SetReadout(ProgressLabel, "領取中……");
        AppendLog($"[開始] 伺服器：{server.Name}，玩家數：{validTargets.Count}，虛寶碼：{serialCode}"
            + (ConfirmOnlyCheckBox.IsChecked == true ? "（僅確認不兌換）" : string.Empty));

        var success = 0;
        var failed = 0;
        var skipped = 0;

        try
        {
            for (var i = 0; i < validTargets.Count; i++)
            {
                // 每個玩家開始前檢查取消請求，確保「取消」按鈕響應足夠快。
                _redeemCancellation.Token.ThrowIfCancellationRequested();

                var row = validTargets[i];
                var playerId = row.PlayerId;
                if (!PlayerRow.PlayerIdPattern.IsMatch(playerId))
                {
                    skipped++;
                    row.LastResult = "跳過：玩家ID只能是數字";
                    AppendLog($"[跳過] {playerId} 玩家ID格式錯誤。");
                    UpdateProgress(i + 1);
                    continue;
                }

                try
                {
                    AppendLog($"[確認] {playerId}");
                    var confirmation = await _redeemClient.ConfirmAsync(
                        server.Id, playerId, serialCode, _redeemCancellation.Token);

                    if (ConfirmOnlyCheckBox.IsChecked == true)
                    {
                        // 僅確認模式不呼叫 Register，適合使用者先批次核對玩家 ID 是否正確。
                        success++;
                        row.LastResult = $"確認成功：{confirmation.UserName} / {confirmation.World}";
                        AppendLog($"[確認成功] {playerId} => {confirmation.UserName} / {confirmation.World}");
                    }
                    else
                    {
                        // 正式兌換前已經拿到確認資訊，所以成功日誌可以顯示玩家暱稱和世界。
                        await _redeemClient.RegisterAsync(server.Id, playerId, serialCode, _redeemCancellation.Token);
                        success++;
                        row.LastResult = $"領取成功：{confirmation.UserName} / {confirmation.World}";
                        AppendLog($"[領取成功] {playerId} => {confirmation.UserName} / {confirmation.World}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    row.LastResult = "失敗：" + ex.Message;
                    AppendLog($"[失敗] {playerId} - {ex.Message}");
                }

                UpdateProgress(i + 1);
                if (i < validTargets.Count - 1)
                {
                    // 給官方 API留一點間隔，避免批次請求過快。
                    await Task.Delay(700, _redeemCancellation.Token);
                }
            }

            SetReadout(ProgressLabel, "已完成");
            AppendLog($"[完成] 成功 {success}，跳過 {skipped}，失敗 {failed}。");
        }
        catch (OperationCanceledException)
        {
            SetReadout(ProgressLabel, "已取消");
            AppendLog($"[取消] 已停止。成功 {success}，跳過 {skipped}，失敗 {failed}。");
        }
        finally
        {
            _redeemCancellation?.Dispose();
            _redeemCancellation = null;
            SetBusy(false);
        }
    }

    /// <summary>
    /// 推進進度條並同步讀數。讀數只給百分比（保留一位小數），
    /// 不再報「已完成 / 總數」—— 跑了幾個、還剩幾個看色塊長度和日誌就夠了。
    /// 任務結束後讀數保留最後一次的值，便於回看這一輪最終跑了多少。
    /// </summary>
    private void UpdateProgress(int completed)
    {
        var total = (int)ProgressBar.Maximum;
        var done = Math.Clamp(completed, 0, total);
        ProgressBar.Value = done;

        SetReadout(ProgressText, total <= 0 ? string.Empty : $"{done * 100d / total:F1}%");
    }

    /// <summary>
    /// 寫入一處讀數文字。空字串時把整列收起：進度條會順勢頂到該列的位置，
    /// 空閒態下「按鈕 + 狀態文字 + 進度條 + 百分比」仍然是一條鋪滿整行的線，
    /// 按鈕右邊不會留出空檔。有文字時按樣式裡的 MinWidth 佔位，避免讀數出現
    /// 或長短變化時把進度條推來推去。
    /// </summary>
    private static void SetReadout(TextBlock target, string value)
    {
        target.Text = value;
        target.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// 切換介面忙碌狀態。批次兌換期間鎖定輸入與玩家庫編輯，只保留取消按鈕。
    /// </summary>
    private void SetBusy(bool busy)
    {
        _busy = busy;
        ServerComboBox.IsEnabled = !busy;
        RefreshServersButton.IsEnabled = !busy;
        SerialCodeTextBox.IsEnabled = !busy;
        ConfirmOnlyCheckBox.IsEnabled = !busy;
        PlayerIdTextBox.IsEnabled = !busy;
        NoteTextBox.IsEnabled = !busy;
        AddPlayerButton.IsEnabled = !busy;
        UpdatePlayerButton.IsEnabled = !busy;
        DeletePlayerButton.IsEnabled = !busy;
        SelectAllButton.IsEnabled = !busy;
        ClearSelectionButton.IsEnabled = !busy;
        ImportClipboardButton.IsEnabled = !busy;
        ExportClipboardButton.IsEnabled = !busy;
        RedeemSelectedButton.IsEnabled = !busy;
        RedeemAllButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        RefreshEditorState();
    }

    /// <summary>
    /// 批次設定玩家是否參與「領取勾選」。
    /// 有搜尋條件時只作用於當前可見的行，符合使用者「看到什麼就選什麼」的直覺。
    /// </summary>
    private void SetAllSelected(bool selected)
    {
        foreach (var player in _playersView.Cast<PlayerRow>())
        {
            player.Selected = selected;
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        SetAllSelected(true);
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        SetAllSelected(false);
    }

    // ==================================================================
    // 日誌立繪
    // ==================================================================

    /// <summary>
    /// 換上一張隨機立繪。內建立繪（隨 exe 編譯嵌入）與資料目錄 portraits 資料夾裡的圖片
    /// 共用一個輪換池：每次啟動隨機一張，點選立繪也可以手動換。
    /// </summary>
    private void ShowRandomPortrait()
    {
        if (_portraitLibrary.CreateRandom() is { } portrait)
        {
            PortraitImage.Source = CropToCentralSquare(portrait);
        }
    }

    /// <summary>
    /// 取圖片的中央正方形。顯示框是正方形，而 WPF 在長寬比不一致時會把 UniformToFill
    /// 的溢位部分從右下角裁掉（相當於錨定左上角），照片主體很容易偏出畫面；
    /// 先裁出中央方塊，矩形圖片就能以"主體居中"的方式落到框裡。
    /// </summary>
    private static BitmapSource CropToCentralSquare(BitmapSource source)
    {
        var side = Math.Min(source.PixelWidth, source.PixelHeight);
        if (source.PixelWidth == side && source.PixelHeight == side)
        {
            return source;
        }

        var cropped = new CroppedBitmap(
            source,
            new Int32Rect((source.PixelWidth - side) / 2, (source.PixelHeight - side) / 2, side, side));
        cropped.Freeze();
        return cropped;
    }

    /// <summary>
    /// 點選立繪手動換一張。
    /// </summary>
    private void PortraitHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowRandomPortrait();
    }

    // ==================================================================
    // 雜項
    // ==================================================================

    /// <summary>
    /// 名稱圖示點選：與舊版 LogoLink_Click 一致，跳轉官方兌換頁面。
    /// </summary>
    private void LogoLinkButton_Click(object sender, RoutedEventArgs e)
    {
        OpenOfficialCodePage();
    }

    /// <summary>
    /// 使用系統預設瀏覽器開啟官方兌換頁面。
    /// UseShellExecute=true 可以讓 Windows 按預設 URL 關聯處理，而不是要求指定瀏覽器路徑。
    /// </summary>
    private void OpenOfficialCodePage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OfficialCodePageUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageDialog.ShowWarning(this, "開啟失敗", ex.Message);
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
        _logLineCount = 0;
        _logCharCount = 0;
        UpdateLogSummary();
    }

    /// <summary>
    /// 在檔案總管中定位玩家庫檔案，方便使用者備份或遷移資料。
    /// </summary>
    private void OpenDataFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(_playerRepository.FilePath))
            {
                // select 引數會直接選中檔案，而不是隻開啟目錄。
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_playerRepository.FilePath}\"")
                {
                    UseShellExecute = true,
                });
                return;
            }

            var folder = Path.GetDirectoryName(_playerRepository.FilePath)!;
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageDialog.ShowWarning(this, "開啟失敗", ex.Message);
        }
    }

    /// <summary>
    /// 追加一條帶時間戳的日誌，並讓日誌框保持顯示最新內容。
    /// </summary>
    private void AppendLog(string message)
    {
        if (_isClosing)
        {
            return;
        }

        var line = $"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}";
        LogBox.AppendText(line);
        _logLineCount++;
        _logCharCount += line.Length;

        if (_logCharCount > LogTrimThreshold)
        {
            // 超長時只保留末尾內容，避免長時間執行後記憶體和重繪成本失控。
            var text = LogBox.Text;
            LogBox.Text = text.Length > LogTrimKeep ? text[^LogTrimKeep..] : text;
            _logCharCount = LogBox.Text.Length;
            _logLineCount = LogBox.Text.Count(character => character == '\n');
        }

        LogBox.ScrollToEnd();
        UpdateLogSummary();
    }

    private void UpdateLogSummary()
    {
        LogSummaryText.Text = _logLineCount == 0
            ? "本次執行暫無記錄"
            : $"本次執行 {_logLineCount} 條記錄";
    }

    /// <summary>
    /// 在視覺化樹裡向上查詢最近的指定型別祖先，用於判斷點選是否落在按鈕上。
    /// </summary>
    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}
