using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class PatternsExamples : ScrollableExamplePage
    {
        public PatternsExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;
    }
}
