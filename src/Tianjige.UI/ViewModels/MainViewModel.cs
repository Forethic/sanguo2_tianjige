using System.Windows.Input;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly StaticRepository _repository;

    public GeneralStarViewModel GeneralStarVm { get; }
    public TroopCompassViewModel TroopCompassVm { get; }
    public WorldSituationViewModel WorldSituationVm { get; }

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsGeneralStarSelected));
                OnPropertyChanged(nameof(IsTroopCompassSelected));
                OnPropertyChanged(nameof(IsWorldSituationSelected));
            }
        }
    }

    public bool IsGeneralStarSelected => SelectedTabIndex == 0;
    public bool IsTroopCompassSelected => SelectedTabIndex == 1;
    public bool IsWorldSituationSelected => SelectedTabIndex == 2;

    private bool _isTopmost = true;
    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    private string _seasonTitle = "建安五年 · 岁在庚辰 · 孟秋七月";
    public string SeasonTitle
    {
        get => _seasonTitle;
        set => SetProperty(ref _seasonTitle, value);
    }

    private string _connectionStatus = "离线大典速查模式 · 气运通泰";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public ICommand SwitchTabCommand { get; }
    public ICommand ToggleTopmostCommand { get; }

    public MainViewModel(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        GeneralStarVm = new GeneralStarViewModel(_repository);
        TroopCompassVm = new TroopCompassViewModel(_repository);
        WorldSituationVm = new WorldSituationViewModel();

        SwitchTabCommand = new RelayCommand<object>(param =>
        {
            if (param is int index) SelectedTabIndex = index;
            else if (param is string s && int.TryParse(s, out var parsed)) SelectedTabIndex = parsed;
        });

        ToggleTopmostCommand = new RelayCommand(() =>
        {
            IsTopmost = !IsTopmost;
        });
    }
}
