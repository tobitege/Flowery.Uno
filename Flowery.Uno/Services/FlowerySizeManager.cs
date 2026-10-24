// CsWinRT1028: Generic collections implementing WinRT interfaces - inherent to Uno Platform, not fixable via code
#pragma warning disable CsWinRT1028

using System;
using System.Collections.Generic;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// Specifies which font size tier to use for responsive scaling.
    /// </summary>
    public enum ResponsiveFontTier
    {
        None,
        Primary,
        Secondary,
        Tertiary,
        /// <summary>
        /// Section headers - smaller than page headers, larger than body text.
        /// Use for control group titles, example section headers, etc.
        /// </summary>
        SectionHeader,
        /// <summary>
        /// Page/screen headers - the largest tier for main titles.
        /// </summary>
        Header
    }

    internal interface IDaisySizeScope
    {
        DaisySize? ChildSizeOverride { get; }
        DaisySize? ChildSizeFallback { get; }
    }

    /// <summary>
    /// Centralized size manager for DaisyUI controls (Uno/WinUI).
    /// </summary>
    public static class FlowerySizeManager
    {
        private static DaisySize _currentSize = DaisySize.Medium;

        private static readonly HashSet<SizeRegistration> LoadedControls = [];
        private static bool _useGlobalSizeByDefault = true;
        private static bool _enableGlobalAutoSize = true;

        public static readonly DependencyProperty IgnoreGlobalSizeProperty =
            DependencyProperty.RegisterAttached(
                "IgnoreGlobalSize",
                typeof(bool),
                typeof(FlowerySizeManager),
                new PropertyMetadata(false, OnIgnoreGlobalSizeChanged));

        public static bool GetIgnoreGlobalSize(DependencyObject control) =>
            (bool)control.GetValue(IgnoreGlobalSizeProperty);

        public static void SetIgnoreGlobalSize(DependencyObject control, bool value)
        {
            var changesInheritedPolicy = GetIgnoreGlobalSize(control) == value && ShouldIgnoreGlobalSize(control) != value;
            control.SetValue(IgnoreGlobalSizeProperty, value);
            // A new local false changes inherited policy even when the native effective value stays false.
            if (changesInheritedPolicy)
                RefreshAllSizes();
        }

        private static void OnIgnoreGlobalSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            // A parent setting can change the effective policy of any loaded descendant.
            RefreshAllSizes();
        }

        /// <summary>
        /// Resolves an authored Size without changing its dependency-property value.
        /// Auto uses a composite override, the enabled global size, or the nearest composite/control fallback.
        /// </summary>
        public static DaisySize ResolveSize(FrameworkElement owner, DaisySize requested, DaisySize fallback = DaisySize.Medium)
        {
            if (requested != DaisySize.Auto)
                return requested;

            DaisySize? scopeFallback = null;
            var parent = VisualTreeHelper.GetParent(owner) ?? owner.Parent;
            while (parent != null)
            {
                if (parent is IDaisySizeScope scope)
                {
                    if (scope.ChildSizeOverride is { } inheritedSize)
                        return inheritedSize;
                    scopeFallback ??= scope.ChildSizeFallback;
                }
                parent = VisualTreeHelper.GetParent(parent) ?? (parent as FrameworkElement)?.Parent;
            }

            return UseGlobalSizeByDefault && !ShouldIgnoreGlobalSize(owner) ? _currentSize : scopeFallback ?? fallback;
        }

        internal static void RefreshSizeScope(FrameworkElement scope)
        {
            foreach (var registration in new List<SizeRegistration>(LoadedControls))
            {
                if (!LoadedControls.Contains(registration))
                    continue;

                var parent = VisualTreeHelper.GetParent(registration.Owner) ?? registration.Owner.Parent;
                while (parent != null)
                {
                    if (ReferenceEquals(parent, scope))
                    {
                        registration.Refresh();
                        break;
                    }
                    parent = VisualTreeHelper.GetParent(parent) ?? (parent as FrameworkElement)?.Parent;
                }
            }
        }

        internal static void Register(FrameworkElement owner, Action refresh, Func<DaisySize> getResolvedSize)
        {
            // The element owns these event handlers. Only loaded registrations are held statically.
            _ = new SizeRegistration(owner, refresh, getResolvedSize);
        }

        private sealed class SizeRegistration
        {
            private readonly Action _refresh;
            private readonly Func<DaisySize> _getResolvedSize;

            public SizeRegistration(FrameworkElement owner, Action refresh, Func<DaisySize> getResolvedSize)
            {
                Owner = owner;
                _refresh = refresh;
                _getResolvedSize = getResolvedSize;
                owner.Loaded += OnLoaded;
                owner.Unloaded += OnUnloaded;
            }

            private void OnLoaded(object sender, RoutedEventArgs args)
            {
                LoadedControls.Add(this);
                Refresh();
            }

            private void OnUnloaded(object sender, RoutedEventArgs args) => LoadedControls.Remove(this);

            public FrameworkElement Owner { get; }

            public DaisySize ResolvedSize => _getResolvedSize();

            public void Refresh() => _refresh();
        }

        /// <summary>
        /// Checks if this control or any of its ancestors has IgnoreGlobalSize explicitly set.
        /// Stops at the first explicit setting and returns that value.
        /// This allows parent containers to opt-out and child controls to opt back in.
        /// </summary>
        public static bool ShouldIgnoreGlobalSize(DependencyObject control)
        {
            if (control == null) return false;

            var current = control;
            try
            {
                while (current != null)
                {
                    // Check if property is explicitly set (not default)
                    var localValue = current.ReadLocalValue(IgnoreGlobalSizeProperty);
                    if (localValue != DependencyProperty.UnsetValue)
                    {
                        // Property is explicitly set - return its value and stop walking
                        return GetIgnoreGlobalSize(current);
                    }

                    // Fallback: Check if it's a FrameworkElement's Parent property if VisualTreeHelper fails
                    DependencyObject? next = null;
                    try
                    {
                        next = VisualTreeHelper.GetParent(current);
                    }
                    catch { }

                    if (next == null && current is FrameworkElement fe)
                    {
                        try { next = fe.Parent; } catch { }
                    }
                    current = next;
                }
            }
            catch
            {
                // If anything fails during tree walk, default to not ignoring
            }
            return false; // No explicit setting found, use global size
        }

        public static readonly DependencyProperty ResponsiveFontProperty =
            DependencyProperty.RegisterAttached(
                "ResponsiveFont",
                typeof(ResponsiveFontTier),
                typeof(FlowerySizeManager),
                new PropertyMetadata(ResponsiveFontTier.None, OnResponsiveFontChanged));

        private static readonly HashSet<TextBlock> ResponsiveTextBlocks = [];

        /// <summary>
        /// Event raised when the global size changes.
        /// </summary>
        public static event EventHandler<DaisySize>? SizeChanged;

        /// <summary>
        /// Gets the currently active global size.
        /// </summary>
        public static DaisySize CurrentSize => _currentSize;

        /// <summary>
        /// Gets the standard single-line control height for a Daisy size.
        /// These values are the code-side counterpart of the DaisySize*Height tokens.
        /// </summary>
        public static double GetControlHeight(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 24d,
            DaisySize.Small => 28d,
            DaisySize.Medium => 32d,
            DaisySize.Large => 34d,
            DaisySize.ExtraLarge => 36d,
            _ => 32d
        };

        /// <summary>
        /// Gets or sets whether controls should use the global size by default.
        /// Default is true (opt-out behavior).
        /// </summary>
        public static bool UseGlobalSizeByDefault
        {
            get => _useGlobalSizeByDefault;
            set
            {
                if (_useGlobalSizeByDefault == value)
                    return;
                _useGlobalSizeByDefault = value;
                RefreshAllSizes();
            }
        }

        /// <summary>
        /// When true (default), ApplySize automatically refreshes loaded controls.
        /// When false, call RefreshAllSizes to apply the current size explicitly.
        /// This does not change how Auto resolves or how newly loaded controls initialize.
        /// </summary>
        public static bool EnableGlobalAutoSize
        {
            get => _enableGlobalAutoSize;
            set
            {
                if (_enableGlobalAutoSize == value)
                    return;
                _enableGlobalAutoSize = value;
                if (value)
                    RefreshAllSizes();
            }
        }

        public static ResponsiveFontTier GetResponsiveFont(DependencyObject control) =>
            (ResponsiveFontTier)control.GetValue(ResponsiveFontProperty);

        public static void SetResponsiveFont(DependencyObject control, ResponsiveFontTier value) =>
            control.SetValue(ResponsiveFontProperty, value);

        private static void OnResponsiveFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
                return;

            var newTier = (ResponsiveFontTier)(e.NewValue ?? ResponsiveFontTier.None);
            var oldTier = (ResponsiveFontTier)(e.OldValue ?? ResponsiveFontTier.None);

            if (newTier != ResponsiveFontTier.None && oldTier == ResponsiveFontTier.None)
            {
                textBlock.Loaded += OnResponsiveTextBlockLoaded;
                textBlock.Unloaded += OnResponsiveTextBlockUnloaded;
                if (textBlock.IsLoaded)
                    ResponsiveTextBlocks.Add(textBlock);
                ApplyFontSizeToControl(textBlock, newTier, _currentSize);
            }
            else if (newTier == ResponsiveFontTier.None && oldTier != ResponsiveFontTier.None)
            {
                ResponsiveTextBlocks.Remove(textBlock);
                textBlock.Loaded -= OnResponsiveTextBlockLoaded;
                textBlock.Unloaded -= OnResponsiveTextBlockUnloaded;
            }
            else if (newTier != ResponsiveFontTier.None)
            {
                ApplyFontSizeToControl(textBlock, newTier, _currentSize);
            }
        }

        private static void OnResponsiveTextBlockLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                ResponsiveTextBlocks.Add(textBlock);
                if (!ShouldIgnoreGlobalSize(textBlock))
                    ApplyFontSizeToControl(textBlock, GetResponsiveFont(textBlock), _currentSize);
            }
        }

        private static void OnResponsiveTextBlockUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
            {
                ResponsiveTextBlocks.Remove(tb);
            }
        }

        /// <summary>
        /// Selects a concrete global size and optionally refreshes loaded controls.
        /// Authored Size values, styles and bindings are never replaced.
        /// </summary>
        public static void ApplySize(DaisySize size)
        {
            if (size < DaisySize.ExtraSmall || size > DaisySize.ExtraLarge)
                throw new ArgumentOutOfRangeException(nameof(size), size, "A global size must be a concrete size.");

            if (_currentSize == size)
                return;

            // Read the old resolution now: authored Size callbacks may have changed it since the last global update.
            List<(SizeRegistration Registration, DaisySize Size)> previousSizes = [];
            if (EnableGlobalAutoSize)
            {
                foreach (var registration in LoadedControls)
                    previousSizes.Add((registration, registration.ResolvedSize));
            }

            _currentSize = size;
            if (EnableGlobalAutoSize)
            {
                foreach (var (registration, previousSize) in previousSizes)
                {
                    if (LoadedControls.Contains(registration) && registration.ResolvedSize != previousSize)
                        registration.Refresh();
                }
                UpdateResponsiveTextBlocks(size);
            }
            SizeChanged?.Invoke(null, size);
        }

        /// <summary>
        /// Gets or sets the application's main window reference.
        /// Size registration also covers controls in other windows and popup roots.
        /// </summary>
        public static Microsoft.UI.Xaml.Window? MainWindow { get; set; }

        /// <summary>
        /// Refreshes the sizing of all loaded controls, even when automatic refresh is disabled.
        /// </summary>
        public static void RefreshAllSizes()
        {
            foreach (var registration in new List<SizeRegistration>(LoadedControls))
            {
                if (LoadedControls.Contains(registration))
                    registration.Refresh();
            }
            UpdateResponsiveTextBlocks(_currentSize);
        }

        private static void UpdateResponsiveTextBlocks(DaisySize size)
        {
            foreach (var tb in ResponsiveTextBlocks)
            {
                // Respect IgnoreGlobalSize on the TextBlock or its ancestors
                if (ShouldIgnoreGlobalSize(tb))
                    continue;

                var tier = GetResponsiveFont(tb);
                if (tier != ResponsiveFontTier.None)
                {
                    ApplyFontSizeToControl(tb, tier, size);
                }
            }
        }

        private static void ApplyFontSizeToControl(TextBlock textBlock, ResponsiveFontTier tier, DaisySize size)
        {
            textBlock.FontSize = GetFontSizeForTier(tier, size);
        }

        /// <summary>
        /// Gets the font size for a given tier and size, reading from design tokens.
        /// </summary>
        public static double GetFontSizeForTier(ResponsiveFontTier tier, DaisySize size)
        {
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(size);
            var tierKey = tier switch
            {
                ResponsiveFontTier.Primary => "",
                ResponsiveFontTier.Secondary => "Secondary",
                ResponsiveFontTier.Tertiary => "Tertiary",
                ResponsiveFontTier.SectionHeader => "SectionHeader",
                ResponsiveFontTier.Header => "Header",
                _ => ""
            };

            // Token format: DaisySize{SizeKey}{TierKey}FontSize
            // e.g., DaisySizeMediumFontSize, DaisySizeLargeSecondaryFontSize
            var tokenKey = $"DaisySize{sizeKey}{tierKey}FontSize";
            var fontSize = DaisyResourceLookup.GetDouble(tokenKey);

            // Fallback defaults if token not found
            if (fontSize > 0)
                return fontSize;

            return tier switch
            {
                ResponsiveFontTier.Primary => 14,
                ResponsiveFontTier.Secondary => 12,
                ResponsiveFontTier.Tertiary => 11,
                ResponsiveFontTier.SectionHeader => 16,
                ResponsiveFontTier.Header => 20,
                _ => 14
            };
        }

        /// <summary>
        /// Gets the default sidebar width for the given size.
        /// </summary>
        public static double GetSidebarWidth(DaisySize size)
        {
            return size switch
            {
                DaisySize.ExtraSmall => 190d,
                DaisySize.Small => 205d,
                DaisySize.Medium => 220d,
                DaisySize.Large => 235d,
                DaisySize.ExtraLarge => 250d,
                _ => 220d
            };
        }

        /// <summary>
        /// Resets the global size to Small.
        /// </summary>
        public static void Reset()
        {
            ApplySize(DaisySize.Small);
        }
    }
}
