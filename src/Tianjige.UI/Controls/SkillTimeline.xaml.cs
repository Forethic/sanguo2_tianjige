using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Tianjige.UI.Controls;

public partial class SkillTimeline : UserControl
{
    public static readonly DependencyProperty SkillTreeProperty =
        DependencyProperty.Register(
            nameof(SkillTree),
            typeof(IEnumerable),
            typeof(SkillTimeline),
            new PropertyMetadata(null));

    public IEnumerable? SkillTree
    {
        get => (IEnumerable?)GetValue(SkillTreeProperty);
        set => SetValue(SkillTreeProperty, value);
    }

    public SkillTimeline()
    {
        InitializeComponent();
    }
}
