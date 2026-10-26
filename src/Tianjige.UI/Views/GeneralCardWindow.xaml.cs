using System.Windows;
using System.Windows.Input;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 三国群英传2 经典单武将名将卡弹窗 (非模态，可置顶，支持多开但单武将唯一激活)
/// </summary>
public partial class GeneralCardWindow : Window
{
    public int GeneralId => (DataContext as GeneralCardViewModel)?.GeneralId ?? 0;

    public GeneralCardWindow()
    {
        InitializeComponent();
    }

    public GeneralCardWindow(GeneralCardViewModel viewModel) : this()
    {
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
