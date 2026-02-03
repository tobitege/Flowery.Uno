using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Flowery.Uno.Gallery.Localization;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class DataInputExamples : ScrollableExamplePage
    {
        public string[] TagPickerTags { get; } =
        [
            "UI",
            "UX",
            "Accessibility",
            "Performance",
            "Bug",
            "Feature",
            "Docs",
            "Refactor",
            "Testing",
            "Release"
        ];

        private static readonly string[] TagPickerLibraryDefaults =
        [
            "Uno Platform",
            "C#",
            "WinUI",
            "XAML",
            "MVUX",
            "Skia",
            "WASM",
            "Hot Reload"
        ];

        public List<string> TagPickerLibraryTags { get; private set; } = CreateTagPickerLibraryTags();

        public GalleryLocalization Localization { get; } = GalleryLocalization.Instance;

        public DataInputExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            if (TagPicker != null)
            {
                TagPicker.Tags = TagPickerTags;
            }

            if (TagPickerLibrary != null)
            {
                TagPickerLibrary.Tags = TagPickerLibraryTags;
                TagPickerLibrary.SelectedTags = TagPickerLibraryTags;
            }

            // XAML compiler can't reliably assign numeric literals to decimal/Nullable<decimal> DPs.
            // Configure the NumericUpDown demos in code-behind.
            NumericUpDownDecimalDemo.Minimum = -100m;
            NumericUpDownDecimalDemo.Maximum = 100m;
            NumericUpDownDecimalDemo.Increment = 1m;
            NumericUpDownDecimalDemo.Value = 42m;

            NumericUpDownCurrencyDemo.Minimum = 0m;
            NumericUpDownCurrencyDemo.Maximum = 9999m;
            NumericUpDownCurrencyDemo.Increment = 0.25m;
            NumericUpDownCurrencyDemo.Value = 19.99m;

            NumericUpDownHexDemo.Minimum = 0m;
            NumericUpDownHexDemo.Maximum = 65535m;
            NumericUpDownHexDemo.Value = 255m;

            NumericUpDownBinaryDemo.Minimum = 0m;
            NumericUpDownBinaryDemo.Maximum = 1024m;
            NumericUpDownBinaryDemo.Value = 42m;

            NumericUpDownColorHexDemo.Minimum = 0m;
            NumericUpDownColorHexDemo.Maximum = 16777215m;
            NumericUpDownColorHexDemo.Value = 16711935m; // #FF00FF

            NumericUpDownColorHexLowerDemo.Minimum = 0m;
            NumericUpDownColorHexLowerDemo.Maximum = 16777215m;
            NumericUpDownColorHexLowerDemo.Value = 65535m; // #00ffff (cyan)

            NumericUpDownIpv4Demo.Minimum = 0m;
            NumericUpDownIpv4Demo.Maximum = 4294967295m;
            NumericUpDownIpv4Demo.Value = 3232235777m; // 192.168.1.1

            NumericUpDownVariantSmall.Value = 12m;
            NumericUpDownVariantMedium.Value = 12m;
            NumericUpDownVariantLarge.Value = 12m;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnSlideCompleted(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Slide completed!");
        }

        private void OnTagPickerLibraryReset(object sender, RoutedEventArgs e)
        {
            TagPickerLibraryTags = CreateTagPickerLibraryTags();

            if (TagPickerLibrary != null)
            {
                TagPickerLibrary.Tags = TagPickerLibraryTags;
                TagPickerLibrary.SelectedTags = TagPickerLibraryTags;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            GalleryLocalization.CultureChanged += OnCultureChanged;
            RefreshLocalizationBindings();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            GalleryLocalization.CultureChanged -= OnCultureChanged;
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            RefreshLocalizationBindings();
        }

        private void RefreshLocalizationBindings()
        {
            if (MainScrollViewer == null)
                return;

            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(RefreshLocalizationBindingsCore);
                return;
            }

            RefreshLocalizationBindingsCore();
        }

        private void RefreshLocalizationBindingsCore()
        {
            if (MainScrollViewer == null)
                return;

            MainScrollViewer.DataContext = null;
            MainScrollViewer.DataContext = Localization;
        }

        private static List<string> CreateTagPickerLibraryTags()
        {
            return [.. TagPickerLibraryDefaults];
        }
    }
}
