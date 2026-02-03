using System;
using System.Collections.Generic;
using Flowery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Controls
{
    public sealed partial class DaisyMenuFlyoutItem : MenuFlyoutItem
    {
        public static readonly DependencyProperty LocalizationKeyProperty =
            DependencyProperty.Register(
                nameof(LocalizationKey),
                typeof(string),
                typeof(DaisyMenuFlyoutItem),
                new PropertyMetadata(string.Empty, OnLocalizationChanged));

        public static readonly DependencyProperty LocalizationSourceProperty =
            DependencyProperty.Register(
                nameof(LocalizationSource),
                typeof(object),
                typeof(DaisyMenuFlyoutItem),
                new PropertyMetadata(null, OnLocalizationChanged));

        public DaisyMenuFlyoutItem()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public string LocalizationKey
        {
            get => (string)GetValue(LocalizationKeyProperty);
            set => SetValue(LocalizationKeyProperty, value);
        }

        public object? LocalizationSource
        {
            get => GetValue(LocalizationSourceProperty);
            set => SetValue(LocalizationSourceProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateText();
        }

        private static void OnLocalizationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMenuFlyoutItem item)
            {
                item.UpdateText();
            }
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            FloweryLocalization.CultureChanged += OnCultureChanged;
            UpdateText();
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            FloweryLocalization.CultureChanged -= OnCultureChanged;
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (string.IsNullOrWhiteSpace(LocalizationKey))
                return;

            if (LocalizationSource is FloweryLocalization localization)
            {
                Text = localization[LocalizationKey];
                return;
            }

            if (LocalizationSource is Func<string, string> resolver)
            {
                Text = resolver(LocalizationKey) ?? LocalizationKey;
                return;
            }

            if (LocalizationSource is IDictionary<string, string> dict && dict.TryGetValue(LocalizationKey, out var value))
            {
                Text = value;
                return;
            }

            Text = FloweryLocalization.Instance[LocalizationKey];
        }
    }
}
