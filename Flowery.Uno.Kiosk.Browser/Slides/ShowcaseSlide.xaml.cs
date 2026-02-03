using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Uno.Kiosk.Browser.Slides
{
    public sealed partial class ShowcaseSlide : UserControl
    {
        public ShowcaseSlide()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Wire up color wheel to update preview
            ColorWheel.ColorChanged += (s, args) =>
            {
                ColorPreview.Background = new SolidColorBrush(args.Color);
                HexText.Text = $"#{args.Color.R:X2}{args.Color.G:X2}{args.Color.B:X2}";
            };
        }
    }
}
