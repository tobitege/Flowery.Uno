using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class ThemingExamples : ScrollableExamplePage
    {
        public ThemingExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;
    }
}
