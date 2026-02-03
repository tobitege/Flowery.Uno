using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Flowery.Controls;
using Flowery.Uno.Gallery.Localization;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class DataDisplayExamples : ScrollableExamplePage
    {
        private int _animatedNumberValue = 42;

        public List<DaisyContributionDay> ContributionData { get; } = BuildContributionData();
        public GalleryLocalization Localization { get; } = GalleryLocalization.Instance;

        public DataDisplayExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // XAML compiler can't reliably assign numeric literals to Nullable<decimal> for custom controls.
            // Set demo values in code-behind instead.
            NumberFlowDemo1.Value = 123m;
            NumberFlowDemo2.Value = 0m;
            NumberFlowDemo3.Value = 19.99m;
            NumberFlowDemo4.Value = 28m;
            NumberFlowDemo4.Minimum = 0m;
            NumberFlowDemo4.Maximum = 999m;
            NumberFlowDemo3.Step = 0.25m;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void AnimatedNumber_Decrease_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            _animatedNumberValue = Math.Max(0, _animatedNumberValue - 1);
            if (AnimatedNumberDemo != null)
                AnimatedNumberDemo.Value = _animatedNumberValue;
        }

        private void AnimatedNumber_Increase_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            _animatedNumberValue++;
            if (AnimatedNumberDemo != null)
                AnimatedNumberDemo.Value = _animatedNumberValue;
        }

        private void NumberFlowSimple_Decrease_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            NumberFlowDemo1?.Decrement();
        }

        private void NumberFlowSimple_Increase_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            NumberFlowDemo1?.Increment();
        }

        private void NumberFlowCurrency_Decrease_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            NumberFlowDemo3?.Decrement();
        }

        private void NumberFlowCurrency_Increase_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            NumberFlowDemo3?.Increment();
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

        private static List<DaisyContributionDay> BuildContributionData()
        {
            var rnd = new Random(12345);
            var days = new List<DaisyContributionDay>();

            // Synthetic but stable dataset for demo purposes.
            var year = 2025;
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);

            var current = start;
            while (current <= end)
            {
                // Bias toward low activity, with occasional spikes.
                var roll = rnd.NextDouble();
                int count = roll < 0.65 ? 0 : roll < 0.85 ? rnd.Next(1, 4) : rnd.Next(4, 15);

                int level = count switch
                {
                    0 => 0,
                    <= 2 => 1,
                    <= 4 => 2,
                    <= 8 => 3,
                    _ => 4
                };

                days.Add(new DaisyContributionDay
                {
                    Date = current.Date,
                    Count = count,
                    Level = level
                });

                current = current.AddDays(1);
            }
            return days;
        }
    }
}
