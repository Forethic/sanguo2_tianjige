using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tianjige.UI.Controls;

public partial class SealBadge : UserControl
{
    public static readonly DependencyProperty TierProperty =
        DependencyProperty.Register(
            nameof(Tier),
            typeof(string),
            typeof(SealBadge),
            new PropertyMetadata(string.Empty, OnTierChanged));

    public string Tier
    {
        get => (string)GetValue(TierProperty);
        set => SetValue(TierProperty, value);
    }

    public static readonly DependencyProperty TierTextProperty =
        DependencyProperty.Register(
            nameof(TierText),
            typeof(string),
            typeof(SealBadge),
            new PropertyMetadata("良将"));

    public string TierText
    {
        get => (string)GetValue(TierTextProperty);
        private set => SetValue(TierTextProperty, value);
    }

    public SealBadge()
    {
        InitializeComponent();
        UpdateAppearance("良将");
    }

    private static void OnTierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SealBadge badge && e.NewValue is string rawTier)
        {
            badge.UpdateAppearance(rawTier);
        }
    }

    private void UpdateAppearance(string rawTier)
    {
        var clean = rawTier.Trim('【', '】', ' ');
        if (string.IsNullOrWhiteSpace(clean)) clean = "良将";
        TierText = clean;

        switch (clean)
        {
            case "无双":
                SealCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#381310"));
                SealCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4A853"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE299"));
                SealEmblem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE299"));
                SealGlow.Color = (Color)ColorConverter.ConvertFromString("#C93B2B");
                SealGlow.Opacity = 0.45;
                break;

            case "谋主":
                SealCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#152436"));
                SealCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F7EA8"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0D7F8"));
                SealEmblem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0D7F8"));
                SealGlow.Color = (Color)ColorConverter.ConvertFromString("#25629E");
                SealGlow.Opacity = 0.4;
                break;

            case "良将":
                SealCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E1613"));
                SealCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#993C30"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2ACA2"));
                SealEmblem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2ACA2"));
                SealGlow.Color = (Color)ColorConverter.ConvertFromString("#8F281E");
                SealGlow.Opacity = 0.35;
                break;

            case "裨将":
                SealCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#261E14"));
                SealCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7D6031"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFC798"));
                SealEmblem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFC798"));
                SealGlow.Opacity = 0;
                break;

            case "白身":
            default:
                SealCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1D1B"));
                SealCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F473F"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A89F94"));
                SealEmblem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A89F94"));
                SealGlow.Opacity = 0;
                break;
        }
    }
}
