using System.Windows;
using System.Windows.Input;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 八门阵法生克全景大典独立弹窗 (8阵法免点击全览与8x8全局矩阵)
/// </summary>
public partial class FormationCounterWindow : Window
{
    public TroopCompassViewModel ViewModel => (TroopCompassViewModel)DataContext;

    public FormationCounterWindow(TroopCompassViewModel? viewModel = null)
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
