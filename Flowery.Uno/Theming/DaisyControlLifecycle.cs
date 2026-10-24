using System;
using Microsoft.UI.Xaml;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Manages theme change subscriptions for Daisy controls.
    /// Use via composition, not inheritance.
    /// </summary>
    public sealed class DaisyControlLifecycle
    {
        private readonly Action _applyAll;
        private bool _isLoaded;

        public DaisyControlLifecycle(
            FrameworkElement owner,
            Action applyAll,
            bool handleLifecycleEvents = true)
        {
            _applyAll = applyAll;

            // Hook lifecycle events if requested
            if (handleLifecycleEvents)
            {
                owner.Loaded += OnLoaded;
                owner.Unloaded += OnUnloaded;
            }
        }

        /// <summary>
        /// Called when auto-handling lifecycle events. Subscribes to theme changes.
        /// </summary>
        public void HandleLoaded()
        {
            if (_isLoaded)
                return;

            _isLoaded = true;
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            _applyAll();
        }

        public void HandleUnloaded()
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            _isLoaded = false;
        }

        // Event handlers for automatic lifecycle management
        private void OnLoaded(object sender, RoutedEventArgs e) => HandleLoaded();
        private void OnUnloaded(object sender, RoutedEventArgs e) => HandleUnloaded();

        private void OnThemeChanged(object? sender, string themeName) => _applyAll();

    }
}
