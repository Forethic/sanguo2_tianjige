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
            new PropertyMetadata("【良将】"));

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
        // Strip brackets if provided e.g. "【无双】" -> "无双"
        var clean = rawTier.Trim('【', '】', ' ');
        TierText = $"【{clean}】";

        // Five-Tier Vermilion Ink Styling:
        // 【无双】: 耀金朱砂 (Radiant Vermilion with Gold Filigree)
        // 【良将】: 雄烈朱砂 (Fiery Vermilion)
        // 【谋主】: 玄青朱砂 (Royal Vermilion / Indigo Deep Ink)
        // 【裨将】: 古铜沉砂 (Amber Bronze)
        // 【白身】: 水墨素石 (Muted Stone Gray)
        switch (clean)
        {
            case "无双":
                OuterBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8C982"));
                InnerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D94536"));
                OuterBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D1612"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
                break;

            case "谋主":
                OuterBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6289A8"));
                InnerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A63428"));
                OuterBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D2530"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0C4E2"));
                break;

            case "良将":
                OuterBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A63428"));
                InnerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D94536"));
                OuterBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#301815"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0A89E"));
                break;

            case "裨将":
                OuterBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C6E2D"));
                InnerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A88544"));
                OuterBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#292218"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DEC28C"));
                break;

            case "白身":
            default:
                OuterBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C5449"));
                InnerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#73695C"));
                OuterBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#211F1D"));
                SealText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A89E8C"));
                break;
        }
    }
}
