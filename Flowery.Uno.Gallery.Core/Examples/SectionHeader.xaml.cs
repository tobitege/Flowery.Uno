using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class SectionHeader : UserControl
    {
        public static readonly DependencyProperty SectionIdProperty =
            DependencyProperty.Register(
                nameof(SectionId),
                typeof(string),
                typeof(SectionHeader),
                new PropertyMetadata(string.Empty));

        public string SectionId
        {
            get => (string)GetValue(SectionIdProperty);
            set => SetValue(SectionIdProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(SectionHeader),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public SectionHeader()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            FlowerySizeManager.SizeChanged += OnGlobalSizeChanged;
            ApplyTheme();
            ApplySize(FlowerySizeManager.CurrentSize);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            FlowerySizeManager.SizeChanged -= OnGlobalSizeChanged;
        }

        private void OnThemeChanged(object? sender, string themeName)
        {
            ApplyTheme();
        }

        private void OnGlobalSizeChanged(object? sender, DaisySize size)
        {
            ApplySize(size);
        }

        private void ApplyTheme()
        {
            // ThemeResource doesn't auto-refresh in WinUI/Uno when MergedDictionaries change,
            // so we manually update the foreground on theme change.
            var brush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            if (brush != null)
            {
                TitleTextBlock.Foreground = brush;
            }
        }

        private void ApplySize(DaisySize size)
        {
            TitleTextBlock.FontSize = size switch
            {
                DaisySize.ExtraSmall => 12,
                DaisySize.Small => 14,
                DaisySize.Medium => 16,
                DaisySize.Large => 18,
                DaisySize.ExtraLarge => 20,
                _ => 14
            };
        }
    }
}
