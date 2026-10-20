using System.Windows;
using System.Windows.Threading;

namespace Tianjige.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Console.Error.WriteLine($"[天机阁异常] 调度器未捕获异常: {e.Exception}");
        MessageBox.Show(
            $"天机阁遭遇意料之外的阻碍：\n{e.Exception.Message}\n\n详细信息：\n{e.Exception}",
            "天机阁 · 运行提示",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.Error.WriteLine($"[天机阁异常] 域未捕获异常: {e.ExceptionObject}");
    }
}

