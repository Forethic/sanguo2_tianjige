using System.Windows;
using System.Windows.Input;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 兵机盘 · 兵阵生克天机 (金木水火土) 弹窗
/// </summary>
public partial class TroopCompassWindow : Window
{
    public TroopCompassWindow() : this(new TroopCompassViewModel())
    {
    }

    public TroopCompassWindow(TroopCompassViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
