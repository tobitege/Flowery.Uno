using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class ShowcaseNeumorphicButton : DaisyBaseContentControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(ShowcaseNeumorphicButton),
                new PropertyMetadata(string.Empty, OnTextChanged));

        private readonly Grid _rootGrid;
        private readonly Border _rootBorder;
        private readonly TextBlock _textBlock;

        public ShowcaseNeumorphicButton()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            _textBlock = new TextBlock
            {
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Child = _textBlock
            };

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_rootBorder);

            Content = _rootGrid;
            ApplyTheme();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            ApplyTheme();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ShowcaseNeumorphicButton control)
            {
                control._textBlock.Text = e.NewValue as string ?? string.Empty;
            }
        }

        private void ApplyTheme()
        {
            var surfaceBrush = GetThemedBrush("DaisyBase200Brush");
            _rootBorder.Background = surfaceBrush;
            Background = surfaceBrush;
            _textBlock.Foreground = GetThemedBrush("DaisyBaseContentBrush");
        }
    }
}
