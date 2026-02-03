// CsWinRT1028: Generic collections implementing WinRT interfaces - inherent to Uno Platform, not fixable via code
#pragma warning disable CsWinRT1028

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

    /// <summary>
    /// Centralized size manager for DaisyUI controls (Uno/WinUI).
    /// </summary>
    public static class FlowerySizeManager
    {
        private static DaisySize _currentSize = DaisySize.Small;

        // Cache for SizeProperty DependencyProperty by type
        private static readonly Dictionary<Type, DependencyProperty?> _sizeDPCache = [];

        public static readonly DependencyProperty IgnoreGlobalSizeProperty =
            DependencyProperty.RegisterAttached(
                "IgnoreGlobalSize",
                typeof(bool),
                typeof(FlowerySizeManager),
                new PropertyMetadata(false));

        public static bool GetIgnoreGlobalSize(DependencyObject control) =>
            (bool)control.GetValue(IgnoreGlobalSizeProperty);

        public static void SetIgnoreGlobalSize(DependencyObject control, bool value) =>
            control.SetValue(IgnoreGlobalSizeProperty, value);

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
                        return (bool)localValue;
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
        /// Gets or sets whether controls should use the global size by default.
        /// Default is true (opt-out behavior).
        /// </summary>
        public static bool UseGlobalSizeByDefault { get; set; } = true;

        /// <summary>
        /// When true (default), ApplySize() automatically propagates to all controls
        /// with a Size property of type DaisySize in the visual tree.
        /// Use IgnoreGlobalSize="True" on individual controls to opt-out.
        /// </summary>
        public static bool EnableGlobalAutoSize { get; set; } = true;

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
                ResponsiveTextBlocks.Add(textBlock);
                textBlock.Unloaded += OnResponsiveTextBlockUnloaded;
                ApplyFontSizeToControl(textBlock, newTier, _currentSize);
            }
            else if (newTier == ResponsiveFontTier.None && oldTier != ResponsiveFontTier.None)
            {
                ResponsiveTextBlocks.Remove(textBlock);
                textBlock.Unloaded -= OnResponsiveTextBlockUnloaded;
            }
            else if (newTier != ResponsiveFontTier.None)
            {
                ApplyFontSizeToControl(textBlock, newTier, _currentSize);
            }
        }

        private static void OnResponsiveTextBlockUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
            {
                ResponsiveTextBlocks.Remove(tb);
                tb.Unloaded -= OnResponsiveTextBlockUnloaded;
            }
        }

        /// <summary>
        /// Apply a global size to all subscribing controls.
        /// When EnableGlobalAutoSize is true, also propagates to all controls
        /// with a Size property in the visual tree.
        /// </summary>
        public static void ApplySize(DaisySize size)
        {
            if (_currentSize == size)
                return;

            _currentSize = size;
            UpdateResponsiveTextBlocks(size);
            SizeChanged?.Invoke(null, size);

            if (EnableGlobalAutoSize)
            {
                PropagateToVisualTree();
            }
        }

        /// <summary>
        /// The main window reference for visual tree propagation.
        /// Set this in your App.xaml.cs after creating the window.
        /// </summary>
        public static Microsoft.UI.Xaml.Window? MainWindow { get; set; }

        /// <summary>
        /// Forces propagation of the current size to all controls in the visual tree.
        /// Call this after the visual tree is fully loaded (e.g., in Window.Loaded).
        /// </summary>
        public static void RefreshAllSizes()
        {
            if (EnableGlobalAutoSize)
            {
                PropagateToVisualTree();
            }
        }

        /// <summary>
        /// Propagates the current size to all controls with a Size property
        /// in the visual tree.
        /// </summary>
        private static void PropagateToVisualTree()
        {
            try
            {
                if (MainWindow?.Content is FrameworkElement root)
                {
                    PropagateSize(root, _currentSize);
                }
            }
            catch
            {
                // Silently ignore errors during propagation
            }
        }

        // Cache property lookups for performance
        private static readonly Dictionary<Type, PropertyInfo?> _sizePropertyCache = [];

        /// <summary>
        /// Iteratively propagates size to all controls in the visual tree to avoid StackOverflow.
        /// </summary>
        private static void PropagateSize(DependencyObject root, DaisySize size)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var element = queue.Dequeue();

                if (element is FrameworkElement fe && ShouldIgnoreGlobalSize(fe))
                {
                    // This branch has opted out - stop propagation for this branch
                    continue;
                }

                if (element is FrameworkElement currentFe)
                {
                    TrySetSizeProperty(currentFe, size);
                }

                // Add children to queue
                int count = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    if (child != null)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to set the Size property on a control if it exists and is DaisySize.
        /// Respects explicitly-set local values (from XAML or code) by not overwriting them.
        /// Uses reflection with caching for performance.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers",
            Justification = "DaisyUI controls that support Size property are always preserved. The cache handles missing members gracefully.")]
        private static void TrySetSizeProperty(FrameworkElement element, DaisySize size)
        {
            var type = element.GetType();

            if (!_sizePropertyCache.TryGetValue(type, out var sizeProp))
            {
                // Cache the property lookup (null if not found)
                sizeProp = type.GetProperty("Size", BindingFlags.Public | BindingFlags.Instance);
                if (sizeProp?.PropertyType != typeof(DaisySize) || !sizeProp.CanWrite)
                {
                    sizeProp = null;
                }
                _sizePropertyCache[type] = sizeProp;
            }

            if (sizeProp == null)
                return;

            try
            {
                // Check if the control has an explicit SizeProperty DependencyProperty
                // If so, check if a local value was set (in XAML or code) and respect it
                if (!_sizeDPCache.TryGetValue(type, out var dp))
                {
                    var sizeField = type.GetField("SizeProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    dp = sizeField?.GetValue(null) as DependencyProperty;
                    _sizeDPCache[type] = dp;
                }

                if (dp != null)
                {
                    var localValue = element.ReadLocalValue(dp);
                    if (localValue != DependencyProperty.UnsetValue)
                    {
                        // Control has an explicit Size set - don't override
                        return;
                    }

                    // Use SetValue(dp, value) as it's more reliable for DPs than reflection set
                    element.SetValue(dp, size);
                }
                else
                {
                    // Fallback to reflection for non-DP properties (rare)
                    sizeProp.SetValue(element, size);
                }
            }
            catch
            {
                // Silently ignore errors setting property
            }
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
                ResponsiveFontTier.Primary => 10,
                ResponsiveFontTier.Secondary => 9,
                ResponsiveFontTier.Tertiary => 8,
                ResponsiveFontTier.SectionHeader => 12,
                ResponsiveFontTier.Header => 14,
                _ => 10
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
        /// Resets the global size to Small (the default).
        /// </summary>
        public static void Reset()
        {
            ApplySize(DaisySize.Small);
        }
    }
}
