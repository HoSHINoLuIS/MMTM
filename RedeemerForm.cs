using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MementoMoriCodeRedeemer;

[DesignerCategory("Form")]
public partial class RedeemerForm : Form
{
    private const string BackgroundResourceName = "MementoMoriCodeRedeemer.Assets.Background_DungeonBattle.png";
    private const string AppIconResourceName = "MementoMoriCodeRedeemer.Assets.AppIcon.ico";
    private const string LogoResourceName = "MementoMoriCodeRedeemer.Assets.MementoMoriLogo.png";
    private const string SelectedRowBackgroundResourceName = "MementoMoriCodeRedeemer.Assets.SelectedRowBackground.png";
    private const string SelectionOnResourceName = "MementoMoriCodeRedeemer.Assets.SelectionOn.png";
    private const string SelectionOffResourceName = "MementoMoriCodeRedeemer.Assets.SelectionOff.png";
    private const string LockOffResourceName = "MementoMoriCodeRedeemer.Assets.LockOff.png";
    private const string LockOnResourceName = "MementoMoriCodeRedeemer.Assets.LockOn.png";
    private const string ProgressFillResourceName = "MementoMoriCodeRedeemer.Assets.ProgressFill.png";
    private const string ProgressTrackResourceName = "MementoMoriCodeRedeemer.Assets.ProgressTrack.png";
    private const string ButtonAddResourceName = "MementoMoriCodeRedeemer.Assets.ButtonAdd.png";
    private const string ButtonEditResourceName = "MementoMoriCodeRedeemer.Assets.ButtonEdit.png";
    private const string ButtonDeleteResourceName = "MementoMoriCodeRedeemer.Assets.ButtonDelete.png";
    private const string ButtonAllOnResourceName = "MementoMoriCodeRedeemer.Assets.ButtonAllOn.png";
    private const string ButtonAllOffResourceName = "MementoMoriCodeRedeemer.Assets.ButtonAllOff.png";
    private const string ButtonRefreshResourceName = "MementoMoriCodeRedeemer.Assets.ButtonRefresh.png";
    private const string ButtonCopyResourceName = "MementoMoriCodeRedeemer.Assets.ButtonCopy.png";
    private const string ButtonLoadInResourceName = "MementoMoriCodeRedeemer.Assets.ButtonLoadIn.png";
    private const string ConfirmCheckboxResourceName = "MementoMoriCodeRedeemer.Assets.ConfirmCheckbox.png";
    private const string ConfirmCheckResourceName = "MementoMoriCodeRedeemer.Assets.ConfirmCheck.png";
    private const string OfficialCodePageUrl = "https://mememori-game.com/tw/code/";

    private static readonly Regex PlayerIdPattern = new("^[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex SerialCodePattern = new("^[0-9a-zA-Z]+$", RegexOptions.Compiled);

    private readonly List<Image> _ownedImages = [];
    private readonly BindingList<PlayerRow> _players = [];
    private readonly PlayerRepository _playerRepository = new();
    private readonly MementoMoriRedeemClient _redeemClient = new();

    private CancellationTokenSource? _redeemCancellation;
    private Icon? _windowIcon;
    private Image? _selectionOnImage;
    private Image? _selectionOffImage;
    private Image? _lockOffImage;
    private Image? _lockOnImage;
    private bool _busy;
    private bool _loadingPlayers;

    public RedeemerForm()
    {
        InitializeComponent();
        ApplyRuntimeResources();
        LoadPlayers();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ClearPlayersGridSelection();
        await LoadServersAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _redeemCancellation?.Cancel();
        base.OnFormClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _redeemClient.Dispose();
        BackgroundImage?.Dispose();
        _windowIcon?.Dispose();
        foreach (var image in _ownedImages)
        {
            image.Dispose();
        }

        base.OnClosed(e);
    }

    private void ApplyRuntimeResources()
    {
        _windowIcon = LoadEmbeddedIcon();
        if (_windowIcon is not null)
        {
            Icon = _windowIcon;
        }

        BackgroundImage = LoadEmbeddedBitmap(BackgroundResourceName);
        BackgroundImageLayout = ImageLayout.Stretch;

        var logoImage = LoadEmbeddedBitmap(LogoResourceName);
        if (logoImage is not null)
        {
            _ownedImages.Add(logoImage);
            _logoLink.Image = logoImage;
        }

        var selectedRowBackground = LoadEmbeddedBitmap(SelectedRowBackgroundResourceName);
        if (selectedRowBackground is not null)
        {
            _ownedImages.Add(selectedRowBackground);
            _playersGrid.CurrentRowBackgroundImage = selectedRowBackground;
        }

        _selectionOnImage = LoadEmbeddedBitmap(SelectionOnResourceName);
        if (_selectionOnImage is not null)
        {
            _ownedImages.Add(_selectionOnImage);
        }

        _selectionOffImage = LoadEmbeddedBitmap(SelectionOffResourceName);
        if (_selectionOffImage is not null)
        {
            _ownedImages.Add(_selectionOffImage);
        }

        _lockOffImage = LoadEmbeddedBitmap(LockOffResourceName);
        if (_lockOffImage is not null)
        {
            _ownedImages.Add(_lockOffImage);
        }

        _lockOnImage = LoadEmbeddedBitmap(LockOnResourceName);
        if (_lockOnImage is not null)
        {
            _ownedImages.Add(_lockOnImage);
        }

        var progressTrackImage = LoadEmbeddedBitmap(ProgressTrackResourceName);
        if (progressTrackImage is not null)
        {
            _ownedImages.Add(progressTrackImage);
            _progressBar.TrackImage = progressTrackImage;
        }

        var progressFillImage = LoadEmbeddedBitmap(ProgressFillResourceName);
        if (progressFillImage is not null)
        {
            _ownedImages.Add(progressFillImage);
            _progressBar.FillImage = progressFillImage;
        }

        var confirmCheckboxImage = LoadEmbeddedBitmap(ConfirmCheckboxResourceName);
        if (confirmCheckboxImage is not null)
        {
            _ownedImages.Add(confirmCheckboxImage);
            _confirmOnlyCheckBox.BoxImage = confirmCheckboxImage;
        }

        var confirmCheckImage = LoadEmbeddedBitmap(ConfirmCheckResourceName);
        if (confirmCheckImage is not null)
        {
            _ownedImages.Add(confirmCheckImage);
            _confirmOnlyCheckBox.CheckImage = confirmCheckImage;
        }

        var iconButtonSize = new Size(44, 44);
        ApplyIconButton(_addPlayerButton, ButtonAddResourceName, "新增", iconButtonSize);
        ApplyIconButton(_updatePlayerButton, ButtonEditResourceName, "修改", iconButtonSize);
        ApplyIconButton(_deletePlayerButton, ButtonDeleteResourceName, "刪除", iconButtonSize);
        ApplyIconButton(_selectAllButton, ButtonAllOnResourceName, "全選", iconButtonSize);
        ApplyIconButton(_clearSelectionButton, ButtonAllOffResourceName, "全不選", iconButtonSize);
        ApplyIconButton(_refreshServersButton, ButtonRefreshResourceName, "刷新");
        ApplyIconButton(_importClipboardButton, ButtonLoadInResourceName, "從剪貼簿匯入ID", iconButtonSize);
        ApplyIconButton(_exportClipboardButton, ButtonCopyResourceName, "複製ID庫", iconButtonSize);

        _toolTip.SetToolTip(_logoLink, "跳轉至官方頁面");
    }

    private void ApplyIconButton(Button button, string resourceName, string toolTip, Size? size = null)
    {
        var image = LoadEmbeddedBitmap(resourceName);
        if (image is not null)
        {
            _ownedImages.Add(image);
            button.Text = string.Empty;
            button.BackgroundImage = image;
            button.BackgroundImageLayout = ImageLayout.Zoom;
        }

        if (size is { } fixedSize)
        {
            button.Size = fixedSize;
            button.MinimumSize = fixedSize;
            button.MaximumSize = fixedSize;
            button.Margin = new Padding(0, 0, 8, 0);
        }

        button.AccessibleName = toolTip;
        button.BackColor = BackColor;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(222, 220, 212);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 233, 225);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        _toolTip.SetToolTip(button, toolTip);
    }

    private static Bitmap? LoadEmbeddedBitmap(string resourceName)
    {
        var assembly = typeof(RedeemerForm).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;

        using var image = Image.FromStream(memory);
        return new Bitmap(image);
    }

    private static Icon? LoadEmbeddedIcon()
    {
        var assembly = typeof(RedeemerForm).Assembly;
        using var stream = assembly.GetManifestResourceStream(AppIconResourceName);
        if (stream is null)
        {
            return null;
        }

        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }

    private async void RefreshServersButton_Click(object? sender, EventArgs e)
    {
        await LoadServersAsync();
    }

    private void LogoLink_Click(object? sender, EventArgs e)
    {
        OpenOfficialCodePage();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        _cancelButton.Enabled = false;
        _redeemCancellation?.Cancel();
        AppendLog("[取消] 正在停止任務...");
    }

    private void SelectAllButton_Click(object? sender, EventArgs e)
    {
        SetAllSelected(true);
    }

    private void ClearSelectionButton_Click(object? sender, EventArgs e)
    {
        SetAllSelected(false);
    }

    private void PlayersGrid_SelectionChanged(object? sender, EventArgs e)
    {
        FillEditorFromCurrentRow();
        ClearPlayersGridHighlight();
    }

    private void PlayersGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_playersGrid.ReadOnly || e.RowIndex < 0 || e.ColumnIndex is not (0 or 1))
        {
            return;
        }

        if (_playersGrid.Rows[e.RowIndex].DataBoundItem is not PlayerRow row)
        {
            return;
        }

        if (e.ColumnIndex == 0)
        {
            row.Locked = !row.Locked;
            SavePlayersSafe();
            RefreshEditorState();
            _playersGrid.InvalidateRow(e.RowIndex);
            return;
        }

        row.Selected = !row.Selected;
        _playersGrid.InvalidateRow(e.RowIndex);
    }

    private void PlayersGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        SavePlayersSafe();
    }

    private void PlayersGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex is not (0 or 1))
        {
            return;
        }

        if (_playersGrid.Rows[e.RowIndex].DataBoundItem is not PlayerRow row)
        {
            return;
        }

        e.Value = e.ColumnIndex == 0
            ? (row.Locked ? _lockOnImage : _lockOffImage)
            : (row.Selected ? _selectionOnImage : _selectionOffImage);
        e.FormattingApplied = true;
    }

    private void PlayersGrid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        ClearPlayersGridHighlight();
    }

    private void PlayersGrid_CurrentCellChanged(object? sender, EventArgs e)
    {
        FillEditorFromCurrentRow();
        ClearPlayersGridHighlight();
    }

    private void PlayersGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_playersGrid.IsCurrentCellDirty)
        {
            _playersGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void OpenOfficialCodePage()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = OfficialCodePageUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "開啟失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    private async Task LoadServersAsync()
    {
        _refreshServersButton.Enabled = false;
        _serverComboBox.Enabled = false;
        _statusLabel.Text = "正在讀取伺服器列表";

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var servers = await _redeemClient.GetServersAsync(cancellation.Token);
            _serverComboBox.Items.Clear();

            foreach (var server in servers)
            {
                _serverComboBox.Items.Add(server);
            }

            var asiaIndex = servers
                .Select((server, index) => new { server, index })
                .FirstOrDefault(item => item.server.Id == 3 || item.server.Name.Contains("Asia", StringComparison.OrdinalIgnoreCase))
                ?.index;

            if (_serverComboBox.Items.Count > 0)
            {
                _serverComboBox.SelectedIndex = asiaIndex ?? 0;
            }

            _statusLabel.Text = $"伺服器列表已更新：{servers.Count} 個";
            AppendLog($"[伺服器] 已載入 {servers.Count} 個伺服器。");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "伺服器列表讀取失敗";
            AppendLog("[錯誤] 伺服器列表讀取失敗：" + ex.Message);
            MessageBox.Show(this, ex.Message, "伺服器列表讀取失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _serverComboBox.Enabled = true;
            _refreshServersButton.Enabled = true;
        }
    }

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
                    PlayerId = player.PlayerId.Trim(),
                    Note = player.Note.Trim(),
                });
            }

            AppendLog($"[玩家庫] 已載入 {_players.Count} 個玩家ID。");
        }
        catch (Exception ex)
        {
            AppendLog("[錯誤] " + ex.Message);
            MessageBox.Show(this, ex.Message, "玩家庫讀取失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loadingPlayers = false;
        }
    }

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
                    PlayerId = player.PlayerId.Trim(),
                    Note = player.Note.Trim(),
                }));
        }
        catch (Exception ex)
        {
            AppendLog("[錯誤] " + ex.Message);
            MessageBox.Show(this, ex.Message, "玩家庫儲存失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddPlayerButton_Click(object? sender, EventArgs e)
    {
        var playerId = _playerIdTextBox.Text.Trim();
        if (!ValidatePlayerIdForEditor(playerId))
        {
            return;
        }

        if (_players.Any(player => string.Equals(player.PlayerId, playerId, StringComparison.Ordinal)))
        {
            MessageBox.Show(this, "這個玩家ID已經在內容庫裡。", "重複ID", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = new PlayerRow
        {
            Selected = true,
            PlayerId = playerId,
            Note = _noteTextBox.Text.Trim(),
        };
        _players.Add(row);
        SavePlayersSafe();
        SelectRow(row);
        AppendLog($"[玩家庫] 已新增 {playerId}。");
    }

    private void UpdatePlayerButton_Click(object? sender, EventArgs e)
    {
        var row = GetCurrentRow();
        if (row is null)
        {
            MessageBox.Show(this, "請先選擇一個玩家ID。", "未選擇", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (row.Locked)
        {
            MessageBox.Show(this, "該行已鎖定，切換為解鎖後才能修改。", "已鎖定", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var playerId = _playerIdTextBox.Text.Trim();
        if (!ValidatePlayerIdForEditor(playerId))
        {
            return;
        }

        if (_players.Any(player => !ReferenceEquals(player, row)
            && string.Equals(player.PlayerId, playerId, StringComparison.Ordinal)))
        {
            MessageBox.Show(this, "這個玩家ID已經在內容庫裡。", "重複ID", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        row.PlayerId = playerId;
        row.Note = _noteTextBox.Text.Trim();
        SavePlayersSafe();
        AppendLog($"[玩家庫] 已修改 {playerId}。");
    }

    private void DeletePlayerButton_Click(object? sender, EventArgs e)
    {
        var row = GetCurrentRow();
        if (row is null)
        {
            MessageBox.Show(this, "請先選擇一個玩家ID。", "未選擇", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (row.Locked)
        {
            MessageBox.Show(this, "該行已鎖定，切換為解鎖後才能刪除。", "已鎖定", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this, $"刪除玩家ID {row.PlayerId}？", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes)
        {
            return;
        }

        _players.Remove(row);
        SavePlayersSafe();
        AppendLog($"[玩家庫] 已刪除 {row.PlayerId}。");
    }

    private void ImportClipboardButton_Click(object? sender, EventArgs e)
    {
        string text;
        try
        {
            text = Clipboard.GetText();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "讀取剪貼簿失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "剪貼簿裡沒有可匯入的玩家ID。", "剪貼簿為空", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var added = 0;
        var skipped = 0;
        foreach (var item in ParseClipboardPlayers(text))
        {
            if (!PlayerIdPattern.IsMatch(item.PlayerId)
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
    }

    private void ExportClipboardButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var text = string.Join(Environment.NewLine, _players.Select(player =>
                string.IsNullOrWhiteSpace(player.Note)
                    ? player.PlayerId
                    : $"{player.PlayerId}\t{player.Note}"));
            Clipboard.SetText(text);
            AppendLog($"[玩家庫] 已複製 {_players.Count} 筆到剪貼簿。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "寫入剪貼簿失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void RedeemSelectedButton_Click(object? sender, EventArgs e)
    {
        if (!EndGridEdit())
        {
            return;
        }

        await RedeemAsync(_players.Where(player => player.Selected).ToList());
    }

    private async void RedeemAllButton_Click(object? sender, EventArgs e)
    {
        if (!EndGridEdit())
        {
            return;
        }

        await RedeemAsync(_players.ToList());
    }

    private async Task RedeemAsync(IReadOnlyList<PlayerRow> targets)
    {
        if (_serverComboBox.SelectedItem is not ServerOption server)
        {
            MessageBox.Show(this, "請先選擇伺服器。", "參數錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var serialCode = _serialCodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(serialCode) || !SerialCodePattern.IsMatch(serialCode))
        {
            MessageBox.Show(this, "虛寶碼只能包含半形英文字母與數字。", "參數錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var validTargets = targets
            .Where(player => !string.IsNullOrWhiteSpace(player.PlayerId))
            .ToList();
        if (validTargets.Count == 0)
        {
            MessageBox.Show(this, "沒有可領取的玩家ID。", "參數錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (var row in validTargets)
        {
            row.LastResult = "等待";
        }

        SetBusy(true);
        _redeemCancellation = new CancellationTokenSource();
        _progressBar.Minimum = 0;
        _progressBar.Maximum = validTargets.Count;
        _progressBar.Value = 0;
        _statusLabel.Text = "開始領取";
        AppendLog($"[開始] 伺服器：{server.Name}，玩家數：{validTargets.Count}，虛寶碼：{serialCode}");

        var success = 0;
        var failed = 0;
        var skipped = 0;

        try
        {
            for (var i = 0; i < validTargets.Count; i++)
            {
                _redeemCancellation.Token.ThrowIfCancellationRequested();

                var row = validTargets[i];
                var playerId = row.PlayerId.Trim();
                if (!PlayerIdPattern.IsMatch(playerId))
                {
                    skipped++;
                    row.LastResult = "跳過：玩家ID只能是數字";
                    AppendLog($"[跳過] {playerId} 玩家ID格式錯誤。");
                    UpdateProgress(i + 1, validTargets.Count, success, skipped, failed);
                    continue;
                }

                try
                {
                    _statusLabel.Text = $"確認 {playerId} ({i + 1}/{validTargets.Count})";
                    AppendLog($"[確認] {playerId}");
                    var confirmation = await _redeemClient.ConfirmAsync(server.Id, playerId, serialCode, _redeemCancellation.Token);

                    if (_confirmOnlyCheckBox.Checked)
                    {
                        success++;
                        row.LastResult = $"確認成功：{confirmation.UserName} / {confirmation.World}";
                        AppendLog($"[確認成功] {playerId} => {confirmation.UserName} / {confirmation.World}");
                    }
                    else
                    {
                        _statusLabel.Text = $"發送 {playerId} ({i + 1}/{validTargets.Count})";
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

                UpdateProgress(i + 1, validTargets.Count, success, skipped, failed);
                if (i < validTargets.Count - 1)
                {
                    await Task.Delay(700, _redeemCancellation.Token);
                }
            }

            _statusLabel.Text = $"完成：成功 {success}，跳過 {skipped}，失敗 {failed}";
            AppendLog($"[完成] 成功 {success}，跳過 {skipped}，失敗 {failed}。");
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = $"已取消：成功 {success}，跳過 {skipped}，失敗 {failed}";
            AppendLog($"[取消] 已停止。成功 {success}，跳過 {skipped}，失敗 {failed}。");
        }
        finally
        {
            _redeemCancellation.Dispose();
            _redeemCancellation = null;
            SetBusy(false);
        }
    }

    private void UpdateProgress(int completed, int total, int success, int skipped, int failed)
    {
        _progressBar.Value = Math.Min(_progressBar.Maximum, completed);
        _statusLabel.Text = $"{completed}/{total}  成功 {success}  跳過 {skipped}  失敗 {failed}";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _serverComboBox.Enabled = !busy;
        _refreshServersButton.Enabled = !busy;
        _serialCodeTextBox.Enabled = !busy;
        _confirmOnlyCheckBox.Enabled = !busy;
        _playersGrid.ReadOnly = busy;
        _playerIdTextBox.Enabled = !busy;
        _noteTextBox.Enabled = !busy;
        _addPlayerButton.Enabled = !busy;
        _updatePlayerButton.Enabled = !busy;
        _deletePlayerButton.Enabled = !busy;
        _selectAllButton.Enabled = !busy;
        _clearSelectionButton.Enabled = !busy;
        _importClipboardButton.Enabled = !busy;
        _exportClipboardButton.Enabled = !busy;
        _redeemSelectedButton.Enabled = !busy;
        _redeemAllButton.Enabled = !busy;
        _cancelButton.Enabled = busy;
        RefreshEditorState();
    }

    private void SetAllSelected(bool selected)
    {
        foreach (var player in _players)
        {
            player.Selected = selected;
        }

        _playersGrid.Invalidate();
    }

    private void PlayersGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var column = _playersGrid.Columns[e.ColumnIndex];
        if (!string.Equals(column.DataPropertyName, nameof(PlayerRow.PlayerId), StringComparison.Ordinal))
        {
            return;
        }

        var value = Convert.ToString(e.FormattedValue)?.Trim() ?? string.Empty;
        if (!PlayerIdPattern.IsMatch(value))
        {
            _playersGrid.Rows[e.RowIndex].ErrorText = "玩家ID只能輸入數字。";
            e.Cancel = true;
            return;
        }

        var duplicate = _players
            .Select((player, index) => new { player, index })
            .Any(item => item.index != e.RowIndex
                && string.Equals(item.player.PlayerId, value, StringComparison.Ordinal));
        if (duplicate)
        {
            _playersGrid.Rows[e.RowIndex].ErrorText = "這個玩家ID已經存在。";
            e.Cancel = true;
        }
        else
        {
            _playersGrid.Rows[e.RowIndex].ErrorText = string.Empty;
        }
    }

    private bool EndGridEdit()
    {
        try
        {
            _playersGrid.EndEdit();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "表格內容有誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private bool ValidatePlayerIdForEditor(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || !PlayerIdPattern.IsMatch(playerId))
        {
            MessageBox.Show(this, "玩家ID只能輸入半形數字。", "參數錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private void FillEditorFromCurrentRow()
    {
        var row = GetCurrentRow();
        if (row is null)
        {
            _playerIdTextBox.Clear();
            _noteTextBox.Clear();
            RefreshEditorState();
            return;
        }

        _playerIdTextBox.Text = row.PlayerId;
        _noteTextBox.Text = row.Note;
        RefreshEditorState();
    }

    private PlayerRow? GetCurrentRow()
    {
        return _playersGrid.CurrentRow?.DataBoundItem as PlayerRow;
    }

    private void ClearPlayersGridSelection()
    {
        _playersGrid.CurrentCell = null;
        ClearPlayersGridHighlight();
        RefreshEditorState();
    }

    private void ClearPlayersGridHighlight()
    {
        _playersGrid.ClearSelection();
        _playersGrid.Invalidate();
    }

    private void SelectRow(PlayerRow row)
    {
        var index = _players.IndexOf(row);
        if (index < 0)
        {
            return;
        }

        _playersGrid.CurrentCell = _playersGrid.Rows[index].Cells[2];
        FillEditorFromCurrentRow();
        ClearPlayersGridHighlight();
    }

    private void RefreshEditorState()
    {
        var canModifyCurrentRow = !_busy && GetCurrentRow() is { Locked: false };
        _updatePlayerButton.Enabled = canModifyCurrentRow;
        _deletePlayerButton.Enabled = canModifyCurrentRow;
    }

    private static IEnumerable<PlayerProfile> ParseClipboardPlayers(string text)
    {
        var found = false;
        foreach (var rawLine in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(new[] { '\t', ',', ';' }, 2, StringSplitOptions.TrimEntries);
            if (parts.Length > 0 && PlayerIdPattern.IsMatch(parts[0]))
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
            if (PlayerIdPattern.IsMatch(token))
            {
                yield return new PlayerProfile { PlayerId = token };
            }
        }
    }

    private void AppendLog(string message)
    {
        if (_logBox.IsDisposed)
        {
            return;
        }

        _logBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private sealed class PlayerRow : INotifyPropertyChanged
    {
        private bool _locked;
        private bool _selected;
        private string _playerId = string.Empty;
        private string _note = string.Empty;
        private string _lastResult = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Locked
        {
            get => _locked;
            set => SetField(ref _locked, value);
        }

        public bool Selected
        {
            get => _selected;
            set => SetField(ref _selected, value);
        }

        public string PlayerId
        {
            get => _playerId;
            set => SetField(ref _playerId, value.Trim());
        }

        public string Note
        {
            get => _note;
            set => SetField(ref _note, value.Trim());
        }

        public string LastResult
        {
            get => _lastResult;
            set => SetField(ref _lastResult, value);
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

