using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class LayoutExamples : ScrollableExamplePage
    {
        public LayoutExamples()
        {
            InitializeComponent();
            LoadDeferredSections();
#if WINDOWS
            if (DemoDrawer != null)
            {
                DemoDrawer.LightDismissOverlayMode = LightDismissOverlayMode.Off;
            }
#endif
            Loaded += LayoutExamples_Loaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void LayoutExamples_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            // Hide complex mask shapes on non-Skia platforms (they require Composition API clipping)
#if !__SKIA__
            if (MaskHeartExample != null) MaskHeartExample.Visibility = Visibility.Collapsed;
            if (MaskHexagonExample != null) MaskHexagonExample.Visibility = Visibility.Collapsed;
            if (MaskDiamondExample != null) MaskDiamondExample.Visibility = Visibility.Collapsed;
            if (MaskTriangleExample != null) MaskTriangleExample.Visibility = Visibility.Collapsed;
#endif
        }

        private void LoadDeferredSections()
        {
            TryLoadSection(nameof(SectionDrawer), "drawer");
            TryLoadSection(nameof(SectionHero), "hero");
            TryLoadSection(nameof(SectionIndicator), "indicator");
            TryLoadSection(nameof(SectionJoin), "join");
            TryLoadSection(nameof(SectionMask), "mask");
            TryLoadSection(nameof(SectionMockup), "mockup");
            TryLoadSection(nameof(SectionStack), "stack");
        }

        private void TryLoadSection(string elementName, string sectionId)
        {
            try
            {
                var element = FindName(elementName);
                if (element == null)
                {
                    GalleryDiagnostics.Log($"{DateTimeOffset.Now:O} [LayoutExamples] Section '{sectionId}' returned null.");
                }
            }
            catch (Exception ex)
            {
                var message = $"{DateTimeOffset.Now:O} [LayoutExamples] Section '{sectionId}' failed: {ex}";
                GalleryDiagnostics.Log(message);
            }
        }

        private void DrawerToggle_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            DemoDrawer?.Toggle();
        }

        private void DrawerClose_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            DemoDrawer?.Close();
        }
    }
}
