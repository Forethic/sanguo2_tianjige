using System.Windows;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Data.Memory.Models;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 《群英策 · 天机阁》副屏指挥战台 (Deck)
/// 横屏全景展现：武将大典、深层过滤与排序、存档自动侦测、战队编组、多重非模态名将弹窗与兵机罗盘
/// </summary>
public partial class DeckWindow : Window
{
    private readonly Dictionary<int, GeneralCardWindow> _openPopups = new();
    private TroopCompassWindow? _troopCompassWindow;
    private TroopCounterWindow? _troopCounterWindow;
    private FormationCounterWindow? _formationCounterWindow;

    public DeckViewModel? ViewModel => DataContext as DeckViewModel;

    public DeckWindow()
    {
        InitializeComponent();
    }

    public DeckWindow(DeckViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.OpenGeneralCardRequested = OnOpenGeneralCardRequested;
        viewModel.OpenTroopCompassRequested = OnOpenTroopCompassRequested;
        viewModel.OpenTroopCounterRequested = OnOpenTroopCounterRequested;
        viewModel.OpenFormationCounterRequested = OnOpenFormationCounterRequested;
    }

    private void OnOpenGeneralCardRequested(GeneralStaticInfo stat, LiveGeneralRecord? live)
    {
        int id = stat.Id;
        if (_openPopups.TryGetValue(id, out var win) && win.IsLoaded)
        {
            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }
            win.Activate();
            win.Focus();
            return;
        }

        var cardVm = new GeneralCardViewModel(stat, live);
        var popup = new GeneralCardWindow(cardVm);
        _openPopups[id] = popup;

        popup.Closed += (s, e) =>
        {
            _openPopups.Remove(id);
        };

        popup.Show();
    }

    private void OnOpenTroopCounterRequested()
    {
        if (_troopCounterWindow != null && _troopCounterWindow.IsLoaded)
        {
            if (_troopCounterWindow.WindowState == WindowState.Minimized)
            {
                _troopCounterWindow.WindowState = WindowState.Normal;
            }
            _troopCounterWindow.Activate();
            _troopCounterWindow.Focus();
            return;
        }

        _troopCounterWindow = new TroopCounterWindow();
        _troopCounterWindow.Closed += (s, e) =>
        {
            _troopCounterWindow = null;
        };
        _troopCounterWindow.Show();
    }

    private void OnOpenFormationCounterRequested()
    {
        if (_formationCounterWindow != null && _formationCounterWindow.IsLoaded)
        {
            if (_formationCounterWindow.WindowState == WindowState.Minimized)
            {
                _formationCounterWindow.WindowState = WindowState.Normal;
            }
            _formationCounterWindow.Activate();
            _formationCounterWindow.Focus();
            return;
        }

        _formationCounterWindow = new FormationCounterWindow();
        _formationCounterWindow.Closed += (s, e) =>
        {
            _formationCounterWindow = null;
        };
        _formationCounterWindow.Show();
    }

    private void OnOpenTroopCompassRequested()
    {
        if (_troopCompassWindow != null && _troopCompassWindow.IsLoaded)
        {
            if (_troopCompassWindow.WindowState == WindowState.Minimized)
            {
                _troopCompassWindow.WindowState = WindowState.Normal;
            }
            _troopCompassWindow.Activate();
            _troopCompassWindow.Focus();
            return;
        }

        _troopCompassWindow = new TroopCompassWindow();
        _troopCompassWindow.Closed += (s, e) =>
        {
            _troopCompassWindow = null;
        };
        _troopCompassWindow.Show();
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

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // 关闭所有子弹窗
        foreach (var popup in _openPopups.Values.ToList())
        {
            try { popup.Close(); } catch { }
        }
        _openPopups.Clear();

        try { _troopCompassWindow?.Close(); } catch { }
        _troopCompassWindow = null;
    }
}
