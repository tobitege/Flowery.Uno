using Flowery.Controls;
using Flowery.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class NavigationExamples : ScrollableExamplePage
    {
        public IReadOnlyList<string> BreadcrumbBarItems { get; } = new[]
        {
            "Home",
            "Library",
            "Data"
        };

        public IReadOnlyList<BreadcrumbBarIconItem> BreadcrumbBarIconItems { get; } = new[]
        {
            new BreadcrumbBarIconItem("Home", FloweryPathHelpers.GetIconPathData("DaisyIconHome")),
            new BreadcrumbBarIconItem("Library", FloweryPathHelpers.GetIconPathData("DaisyIconFolder")),
            new BreadcrumbBarIconItem("Data", FloweryPathHelpers.GetIconPathData("DaisyIconDocument"))
        };

        public sealed class BreadcrumbBarIconItem
        {
            public BreadcrumbBarIconItem(string text, string? iconData)
            {
                Text = text;
                IconData = iconData;
            }

            public string Text { get; }

            public string? IconData { get; }
        }

        public NavigationExamples()
        {
            InitializeComponent();
        }

        private void OnPhotoTabActionClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int index))
            {
                PhotoTabImage0.Opacity = index == 0 ? 1 : 0;
                PhotoTabImage1.Opacity = index == 1 ? 1 : 0;
                PhotoTabImage2.Opacity = index == 2 ? 1 : 0;
            }
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void Dock_ItemSelected(object sender, DockItemSelectedEventArgs e)
        {
            _ = sender;

            var name = e.Index switch
            {
                0 => "Home",
                1 => "User",
                2 => "Settings",
                3 => "Palette",
                _ => $"Item {e.Index + 1}"
            };

            DockSelectionText.Text = $"Selected: {name}";
        }

        private void Pagination_PageChanged(object sender, int page)
        {
            _ = sender;

            if (PaginationStatusText != null)
                PaginationStatusText.Text = $"Current page: {page}";
        }
        private void OnStepsPrevious(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (InteractiveSteps.SelectedIndex > 0)
                InteractiveSteps.SelectedIndex--;
        }

        private void OnStepsNext(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (InteractiveSteps.SelectedIndex < InteractiveSteps.ItemCount - 1)
                InteractiveSteps.SelectedIndex++;
        }

        private void OnStepsReset(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            InteractiveSteps.SelectedIndex = 0;
        }

        // These event handlers are required because the DaisyTabs control exposes events for tab context menu actions.
        // Even if we don't need custom logic, we must provide handlers to satisfy the event subscriptions in XAML.
        // The discard statements (_ = ...) suppress compiler warnings about unused parameters.
#pragma warning disable CA1822 // Mark members as static
        private void OnCodeEditorCloseTab(object sender, DaisyTabItem item)
        {
            _ = sender;
            _ = item;
        }

        private void OnCodeEditorCloseOtherTabs(object sender, DaisyTabItem item)
        {
            _ = sender;
            _ = item;
        }

        private void OnCodeEditorCloseTabsToRight(object sender, DaisyTabItem item)
        {
            _ = sender;
            _ = item;
        }
#pragma warning restore CA1822

        private void OnCodeEditorTabPaletteColorChange(object sender, DaisyTabPaletteColorEventArgs e)
        {
            DaisyTabs.SetTabPaletteColor(e.Item, e.Color);
        }
    }
}
