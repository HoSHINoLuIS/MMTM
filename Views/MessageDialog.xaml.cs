using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MementoMoriCodeRedeemer.Wpf.Views;

/// <summary>
/// 對話方塊型別。隻影響圖示形狀和配色，文案完全由呼叫方決定。
/// </summary>
public enum DialogKind
{
    /// <summary>普通提示。</summary>
    Information,

    /// <summary>需要注意的警告。</summary>
    Warning,

    /// <summary>錯誤。</summary>
    Error,

    /// <summary>需要使用者確認的選擇。</summary>
    Question,
}

/// <summary>
/// 替換系統 MessageBox 的自繪對話方塊。
/// 系統 MessageBox 無法跟隨應用主題，這裡用同一套羊皮紙卡片皮膚，
/// 保證從主視窗到提示框的視覺是一致的。
/// </summary>
public partial class MessageDialog : Window
{
    private bool _confirmed;

    private MessageDialog()
    {
        InitializeComponent();
    }

    /// <summary>顯示一條普通提示。</summary>
    public static void ShowInfo(Window? owner, string title, string message)
    {
        ShowCore(owner, DialogKind.Information, title, message, withCancel: false);
    }

    /// <summary>顯示一條警告。</summary>
    public static void ShowWarning(Window? owner, string title, string message)
    {
        ShowCore(owner, DialogKind.Warning, title, message, withCancel: false);
    }

    /// <summary>顯示一條錯誤。</summary>
    public static void ShowError(Window? owner, string title, string message)
    {
        ShowCore(owner, DialogKind.Error, title, message, withCancel: false);
    }

    /// <summary>
    /// 顯示一個確認框。使用者點「確定」回傳 true，其餘情況（含關閉視窗）回傳 false。
    /// </summary>
    public static bool Confirm(Window? owner, string title, string message)
    {
        return ShowCore(owner, DialogKind.Question, title, message, withCancel: true);
    }

    private static bool ShowCore(Window? owner, DialogKind kind, string title, string message, bool withCancel)
    {
        var dialog = new MessageDialog();
        dialog.ApplyKind(kind);
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.CancelButton.Visibility = withCancel ? Visibility.Visible : Visibility.Collapsed;
        dialog.OkButton.Content = withCancel ? "確定" : "知道了";

        // 傳入的 owner 可能還沒顯示出來（例如啟動階段的異常），這時直接無主顯示。
        if (owner is not null && owner.IsLoaded && !ReferenceEquals(owner, dialog))
        {
            dialog.Owner = owner;
        }

        dialog.ShowDialog();
        return dialog._confirmed;
    }

    /// <summary>
    /// 按型別切換圖示與配色。幾何形狀只有「!」和「?」兩種，靠顏色區分語義。
    /// </summary>
    private void ApplyKind(DialogKind kind)
    {
        switch (kind)
        {
            case DialogKind.Warning:
                IconGlyph.Text = "!";
                IconGlyph.Foreground = (Brush)FindResource("GoldBrush");
                IconCircle.Fill = (Brush)FindResource("GoldWashBrush");
                break;

            case DialogKind.Error:
                IconGlyph.Text = "!";
                IconGlyph.Foreground = (Brush)FindResource("RoseBrush");
                IconCircle.Fill = (Brush)FindResource("RoseWashBrush");
                break;

            case DialogKind.Question:
                IconGlyph.Text = "?";
                IconGlyph.Foreground = (Brush)FindResource("SageDeepBrush");
                IconCircle.Fill = (Brush)FindResource("SageWashBrush");
                break;

            default:
                IconGlyph.Text = "i";
                IconGlyph.Foreground = (Brush)FindResource("SageDeepBrush");
                IconCircle.Fill = (Brush)FindResource("SageWashBrush");
                break;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _confirmed = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _confirmed = false;
        Close();
    }

    /// <summary>
    /// 鍵盤快捷鍵：Esc 取消，Enter 確定。
    /// 這裡用 PreviewKeyDown 統一處理，避免和按鈕自身的 IsDefault/IsCancel 邏輯重複觸發。
    /// </summary>
    private void MessageDialog_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _confirmed = false;
            e.Handled = true;
            Close();
        }
        else if (e.Key == Key.Enter)
        {
            _confirmed = true;
            e.Handled = true;
            Close();
        }
    }

    /// <summary>
    /// 無邊框視窗需要自己實現拖曳。點在按鈕上時不觸發，避免搶掉點選。
    /// </summary>
    private void RootCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindAncestorButton(source) is not null)
        {
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static Button? FindAncestorButton(DependencyObject source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Button button)
            {
                return button;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}
