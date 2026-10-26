using System.Windows;
using System.Windows.Input;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 兵种生克全景大典独立弹窗 (13兵种免点击全览与13x13全局矩阵)
/// </summary>
public partial class TroopCounterWindow : Window
{
    public TroopCompassViewModel ViewModel => (TroopCompassViewModel)DataContext;

    public TroopCounterWindow(TroopCompassViewModel? viewModel = null)
    {
        InitializeComponent();
        DataContext = viewModel ?? new TroopCompassViewModel();
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
