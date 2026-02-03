using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A swap toggle control styled after DaisyUI's Swap component.
    /// Shows different content based on checked/unchecked state.
    /// </summary>
    public partial class DaisySwap : ToggleButton
    {
        private ContentPresenter? _onPresenter;
        private ContentPresenter? _offPresenter;
        private Grid? _contentGrid;

        #region OnContent
        public static readonly DependencyProperty OnContentProperty =
            DependencyProperty.Register(
                nameof(OnContent),
                typeof(object),
                typeof(DaisySwap),
                new PropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Content displayed when the swap is checked (on).
        /// </summary>
        public object? OnContent
        {
            get => GetValue(OnContentProperty);
            set => SetValue(OnContentProperty, value);
        }
        #endregion

        #region OffContent
        public static readonly DependencyProperty OffContentProperty =
            DependencyProperty.Register(
                nameof(OffContent),
                typeof(object),
                typeof(DaisySwap),
                new PropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Content displayed when the swap is unchecked (off).
        /// </summary>
        public object? OffContent
        {
            get => GetValue(OffContentProperty);
            set => SetValue(OffContentProperty, value);
        }
        #endregion

        #region IndeterminateContent
        public static readonly DependencyProperty IndeterminateContentProperty =
            DependencyProperty.Register(
                nameof(IndeterminateContent),
                typeof(object),
                typeof(DaisySwap),
                new PropertyMetadata(null));

        /// <summary>
        /// Content displayed when the swap is in indeterminate state.
        /// </summary>
        public object? IndeterminateContent
        {
            get => GetValue(IndeterminateContentProperty);
            set => SetValue(IndeterminateContentProperty, value);
        }
        #endregion

        #region TransitionEffect
        public static readonly DependencyProperty TransitionEffectProperty =
            DependencyProperty.Register(
                nameof(TransitionEffect),
                typeof(SwapEffect),
                typeof(DaisySwap),
                new PropertyMetadata(SwapEffect.None));

        /// <summary>
        /// The transition effect when swapping between states.
        /// </summary>
        public SwapEffect TransitionEffect
        {
            get => (SwapEffect)GetValue(TransitionEffectProperty);
            set => SetValue(TransitionEffectProperty, value);
        }
        #endregion

        public DaisySwap()
        {
            DefaultStyleKey = typeof(DaisySwap);
            Loaded += OnLoaded;
            Click += OnClick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_contentGrid != null)
            {
                UpdateVisualState();
                return;
            }

            BuildVisualTree();
            UpdateVisualState();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            UpdateVisualState();
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySwap swap)
            {
                swap.UpdateContent();
                swap.UpdateVisualState();
            }
        }

        private void BuildVisualTree()
        {
            _contentGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _onPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            _offPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Visible
            };

            _contentGrid.Children.Add(_offPresenter);
            _contentGrid.Children.Add(_onPresenter);

            Content = _contentGrid;

            UpdateContent();
            ApplyTheme();
        }

        private void UpdateContent()
        {
            if (_onPresenter != null)
            {
                _onPresenter.Content = OnContent;
            }
            if (_offPresenter != null)
            {
                _offPresenter.Content = OffContent;
            }
        }

        private void UpdateVisualState()
        {
            if (_onPresenter == null || _offPresenter == null)
                return;

            if (IsChecked == true)
            {
                _onPresenter.Visibility = Visibility.Visible;
                _offPresenter.Visibility = Visibility.Collapsed;
            }
            else
            {
                _onPresenter.Visibility = Visibility.Collapsed;
                _offPresenter.Visibility = Visibility.Visible;
            }
        }

        private void ApplyTheme()
        {
            // Check for lightweight styling overrides
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisySwap", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisySwap", "BorderBrush");

            // Make the button transparent by default so only the content shows
            Background = bgOverride ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            BorderBrush = borderOverride ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            BorderThickness = new Thickness(0);
            Padding = new Thickness(4);
            MinWidth = 0;
            MinHeight = 0;
        }
    }
}
