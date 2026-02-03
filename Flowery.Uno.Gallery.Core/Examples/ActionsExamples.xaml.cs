using System;
using System.Collections.Generic;
using Flowery.Controls;
using Flowery.Services;
using Flowery.Theming;
using Flowery.Uno.Gallery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class ActionsExamples : ScrollableExamplePage
    {
        public event EventHandler? OpenModalRequested;
        public event EventHandler<ModalRadiiEventArgs>? OpenModalWithRadiiRequested;

        public string[] DropdownMenuItems { get; } =
        [
            "Create",
            "Duplicate",
            "Archive",
            "Delete"
        ];

        public GalleryLocalization Localization { get; } = GalleryLocalization.Instance;

        private double _figmaCollapsedWidth = 68;
        private double _figmaExpandedWidth = 320;
        private double _figmaDetailsWidth = 220;
        private bool _isInitializingNeumorphicTuning;

        public ActionsExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            FlowerySizeManager.SizeChanged += OnGlobalSizeChanged;
            GalleryLocalization.CultureChanged += OnCultureChanged;
            RefreshLocalizationBindings();
            ApplyCopyButtonIconExample();
            UpdateFigmaCommentSize(FlowerySizeManager.CurrentSize);
            InitializeNeumorphicTuningControls();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            FlowerySizeManager.SizeChanged -= OnGlobalSizeChanged;
            GalleryLocalization.CultureChanged -= OnCultureChanged;
        }

        private void OnThemeChanged(object? sender, string themeName)
        {
            ApplyCopyButtonIconExample();
        }

        private void OnGlobalSizeChanged(object? sender, DaisySize size)
        {
            UpdateFigmaCommentSize(size);
        }

        private void UpdateFigmaCommentSize(DaisySize size)
        {
            var scale = size switch
            {
                DaisySize.ExtraSmall => 0.75,
                DaisySize.Small => 1.0,
                DaisySize.Medium => 1.2,
                DaisySize.Large => 1.45,
                DaisySize.ExtraLarge => 1.75,
                _ => 1.0
            };

            _figmaCollapsedWidth = 68 * scale;
            _figmaExpandedWidth = 320 * scale;
            _figmaDetailsWidth = 220 * scale;

            var avatarSize = size switch
            {
                DaisySize.ExtraSmall => DaisySize.ExtraSmall,
                DaisySize.Small => DaisySize.Small,
                DaisySize.Medium => DaisySize.Medium,
                DaisySize.Large => DaisySize.Medium,
                DaisySize.ExtraLarge => DaisySize.Large,
                _ => DaisySize.Small
            };

            var padding = size switch
            {
                DaisySize.ExtraSmall => 4,
                DaisySize.Small => 8,
                DaisySize.Medium => 10,
                DaisySize.Large => 12,
                DaisySize.ExtraLarge => 14,
                _ => 8
            };

            var cornerRadius = size switch
            {
                DaisySize.ExtraSmall => 10,
                DaisySize.Small => 16,
                DaisySize.Medium => 18,
                DaisySize.Large => 22,
                DaisySize.ExtraLarge => 26,
                _ => 16
            };

            var fontSize = size switch
            {
                DaisySize.ExtraSmall => 10.0,
                DaisySize.Small => 12.0,
                DaisySize.Medium => 14.0,
                DaisySize.Large => 15.0,
                DaisySize.ExtraLarge => 16.0,
                _ => 12.0
            };

            var secondaryFontSize = size switch
            {
                DaisySize.ExtraSmall => 9.0,
                DaisySize.Small => 10.0,
                DaisySize.Medium => 11.0,
                DaisySize.Large => 12.0,
                DaisySize.ExtraLarge => 13.0,
                _ => 10.0
            };

            if (CommentBubbleBorder != null)
            {
                var isExpanded = CommentToggle?.IsChecked == true;
                CommentBubbleBorder.Width = isExpanded ? _figmaExpandedWidth : _figmaCollapsedWidth;
                CommentBubbleBorder.Padding = new Thickness(padding);
                CommentBubbleBorder.CornerRadius = new CornerRadius(cornerRadius);
            }

            if (CommentDetailsPanel != null)
            {
                CommentDetailsPanel.Width = _figmaDetailsWidth;
            }

            if (CommentAvatar != null)
            {
                CommentAvatar.Size = avatarSize;
            }

            if (CommentContentGrid != null)
            {
                CommentContentGrid.ColumnSpacing = padding > 6 ? padding + 2 : 6;
            }

            if (CommentAuthorText != null)
            {
                CommentAuthorText.FontSize = fontSize;
            }

            if (CommentTimeText != null)
            {
                CommentTimeText.FontSize = secondaryFontSize;
            }

            if (CommentBodyText != null)
            {
                CommentBodyText.FontSize = secondaryFontSize;
            }
        }

        private void InitializeNeumorphicTuningControls()
        {
            _isInitializingNeumorphicTuning = true;

            if (NeumorphicIntensityBox != null)
            {
                NeumorphicIntensityBox.Value = (decimal)DaisyBaseContentControl.GlobalNeumorphicIntensity;
                NeumorphicIntensityBox.Minimum = 0m;
                NeumorphicIntensityBox.Maximum = 1m;
                NeumorphicIntensityBox.Increment = 0.05m;
                NeumorphicIntensityBox.FormatString = "0.##";
                NeumorphicIntensityBox.MaxDecimalPlaces = 2;
                NeumorphicIntensityBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, NeumorphicTuningValueChanged);
            }

            if (NeumorphicOffsetBox != null)
            {
                NeumorphicOffsetBox.Value = (decimal)DaisyNeumorphic.GetOffset(this);
                NeumorphicOffsetBox.Minimum = 0m;
                NeumorphicOffsetBox.Maximum = 32m;
                NeumorphicOffsetBox.Increment = 1m;
                NeumorphicOffsetBox.FormatString = "0";
                NeumorphicOffsetBox.MaxDecimalPlaces = 0;
                NeumorphicOffsetBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, NeumorphicTuningValueChanged);
            }

            if (NeumorphicBlurBox != null)
            {
                NeumorphicBlurBox.Value = (decimal)DaisyNeumorphic.GetBlurRadius(this);
                NeumorphicBlurBox.Minimum = 0m;
                NeumorphicBlurBox.Maximum = 60m;
                NeumorphicBlurBox.Increment = 1m;
                NeumorphicBlurBox.FormatString = "0";
                NeumorphicBlurBox.MaxDecimalPlaces = 0;
                NeumorphicBlurBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, NeumorphicTuningValueChanged);
            }

            _isInitializingNeumorphicTuning = false;
            ApplyNeumorphicTuning();
        }

        private void NeumorphicTuningValueChanged(DependencyObject sender, DependencyProperty dp)
        {
            _ = sender;
            _ = dp;

            if (_isInitializingNeumorphicTuning)
                return;

            ApplyNeumorphicTuning();
        }

        private void ApplyNeumorphicTuning()
        {
            var intensity = (double)(NeumorphicIntensityBox?.Value ?? (decimal)DaisyBaseContentControl.GlobalNeumorphicIntensity);
            DaisyBaseContentControl.GlobalNeumorphicIntensity = Math.Clamp(intensity, 0.0, 1.0);

            var offset = (double)(NeumorphicOffsetBox?.Value ?? (decimal)DaisyNeumorphic.GetOffset(this));
            var blur = (double)(NeumorphicBlurBox?.Value ?? (decimal)DaisyNeumorphic.GetBlurRadius(this));

            if (MainScrollViewer != null)
            {
                ApplyNeumorphicOverrides(MainScrollViewer, offset, blur);
            }
        }

        private static void ApplyNeumorphicOverrides(DependencyObject root, double offset, double blur)
        {
            if (root is DaisyButton button)
            {
                DaisyNeumorphic.SetOffset(button, offset);
                DaisyNeumorphic.SetBlurRadius(button, blur);
            }
            else if (root is DaisyBaseContentControl baseControl)
            {
                DaisyNeumorphic.SetOffset(baseControl, offset);
                DaisyNeumorphic.SetBlurRadius(baseControl, blur);
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                ApplyNeumorphicOverrides(child, offset, blur);
            }
        }

        private void ApplyCopyButtonIconExample()
        {
            if (CopyButtonIconOnly == null)
                return;

            // Uno: Do not use PathIcon dynamically. Use Path elements.
            CopyButtonIconOnly.Content = FloweryPathHelpers.CreateCopyIcon(size: 16);
            CopyButtonIconOnly.SuccessContent = FloweryPathHelpers.CreateCheckIcon(size: 16);
        }

        private void OpenModalBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenModalRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OpenDefaultModal_Click(object sender, RoutedEventArgs e)
        {
            OpenModalWithRadiiRequested?.Invoke(this, new ModalRadiiEventArgs
            {
                TopLeft = 16,
                TopRight = 16,
                BottomLeft = 16,
                BottomRight = 16,
                Title = "Default Corners"
            });
        }

        private void OpenPillTopModal_Click(object sender, RoutedEventArgs e)
        {
            OpenModalWithRadiiRequested?.Invoke(this, new ModalRadiiEventArgs
            {
                TopLeft = 24,
                TopRight = 24,
                BottomLeft = 8,
                BottomRight = 8,
                Title = "Pill Top"
            });
        }

        private void OpenSharpModal_Click(object sender, RoutedEventArgs e)
        {
            OpenModalWithRadiiRequested?.Invoke(this, new ModalRadiiEventArgs
            {
                TopLeft = 0,
                TopRight = 0,
                BottomLeft = 0,
                BottomRight = 0,
                Title = "Sharp Corners"
            });
        }

        private void MenuDropdown_SelectedItemChanged(object sender, DaisyDropdownSelectionChangedEventArgs e)
        {
            if (DropdownMenuResultText == null || MenuDropdown == null)
                return;

            var selected = e.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                DropdownMenuResultText.Text = $"Selected: {selected}";

                // Reset selection so the menu can be used for repeated actions
                MenuDropdown.SelectedItem = null;
            }
        }

        private void FabItem_Click(object sender, RoutedEventArgs e)
        {
            if (DropdownMenuResultText == null)
                return;

            if (sender is FrameworkElement fe && fe.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                DropdownMenuResultText.Text = $"FAB clicked: {tag}";
            }
        }

        private void CommentToggle_Checked(object sender, RoutedEventArgs e)
        {
            AnimateCommentBubble(isExpanded: true);
        }

        private void CommentToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            AnimateCommentBubble(isExpanded: false);
        }

        private void AnimateCommentBubble(bool isExpanded)
        {
            if (CommentBubbleBorder == null || CommentDetailsPanel == null)
                return;

            if (isExpanded)
            {
                CommentDetailsPanel.Visibility = Visibility.Visible;
                CommentDetailsPanel.Opacity = 0;
            }

            var storyboard = new Storyboard();

            var widthAnim = new DoubleAnimation
            {
                From = CommentBubbleBorder.ActualWidth > 0 ? CommentBubbleBorder.ActualWidth : CommentBubbleBorder.Width,
                To = isExpanded ? _figmaExpandedWidth : _figmaCollapsedWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(widthAnim, CommentBubbleBorder);
            Storyboard.SetTargetProperty(widthAnim, "Width");
            storyboard.Children.Add(widthAnim);

            var opacityAnim = new DoubleAnimation
            {
                From = isExpanded ? 0 : 1,
                To = isExpanded ? 1 : 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(220)),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(opacityAnim, CommentDetailsPanel);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");
            storyboard.Children.Add(opacityAnim);

            storyboard.Completed += (_, _) =>
            {
                if (!isExpanded)
                {
                    CommentDetailsPanel.Visibility = Visibility.Collapsed;
                    CommentDetailsPanel.Opacity = 1;
                }
            };

            storyboard.Begin();
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
    }
}
