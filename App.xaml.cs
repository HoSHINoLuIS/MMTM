using System.Windows;
using System.Windows.Threading;
using MementoMoriCodeRedeemer.Wpf.Views;

namespace MementoMoriCodeRedeemer.Wpf;

/// <summary>
/// 應用程式入口。
/// 除了常規啟動，還掛了一個全域性未處理異常兜底：
/// 任何漏網的異常都會彈出統一樣式的提示框，而不是直接讓程序消失。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var message = e.Exception.InnerException?.Message ?? e.Exception.Message;
        MessageDialog.ShowError(Current?.MainWindow, "程式遇到未處理的問題", message);
    }
}
