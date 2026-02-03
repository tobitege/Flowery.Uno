using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class DividerExamples : ScrollableExamplePage
    {
        public DividerExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;
    }
}
