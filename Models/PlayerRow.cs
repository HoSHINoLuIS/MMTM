using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MementoMoriCodeRedeemer.Wpf.Models;

/// <summary>
/// 介面表格使用的玩家行模型。
/// 相比持久化的 <see cref="PlayerProfile"/> 多了兩個執行時欄位：
/// <see cref="Selected"/> 控制是否參與「領取勾選」，<see cref="LastResult"/> 展示最近一次確認/兌換結果。
/// 實現 INotifyPropertyChanged 後，WPF 的繫結會在屬性變化時自動重新整理單元格，
/// 不需要再手動呼叫 InvalidateRow 之類的重繪介面。
/// </summary>
public sealed class PlayerRow : INotifyPropertyChanged
{
    /// <summary>玩家 ID 的官方格式：只能是半形數字。</summary>
    public static readonly Regex PlayerIdPattern = new("^[0-9]+$", RegexOptions.Compiled);

    private bool _locked;
    private bool _selected;
    private string _playerId = string.Empty;
    private string _note = string.Empty;
    private string _lastResult = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>是否鎖定該行。鎖定後不能從編輯區修改或刪除。</summary>
    public bool Locked
    {
        get => _locked;
        set
        {
            if (SetField(ref _locked, value))
            {
                OnPropertyChanged(nameof(CanModify));
            }
        }
    }

    /// <summary>是否參與「領取勾選」的批次任務。</summary>
    public bool Selected
    {
        get => _selected;
        set => SetField(ref _selected, value);
    }

    /// <summary>玩家 ID。賦值時統一 Trim，避免首尾空格進入儲存檔案或介面引數。</summary>
    public string PlayerId
    {
        get => _playerId;
        set => SetField(ref _playerId, (value ?? string.Empty).Trim());
    }

    /// <summary>使用者備註。只用於本地顯示和匯入匯出，不會提交給官方 API。</summary>
    public string Note
    {
        get => _note;
        set => SetField(ref _note, (value ?? string.Empty).Trim());
    }

    /// <summary>最近一次確認或兌換的結果文字。</summary>
    public string LastResult
    {
        get => _lastResult;
        set
        {
            if (SetField(ref _lastResult, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(ResultKind));
            }
        }
    }

    /// <summary>當前行是否允許被編輯區修改或刪除（未鎖定即可）。</summary>
    public bool CanModify => !_locked;

    /// <summary>
    /// 把結果文字歸一成四種狀態，介面據此上不同的顏色。
    /// 顏色對映寫在 XAML 的 DataTrigger 裡，模型只負責給出語義。
    /// </summary>
    public string ResultKind
    {
        get
        {
            if (string.IsNullOrEmpty(_lastResult))
            {
                return "none";
            }

            if (_lastResult.StartsWith("領取成功", StringComparison.Ordinal)
                || _lastResult.StartsWith("確認成功", StringComparison.Ordinal))
            {
                return "success";
            }

            if (_lastResult.StartsWith("失敗", StringComparison.Ordinal))
            {
                return "fail";
            }

            if (_lastResult.StartsWith("跳過", StringComparison.Ordinal))
            {
                return "skip";
            }

            return _lastResult == "等待" ? "wait" : "none";
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
