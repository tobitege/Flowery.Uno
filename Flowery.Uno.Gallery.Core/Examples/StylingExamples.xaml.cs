using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    /// <summary>
    /// Demonstrates lightweight styling for Daisy controls using resource overrides.
    /// </summary>
    public sealed partial class StylingExamples : ScrollableExamplePage
    {
        public StylingExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // Add tabs to demonstrate styling
            DefaultTabs.AddTab("Tab 1", null);
            DefaultTabs.AddTab("Tab 2", null);
            DefaultTabs.AddTab("Tab 3", null);

            StyledTabs.AddTab("Home", null);
            StyledTabs.AddTab("Profile", null);
            StyledTabs.AddTab("Settings", null);
        }
    }
}
