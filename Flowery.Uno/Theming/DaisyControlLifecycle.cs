using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Manages theme and size change subscriptions for Daisy controls.
    /// Use via composition, not inheritance.
    /// </summary>
    public sealed class DaisyControlLifecycle
    {
        private readonly FrameworkElement _owner;
        private readonly Action _applyAll;
        private readonly Func<DaisySize> _getSize;
        private readonly Action<DaisySize> _setSize;
        private readonly bool _subscribeSizeChanges;

        public DaisyControlLifecycle(
            FrameworkElement owner,
            Action applyAll,
            Func<DaisySize> getSize,
            Action<DaisySize> setSize,
            bool handleLifecycleEvents = true,
            bool subscribeSizeChanges = true)
        {
            _owner = owner;
            _applyAll = applyAll;
            _getSize = getSize;
            _setSize = setSize;
            _subscribeSizeChanges = subscribeSizeChanges;

            // Note: Do NOT apply global size here in constructor!
            // At this point, XAML attribute processing hasn't happened yet,
            // so IgnoreGlobalSize and explicit Size values aren't set.
            // The initial size application happens in HandleLoaded() instead.

            // Hook lifecycle events if requested
            if (handleLifecycleEvents)
            {
                owner.Loaded += OnLoaded;
                owner.Unloaded += OnUnloaded;
            }
        }

        /// <summary>
        /// Called when auto-handling lifecycle events. Subscribes to theme/size changes.
        /// </summary>
        public void HandleLoaded()
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            if (_subscribeSizeChanges)
            {
                FlowerySizeManager.SizeChanged += OnGlobalSizeChanged;

                // Apply global size on loaded (not constructor) so XAML properties are set
                // Check if control has an explicit Size value set - if so, respect it
                if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.ShouldIgnoreGlobalSize(_owner))
                {
                    // Only apply if Size wasn't explicitly set in XAML
                    var sizeDP = TryGetSizeProperty(_owner);

                    if (sizeDP == null || _owner.ReadLocalValue(sizeDP) == DependencyProperty.UnsetValue)
                    {
                        _setSize(FlowerySizeManager.CurrentSize);
                    }
                }
            }
            _applyAll();
        }

        public void HandleUnloaded()
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            if (_subscribeSizeChanges)
            {
                FlowerySizeManager.SizeChanged -= OnGlobalSizeChanged;
            }
        }

        // Event handlers for automatic lifecycle management
        private void OnLoaded(object sender, RoutedEventArgs e) => HandleLoaded();
        private void OnUnloaded(object sender, RoutedEventArgs e) => HandleUnloaded();

        private void OnThemeChanged(object? sender, string themeName) => _applyAll();

        private void OnGlobalSizeChanged(object? sender, DaisySize size)
        {
            // Use ShouldIgnoreGlobalSize to check this control AND ancestors
            if (FlowerySizeManager.ShouldIgnoreGlobalSize(_owner))
                return;

            if (FlowerySizeManager.UseGlobalSizeByDefault)
            {
                _setSize(size);
            }
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2075",
            Justification = "SizeProperty lookup is optional and guarded; controls without it fallback safely.")]
        private static DependencyProperty? TryGetSizeProperty(FrameworkElement owner)
        {
            try
            {
                return owner.GetType().GetField(
                        "SizeProperty",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
                    ?.GetValue(null) as DependencyProperty;
            }
            catch
            {
                return null;
            }
        }
    }
}
