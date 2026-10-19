using System.Windows;
using System.Windows.Input;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI;

/// <summary>
/// 天机阁桌面参谋工作台主窗体
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && DataContext is MainViewModel vm)
        {
            if (vm.GeneralStarVm.IsReverseLookupOpen)
            {
                vm.GeneralStarVm.CloseReverseLookupCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}