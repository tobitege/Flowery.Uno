using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// A top navigation bar styled after DaisyUI's Navbar component.
    /// Provides Start, Center, and End content areas for flexible layout.
    /// </summary>
    public partial class DaisyNavbar : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private ContentPresenter? _startPresenter;
        private ContentPresenter? _centerPresenter;
        private ContentPresenter? _endPresenter;
        private Border? _rootBorder;

        #region Dependency Properties

        public static readonly DependencyProperty NavbarStartProperty =
            DependencyProperty.Register(nameof(NavbarStart), typeof(object), typeof(DaisyNavbar),
                new PropertyMetadata(null, OnNavbarContentChanged));

        public object? NavbarStart
        {
            get => GetValue(NavbarStartProperty);
            set => SetValue(NavbarStartProperty, value);
        }

        public static readonly DependencyProperty NavbarCenterProperty =
            DependencyProperty.Register(nameof(NavbarCenter), typeof(object), typeof(DaisyNavbar),
                new PropertyMetadata(null, OnNavbarContentChanged));

        public object? NavbarCenter
        {
            get => GetValue(NavbarCenterProperty);
            set => SetValue(NavbarCenterProperty, value);
        }

        public static readonly DependencyProperty NavbarEndProperty =
            DependencyProperty.Register(nameof(NavbarEnd), typeof(object), typeof(DaisyNavbar),
                new PropertyMetadata(null, OnNavbarContentChanged));

        public object? NavbarEnd
        {
            get => GetValue(NavbarEndProperty);
            set => SetValue(NavbarEndProperty, value);
        }

        public static readonly DependencyProperty IsFullWidthProperty =
            DependencyProperty.Register(nameof(IsFullWidth), typeof(bool), typeof(DaisyNavbar),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsFullWidth
        {
            get => (bool)GetValue(IsFullWidthProperty);
            set => SetValue(IsFullWidthProperty, value);
        }

        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(nameof(HasShadow), typeof(bool), typeof(DaisyNavbar),
                new PropertyMetadata(true, OnAppearanceChanged));

        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        #endregion

        public DaisyNavbar()
        {
            DefaultStyleKey = typeof(DaisyNavbar);
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                UpdateAppearance();
                return;
            }

            BuildVisualTree();
            UpdateAppearance();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            UpdateAppearance();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        private static void OnNavbarContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNavbar navbar)
            {
                navbar.UpdateContent();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyNavbar navbar)
            {
                navbar.UpdateAppearance();
            }
        }

        private void BuildVisualTree()
        {
            // Build the visual tree programmatically
            _rootBorder = new Border
            {
                Padding = new Thickness(16, 12, 16, 12),
                CornerRadius = new CornerRadius(12),
            };

            _rootGrid = new Grid();
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Start section (left aligned)
            _startPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_startPresenter, 0);

            // Center section (centered)
            _centerPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_centerPresenter, 1);

            // End section (right aligned)
            _endPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_endPresenter, 2);

            _rootGrid.Children.Add(_startPresenter);
            _rootGrid.Children.Add(_centerPresenter);
            _rootGrid.Children.Add(_endPresenter);

            _rootBorder.Child = _rootGrid;
            Content = _rootBorder;

            UpdateContent();
            UpdateAppearance();
        }

        private void UpdateContent()
        {
            if (_startPresenter != null)
                _startPresenter.Content = NavbarStart;
            if (_centerPresenter != null)
                _centerPresenter.Content = NavbarCenter;
            if (_endPresenter != null)
                _endPresenter.Content = NavbarEnd;
        }

        private void UpdateAppearance()
        {
            if (_rootBorder == null) return;

            // Corner radius based on IsFullWidth
            _rootBorder.CornerRadius = IsFullWidth ? new CornerRadius(0) : new CornerRadius(12);

            // Try to get theme background, fallback to a neutral color
            if (Application.Current.Resources.TryGetValue("DaisyBase200Brush", out var bg) && bg is Brush bgBrush)
            {
                _rootBorder.Background = bgBrush;
            }
            else
            {
                _rootBorder.Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 50));
            }
        }
    }
}
