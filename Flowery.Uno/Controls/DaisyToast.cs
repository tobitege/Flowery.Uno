using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Flowery.Controls
{
    /// <summary>
    /// Toast container horizontal position.
    /// </summary>
    public enum ToastHorizontalPosition
    {
        Start,
        Center,
        End
    }

    /// <summary>
    /// Toast container vertical position.
    /// </summary>
    public enum ToastVerticalPosition
    {
        Top,
        Middle,
        Bottom
    }

    /// <summary>
    /// A Toast container control styled after DaisyUI's Toast component.
    /// Displays notification messages in a corner of the screen.
    /// </summary>
    public partial class DaisyToast : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<UIElement> _toastItems = [];

        public DaisyToast()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        #region HorizontalPosition
        public static readonly DependencyProperty HorizontalPositionProperty =
            DependencyProperty.Register(
                nameof(HorizontalPosition),
                typeof(ToastHorizontalPosition),
                typeof(DaisyToast),
                new PropertyMetadata(ToastHorizontalPosition.End, OnAppearanceChanged));

        public ToastHorizontalPosition HorizontalPosition
        {
            get => (ToastHorizontalPosition)GetValue(HorizontalPositionProperty);
            set => SetValue(HorizontalPositionProperty, value);
        }
        #endregion

        #region VerticalPosition
        public static readonly DependencyProperty VerticalPositionProperty =
            DependencyProperty.Register(
                nameof(VerticalPosition),
                typeof(ToastVerticalPosition),
                typeof(DaisyToast),
                new PropertyMetadata(ToastVerticalPosition.Bottom, OnAppearanceChanged));

        public ToastVerticalPosition VerticalPosition
        {
            get => (ToastVerticalPosition)GetValue(VerticalPositionProperty);
            set => SetValue(VerticalPositionProperty, value);
        }
        #endregion

        #region ToastSpacing
        public static readonly DependencyProperty ToastSpacingProperty =
            DependencyProperty.Register(
                nameof(ToastSpacing),
                typeof(double),
                typeof(DaisyToast),
                new PropertyMetadata(8.0, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the spacing between toast items.
        /// </summary>
        public double ToastSpacing
        {
            get => (double)GetValue(ToastSpacingProperty);
            set => SetValue(ToastSpacingProperty, value);
        }
        #endregion

        #region ToastOffset
        public static readonly DependencyProperty ToastOffsetProperty =
            DependencyProperty.Register(
                nameof(ToastOffset),
                typeof(Thickness),
                typeof(DaisyToast),
                new PropertyMetadata(new Thickness(16), OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the offset from the edge of the container.
        /// </summary>
        public Thickness ToastOffset
        {
            get => (Thickness)GetValue(ToastOffsetProperty);
            set => SetValue(ToastOffsetProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyToast toast)
                toast.ApplyAll();
        }

        private void BuildVisualTree()
        {
            if (_itemsPanel != null)
                return;

            _itemsPanel = new StackPanel
            {
                Spacing = ToastSpacing
            };

            _toastItems.Clear();

            // Collect children from Content
            if (Content is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                foreach (var child in children)
                {
                    _toastItems.Add(child);
                    _itemsPanel.Children.Add(child);
                }
            }

            Content = _itemsPanel;
        }

        private void ApplyAll()
        {
            if (_itemsPanel == null)
                return;

            _itemsPanel.Spacing = ToastSpacing;
            Margin = ToastOffset;

            // Apply alignment based on position
            HorizontalAlignment = HorizontalPosition switch
            {
                ToastHorizontalPosition.Start => HorizontalAlignment.Left,
                ToastHorizontalPosition.Center => HorizontalAlignment.Center,
                ToastHorizontalPosition.End => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Right
            };

            VerticalAlignment = VerticalPosition switch
            {
                ToastVerticalPosition.Top => VerticalAlignment.Top,
                ToastVerticalPosition.Middle => VerticalAlignment.Center,
                ToastVerticalPosition.Bottom => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Bottom
            };
        }

        /// <summary>
        /// Adds a toast item to the container.
        /// </summary>
        public void AddToast(UIElement toast)
        {
            if (_itemsPanel == null)
                return;

            _toastItems.Add(toast);
            _itemsPanel.Children.Add(toast);
        }

        /// <summary>
        /// Removes a toast item from the container.
        /// </summary>
        public void RemoveToast(UIElement toast)
        {
            if (_itemsPanel == null)
                return;

            _toastItems.Remove(toast);
            _itemsPanel.Children.Remove(toast);
        }

        /// <summary>
        /// Clears all toast items.
        /// </summary>
        public void ClearToasts()
        {
            if (_itemsPanel == null)
                return;

            _toastItems.Clear();
            _itemsPanel.Children.Clear();
        }
    }
}
