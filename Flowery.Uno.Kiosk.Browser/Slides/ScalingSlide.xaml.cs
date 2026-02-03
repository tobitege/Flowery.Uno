using System;
using Flowery.Controls;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Kiosk.Browser.Slides
{
    public sealed partial class ScalingSlide : UserControl
    {
        private DispatcherTimer? _zoomTimer;
        private double _currentZoomPercent = 50.0;
        private const double TargetZoomPercent = 200.0;
        private const double ZoomDuration = 6.0; // seconds
        private const double UpdateIntervalMs = 100.0;
        
        public ScalingSlide()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Enable scaling and allow up to 200%
            FloweryScaleManager.IsEnabled = true;
            FloweryScaleManager.MaxScaleFactor = 2.0; // Required to allow scaling above 100%
            FloweryScaleManager.ScaleFactorChanged += OnScaleFactorChanged;
            
            // Start at 50% zoom
            _currentZoomPercent = 50.0;
            ApplyZoom();
            
            // Start the zoom animation timer
            var zoomStep = (TargetZoomPercent - 50.0) / (ZoomDuration * 1000.0 / UpdateIntervalMs);
            
            _zoomTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UpdateIntervalMs)
            };
            
            _zoomTimer.Tick += (s, args) =>
            {
                _currentZoomPercent += zoomStep;
                
                if (_currentZoomPercent >= TargetZoomPercent)
                {
                    _currentZoomPercent = TargetZoomPercent;
                    _zoomTimer?.Stop();
                }
                
                ApplyZoom();
            };
            
            _zoomTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _zoomTimer?.Stop();
            _zoomTimer = null;
            FloweryScaleManager.ScaleFactorChanged -= OnScaleFactorChanged;
            
            // Reset scale override and max when leaving
            FloweryScaleManager.OverrideScaleFactor = null;
            FloweryScaleManager.MaxScaleFactor = 1.0;
        }

        private void OnScaleFactorChanged(object? sender, ScaleChangedEventArgs e)
        {
            // Apply scaling when the scale factor changes
            DispatcherQueue.TryEnqueue(() => ApplyScalingToControls(e.ScaleFactor));
        }

        private void ApplyZoom()
        {
            // Update the radial progress display
            ZoomProgress.Value = _currentZoomPercent;
            
            // Set the global scale factor override - this triggers ScaleFactorChanged
            var scaleFactor = _currentZoomPercent / 100.0;
            FloweryScaleManager.OverrideScaleFactor = scaleFactor;
        }

        private void ApplyScalingToControls(double scaleFactor)
        {
            // Apply scaling using ScaleHelper - same approach as ScalingExamples.xaml.cs
            
            // Card padding
            DemoCard.Padding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingSmall, scaleFactor);
            
            // Card content spacing
            DemoCardContent.Spacing = ScaleHelper.SpacingSmall(scaleFactor);
            
            // Button row spacing
            DemoButtonRow.Spacing = ScaleHelper.SpacingXS(scaleFactor);
            
            // Badge row spacing  
            DemoBadgeRow.Spacing = ScaleHelper.SpacingXS(scaleFactor);
            
            // Title font size
            DemoTitle.FontSize = ScaleHelper.FontSubheading(scaleFactor);
            
            // Input and button sizes
            var controlSize = ScaleFactorToSize(scaleFactor);
            DemoInput.Size = controlSize;
            DemoButton1.Size = controlSize;
            DemoButton2.Size = controlSize;
            DemoButton3.Size = controlSize;
            
            // Badge sizes
            var badgeSize = ScaleFactorToSize(scaleFactor);
            DemoBadge1.Size = badgeSize;
            DemoBadge2.Size = badgeSize;
            DemoBadge3.Size = badgeSize;
        }

        /// <summary>
        /// Converts a scale factor to a DaisySize enum value.
        /// For 50-200% range: ExtraSmall→Small→Medium→Large→ExtraLarge
        /// </summary>
        private static DaisySize ScaleFactorToSize(double scaleFactor)
        {
            return scaleFactor switch
            {
                < 0.65 => DaisySize.ExtraSmall,  // 50-64%
                < 0.90 => DaisySize.Small,       // 65-89%
                < 1.20 => DaisySize.Medium,      // 90-119%
                < 1.60 => DaisySize.Large,       // 120-159%
                _ => DaisySize.ExtraLarge        // 160%+
            };
        }
    }
}
