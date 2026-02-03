using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Flowery.Controls;
using Flowery.Services;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class ScalingExamples : ScrollableExamplePage, INotifyPropertyChanged
    {
        private Window? _hostWindow;
        private bool _isScalingEnabled = true;
        private int _maxScalePresetIndex = 0;
        private bool _isManualZoomEnabled = false;
        private double _manualZoomPercent = 100.0;
        private double _manualZoomMaxPercent = 100.0;
        private int _resolutionPresetIndex = -1;
        private bool _isDesktopApp = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets whether auto-scaling is enabled for this demo.
        /// </summary>
        public bool IsScalingEnabled
        {
            get => _isScalingEnabled;
            set
            {
                if (_isScalingEnabled != value)
                {
                    _isScalingEnabled = value;
                    OnPropertyChanged();
                    FloweryScaleManager.IsEnabled = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the max scale preset for this demo.
        /// 0=100%, 1=125%, 2=150%, 3=200%, 4=300%.
        /// </summary>
        public int MaxScalePresetIndex
        {
            get => _maxScalePresetIndex;
            set
            {
                if (_maxScalePresetIndex != value)
                {
                    _maxScalePresetIndex = value;
                    OnPropertyChanged();
                    FloweryScaleManager.MaxScaleFactor = GetMaxScaleFactorForPresetIndex(value);
                    UpdateManualZoomMaximum();
                    if (IsManualZoomEnabled)
                    {
                        ApplyManualZoomOverride();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether manual zoom override is enabled.
        /// </summary>
        public bool IsManualZoomEnabled
        {
            get => _isManualZoomEnabled;
            set
            {
                if (_isManualZoomEnabled != value)
                {
                    _isManualZoomEnabled = value;
                    OnPropertyChanged();
                    ApplyManualZoomOverride();
                }
            }
        }

        /// <summary>
        /// Gets or sets the manual zoom percentage (e.g. 150 for 150%).
        /// </summary>
        public double ManualZoomPercent
        {
            get => _manualZoomPercent;
            set
            {
                if (Math.Abs(_manualZoomPercent - value) > 0.01)
                {
                    _manualZoomPercent = value;
                    OnPropertyChanged();
                    UpdateZoomPercentText();
                    if (IsManualZoomEnabled)
                    {
                        ApplyManualZoomOverride();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum zoom percentage for the slider (derived from Max Scale).
        /// </summary>
        public double ManualZoomMaxPercent
        {
            get => _manualZoomMaxPercent;
            set
            {
                if (Math.Abs(_manualZoomMaxPercent - value) > 0.01)
                {
                    _manualZoomMaxPercent = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the resolution preset index for this demo.
        /// </summary>
        public int ResolutionPresetIndex
        {
            get => _resolutionPresetIndex;
            set
            {
                if (_resolutionPresetIndex != value)
                {
                    _resolutionPresetIndex = value;
                    OnPropertyChanged();
                    ApplyResolutionPreset(value);
                }
            }
        }

        /// <summary>
        /// Gets whether this demo is hosted in a desktop Window.
        /// </summary>
        public bool IsDesktopApp
        {
            get => _isDesktopApp;
            private set
            {
                if (_isDesktopApp != value)
                {
                    _isDesktopApp = value;
                    OnPropertyChanged();
                }
            }
        }

        public ScalingExamples()
        {
            InitializeComponent();

            // Initialize local UI state from global state (if any settings were pre-configured).
            // NOTE: Use backing fields to avoid triggering global FloweryScaleManager changes
            // during construction. Global state is only modified in OnLoaded/OnUnloaded.
            _maxScalePresetIndex = GetMaxScalePresetIndex(FloweryScaleManager.MaxScaleFactor);
            _manualZoomMaxPercent = FloweryScaleManager.MaxScaleFactor * 100.0;

            if (FloweryScaleManager.OverrideScaleFactor.HasValue)
            {
                _isManualZoomEnabled = true;
                _manualZoomPercent = FloweryScaleManager.OverrideScaleFactor.Value * 100.0;
                if (_manualZoomPercent > _manualZoomMaxPercent)
                    _manualZoomPercent = _manualZoomMaxPercent;
            }

            // Note: _isScalingEnabled stays at default (true) but we don't set the property
            // to avoid triggering FloweryScaleManager.IsEnabled during construction.

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // Subscribe to scale factor changes to update the UI
            FloweryScaleManager.ScaleFactorChanged += OnScaleFactorChanged;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Enable scaling globally ONLY when this page is actually loaded/displayed.
            // This prevents affecting other pages when ScalingExamples is just instantiated.
            FloweryScaleManager.IsEnabled = true;

            // Find the parent window
            _hostWindow = FloweryScaleManager.MainWindow;

            // Check if we're in a desktop app context
            IsDesktopApp = _hostWindow != null;

            // Apply initial scale factor
            var scaleFactor = FloweryScaleManager.GetScaleFactor(this);
            if (scaleFactor <= 0 || Math.Abs(scaleFactor - 1.0) < 0.001)
            {
                // Calculate initial scale factor
                if (_hostWindow?.Content is FrameworkElement content)
                {
                    scaleFactor = FloweryScaleManager.CalculateScaleFactor(content.ActualWidth, content.ActualHeight);
                }
                else
                {
                    scaleFactor = 1.0;
                }
            }

            ApplyScaling(scaleFactor);
            UpdateZoomPercentText();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            FloweryScaleManager.ScaleFactorChanged -= OnScaleFactorChanged;

            // Reset global scale state to defaults when leaving this demo page.
            // This prevents scaling settings from affecting other Gallery pages.
            FloweryScaleManager.IsEnabled = false;
            FloweryScaleManager.MaxScaleFactor = 1.0;
            FloweryScaleManager.OverrideScaleFactor = null;
        }

        private void OnScaleFactorChanged(object? sender, ScaleChangedEventArgs e)
        {
            // Update the UI on the UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                ApplyScaling(e.ScaleFactor);
            });
        }

        private void ApplyScaling(double scaleFactor)
        {
            // If scaling is disabled, use 1.0
            if (!IsScalingEnabled)
                scaleFactor = 1.0;

            // Apply scaling to all the named elements
            ApplyCustomerCardScaling(scaleFactor);
            ApplyPresetsCardScaling(scaleFactor);
            ApplyAboutCardScaling(scaleFactor);
        }

        private void ApplyCustomerCardScaling(double scaleFactor)
        {
            // Customer subtitle
            CustomerSubtitle.FontSize = ScaleHelper.FontCaption(scaleFactor);

            // Main card padding
            MainCustomerCard.Padding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingMedium, scaleFactor);

            // Customer header border padding
            CustomerHeaderBorder.Padding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingSmall, scaleFactor);

            // Company name
            CompanyNameText.FontSize = ScaleHelper.FontTitle(scaleFactor);

            // Customer number labels
            CustomerNumberLabel.FontSize = ScaleHelper.FontSmall(scaleFactor);
            CustomerIdText.FontSize = ScaleHelper.FontBody(scaleFactor);

            // Card widths
            var cardWidth = ScaleHelper.CardWidth(scaleFactor);
            AddressCard.Width = cardWidth;
            ContactCard.Width = cardWidth;
            PaymentCard.Width = cardWidth;
            GroupCard.Width = cardWidth;
            ActivityCard.Width = cardWidth;
            NotesCard.Width = cardWidth;

            // Card paddings
            var cardPadding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingSmall, scaleFactor);
            AddressCard.Padding = cardPadding;
            ContactCard.Padding = cardPadding;
            PaymentCard.Padding = cardPadding;
            GroupCard.Padding = cardPadding;
            ActivityCard.Padding = cardPadding;
            NotesCard.Padding = cardPadding;

            // Card content spacing
            var smallSpacing = ScaleHelper.SpacingSmall(scaleFactor);
            var xsSpacing = ScaleHelper.SpacingXS(scaleFactor);

            AddressCardContent.Spacing = smallSpacing;
            ContactCardContent.Spacing = smallSpacing;
            PaymentCardContent.Spacing = smallSpacing;
            GroupCardContent.Spacing = smallSpacing;
            ActivityCardContent.Spacing = smallSpacing;
            NotesCardContent.Spacing = smallSpacing;

            AddressFieldsPanel.Spacing = xsSpacing;
            ContactFieldsPanel.Spacing = xsSpacing;
            PaymentFieldsPanel.Spacing = xsSpacing;
            GroupFieldsPanel.Spacing = xsSpacing;
            ActivityItemsPanel.Spacing = xsSpacing;

            // Icon sizes and text - using DaisyIconText's combined properties
            var iconSmall = ScaleHelper.IconSmall(scaleFactor);
            var headingSize = ScaleHelper.FontSubheading(scaleFactor);
            
            AddressHeader.IconSize = iconSmall;
            AddressHeader.FontSizeOverride = headingSize;
            
            ContactHeader.IconSize = iconSmall;
            ContactHeader.FontSizeOverride = headingSize;
            
            PaymentHeader.IconSize = iconSmall;
            PaymentHeader.FontSizeOverride = headingSize;
            
            GroupHeader.IconSize = iconSmall;
            GroupHeader.FontSizeOverride = headingSize;
            
            ActivityHeader.IconSize = iconSmall;
            ActivityHeader.FontSizeOverride = headingSize;
            
            NotesHeader.IconSize = iconSmall;
            NotesHeader.FontSizeOverride = headingSize;

            // Social buttons: use Size property instead of explicit Width/Height
            // The Size property controls both button and internal icon sizing
            var socialButtonSize = ScaleFactorToSize(scaleFactor);
            SocialButton1.Size = socialButtonSize;
            SocialButton2.Size = socialButtonSize;
            SocialButton3.Size = socialButtonSize;

            // Country label
            CountryLabel.FontSize = ScaleHelper.FontSmall(scaleFactor);
            CountrySelect.Width = ScaleHelper.GetScaledValue(200, 120, scaleFactor);

            // Group label
            GroupLabelText.FontSize = ScaleHelper.FontBody(scaleFactor);

            // Activity items padding and font sizes
            var activityPadding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingXS, scaleFactor);
            ActivityItem1.Padding = activityPadding;
            ActivityItem2.Padding = activityPadding;
            ActivityItem3.Padding = activityPadding;

            var captionSize = ScaleHelper.FontCaption(scaleFactor);
            Activity1Text.FontSize = captionSize;
            Activity2Text.FontSize = captionSize;
            Activity3Text.FontSize = captionSize;

            Activity1Time.FontSize = ScaleHelper.FontSmall(scaleFactor);
            Activity2Time.FontSize = ScaleHelper.FontSmall(scaleFactor);
            Activity3Time.FontSize = ScaleHelper.FontSmall(scaleFactor);

            // Action bar padding and spacing
            ActionBar.Padding = ScaleHelper.GetScaledThickness(FloweryScalePreset.SpacingSmall, scaleFactor);
            ActionButtonsPanel.Spacing = smallSpacing;

            // Scale DaisyInput controls via Size property
            var inputSize = ScaleFactorToSize(scaleFactor);
            ApplyInputSizeToPanel(ContactFieldsPanel, inputSize);
            ApplyInputSizeToPanel(AddressFieldsPanel, inputSize);
            ApplyInputSizeToPanel(PaymentFieldsPanel, inputSize);
            ApplyInputSizeToPanel(GroupFieldsPanel, inputSize);
        }

        /// <summary>
        /// Converts a scale factor to a DaisySize enum value.
        /// </summary>
        private static DaisySize ScaleFactorToSize(double scaleFactor)
        {
            return scaleFactor switch
            {
                < 0.65 => DaisySize.ExtraSmall,
                < 0.8 => DaisySize.Small,
                < 1.1 => DaisySize.Medium,
                < 1.4 => DaisySize.Large,
                _ => DaisySize.ExtraLarge
            };
        }

        /// <summary>
        /// Applies Size property to all DaisyInput controls in a panel.
        /// </summary>
        private static void ApplyInputSizeToPanel(StackPanel panel, DaisySize size)
        {
            foreach (var child in panel.Children)
            {
                if (child is DaisyInput input)
                {
                    input.Size = size;
                }
            }
        }

        private void ApplyPresetsCardScaling(double scaleFactor)
        {
            // Presets subtitle
            PresetsSubtitle.FontSize = ScaleHelper.FontCaption(scaleFactor);

            // Card widths
            var cardWidth = ScaleHelper.CardWidth(scaleFactor);
            FontPresetsCard.Width = cardWidth;
            SpacingPresetsCard.Width = cardWidth;

            // Font presets card title
            FontPresetsTitleText.FontSize = ScaleHelper.FontHeading(scaleFactor);
            SpacingPresetsTitleText.FontSize = ScaleHelper.FontHeading(scaleFactor);
            UsageTitleText.FontSize = ScaleHelper.FontHeading(scaleFactor);

            // Font size demos - each TextBlock uses its preset size
            FontDisplayDemo.FontSize = ScaleHelper.FontDisplay(scaleFactor);
            FontTitleDemo.FontSize = ScaleHelper.FontTitle(scaleFactor);
            FontHeadingDemo.FontSize = ScaleHelper.FontHeading(scaleFactor);
            FontSubheadingDemo.FontSize = ScaleHelper.FontSubheading(scaleFactor);
            FontBodyDemo.FontSize = ScaleHelper.FontBody(scaleFactor);
            FontCaptionDemo.FontSize = ScaleHelper.FontCaption(scaleFactor);
            FontSmallDemo.FontSize = ScaleHelper.FontSmall(scaleFactor);

            // Spacing demos - height represents the spacing value
            SpacingXLDemo.MinHeight = ScaleHelper.SpacingXL(scaleFactor);
            SpacingLargeDemo.MinHeight = ScaleHelper.SpacingLarge(scaleFactor);
            SpacingMediumDemo.MinHeight = ScaleHelper.SpacingMedium(scaleFactor);
            SpacingSmallDemo.MinHeight = ScaleHelper.SpacingSmall(scaleFactor);
            SpacingXSDemo.MinHeight = ScaleHelper.SpacingXS(scaleFactor);

            SpacingXLText.FontSize = ScaleHelper.FontCaption(scaleFactor);
            SpacingLargeText.FontSize = ScaleHelper.FontCaption(scaleFactor);
            SpacingMediumText.FontSize = ScaleHelper.FontCaption(scaleFactor);
            SpacingSmallText.FontSize = ScaleHelper.FontCaption(scaleFactor);
            SpacingXSText.FontSize = ScaleHelper.FontCaption(scaleFactor);
        }

        private void ApplyAboutCardScaling(double scaleFactor)
        {
            AboutHeadingText.FontSize = ScaleHelper.FontHeading(scaleFactor);
            AboutDescriptionText.FontSize = ScaleHelper.FontBody(scaleFactor);
            AboutTipText.FontSize = ScaleHelper.FontCaption(scaleFactor);
        }

        private void UpdateZoomPercentText()
        {
            if (ZoomPercentText != null)
            {
                ZoomPercentText.Text = $"{ManualZoomPercent:0}%";
            }
        }

        private static int GetMaxScalePresetIndex(double maxScaleFactor)
        {
            if (maxScaleFactor >= 3.0) return 4;
            if (maxScaleFactor >= 2.0) return 3;
            if (maxScaleFactor >= 1.5) return 2;
            if (maxScaleFactor >= 1.25) return 1;
            return 0;
        }

        private static double GetMaxScaleFactorForPresetIndex(int presetIndex)
        {
            return presetIndex switch
            {
                1 => 1.25,
                2 => 1.5,
                3 => 2.0,
                4 => 3.0,
                _ => 1.0
            };
        }

        private void UpdateManualZoomMaximum()
        {
            ManualZoomMaxPercent = GetMaxScaleFactorForPresetIndex(MaxScalePresetIndex) * 100.0;
            if (ManualZoomPercent > ManualZoomMaxPercent)
                ManualZoomPercent = ManualZoomMaxPercent;
        }

        private void ApplyManualZoomOverride()
        {
            if (!IsManualZoomEnabled)
            {
                FloweryScaleManager.OverrideScaleFactor = null;
                return;
            }

            var percent = ManualZoomPercent;
            if (percent > ManualZoomMaxPercent)
                percent = ManualZoomMaxPercent;

            FloweryScaleManager.OverrideScaleFactor = percent / 100.0;
        }

        private static void ApplyResolutionPreset(int presetIndex)
        {
            if (presetIndex < 0)
                return;

            var window = FloweryScaleManager.MainWindow;
            if (window == null)
                return;

            var (width, height) = presetIndex switch
            {
                0 => (960.0, 540.0),
                1 => (1280.0, 720.0),
                2 => (1366.0, 768.0),
                3 => (1600.0, 900.0),
                4 => (1920.0, 1080.0),
                5 => (2560.0, 1440.0),
                6 => (3440.0, 1440.0),
                7 => (3840.0, 2160.0),
                _ => (0.0, 0.0)
            };

            if (width <= 0 || height <= 0)
                return;

            try
            {
                // Get the DPI scaling
                var rasterizationScale = window.Content?.XamlRoot?.RasterizationScale ?? 1.0;

                // Resize the window using AppWindow
                var appWindow = window.AppWindow;
                if (appWindow != null)
                {
                    // AppWindow.Resize expects physical (not logical) pixels
                    var physicalWidth = (int)(width);
                    var physicalHeight = (int)(height);
                    appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = physicalWidth, Height = physicalHeight });
                }
            }
            catch
            {
                // Resize not supported on this platform
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
