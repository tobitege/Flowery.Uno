namespace Flowery.Controls
{
    /// <summary>
    /// An Alert control styled after DaisyUI's Alert component.
    /// Displays informational messages with variant-based coloring.
    /// </summary>
    public partial class DaisyAlert : DaisyBaseContentControl
    {
        private Border? _alertBorder;
        private Microsoft.UI.Xaml.Shapes.Path? _iconElement;
        private ContentPresenter? _contentPresenter;
        private object? _userContent;

        public DaisyAlert()
        {
            DefaultStyleKey = typeof(DaisyAlert);
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyAlert),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyAlertVariant),
                typeof(DaisyAlert),
                new PropertyMetadata(DaisyAlertVariant.Info, OnAppearanceChanged));

        public DaisyAlertVariant Variant
        {
            get => (DaisyAlertVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(DaisyAlert),
                new PropertyMetadata(null, OnIconChanged));

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(
                nameof(ShowIcon),
                typeof(bool),
                typeof(DaisyAlert),
                new PropertyMetadata(true, OnAppearanceChanged));

        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(DaisyAlert),
                new PropertyMetadata(null, OnMessageChanged));

        public string? Message
        {
            get => (string?)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAlert alert)
            {
                alert.ApplyAll();
            }
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAlert alert)
            {
                alert.UpdateIcon();
            }
        }

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAlert alert)
            {
                alert._userContent = e.NewValue as string;
                if (alert._contentPresenter != null)
                {
                    alert._contentPresenter.Content = alert._userContent;
                }
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            // Capture user content before we replace Content with our visual tree.
            if (Content != null && !ReferenceEquals(Content, _alertBorder))
            {
                _userContent = Content;
                // Detach before re-parenting into our ContentPresenter (Uno throws if an element has 2 parents).
                base.Content = null;
            }
            else if (!string.IsNullOrEmpty(Message))
            {
                _userContent = Message;
            }

            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _alertBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            if (_alertBorder != null && _contentPresenter != null)
            {
                _contentPresenter.Content = _userContent;
                return;
            }

            _alertBorder = new Border
            {
                Padding = new Thickness(16, 12, 16, 12),
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _iconElement = new Microsoft.UI.Xaml.Shapes.Path
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform
            };
            Grid.SetColumn(_iconElement, 0);
            rootGrid.Children.Add(_iconElement);

            _contentPresenter = new ContentPresenter
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = _userContent
            };
            Grid.SetColumn(_contentPresenter, 1);
            rootGrid.Children.Add(_contentPresenter);

            _alertBorder.Child = rootGrid;
            base.Content = _alertBorder;
        }

        private void UpdateIcon()
        {
            if (_iconElement == null) return;

            if (Icon is Geometry geometry)
            {
                _iconElement.Data = geometry;
            }
            else if (Icon is string pathData)
            {
                _iconElement.Data = FloweryPathHelpers.ParseGeometry(pathData);
            }
            else if (Icon == null)
            {
                // Use default icon based on variant
                _iconElement.Data = GetDefaultIconForVariant();
            }
        }

        private PathGeometry GetDefaultIconForVariant()
        {
            // Return simple geometric shapes for each variant
            // These are simplified - ideally use actual icon resources
            return Variant switch
            {
                DaisyAlertVariant.Info => GetInfoIconGeometry(),
                DaisyAlertVariant.Success => GetSuccessIconGeometry(),
                DaisyAlertVariant.Warning => GetWarningIconGeometry(),
                DaisyAlertVariant.Error => GetErrorIconGeometry(),
                _ => GetInfoIconGeometry()
            };
        }

        private static PathGeometry GetInfoIconGeometry()
        {
            // Circle with "i" - using a standard info icon path (circle outline with i inside)
            // M12,2 C6.48,2 2,6.48 2,12 C2,17.52 6.48,22 12,22 C17.52,22 22,17.52 22,12 C22,6.48 17.52,2 12,2 Z
            // M13,17 L11,17 L11,11 L13,11 L13,17 Z M13,9 L11,9 L11,7 L13,7 L13,9 Z
            var pathGeometry = new PathGeometry();

            // Outer circle
            var circleFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(12, 2),
                IsClosed = true,
                IsFilled = true
            };
            // Draw circle using bezier curves (approximation)
            circleFigure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(6.48, 2),
                Point2 = new Windows.Foundation.Point(2, 6.48),
                Point3 = new Windows.Foundation.Point(2, 12)
            });
            circleFigure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(2, 17.52),
                Point2 = new Windows.Foundation.Point(6.48, 22),
                Point3 = new Windows.Foundation.Point(12, 22)
            });
            circleFigure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(17.52, 22),
                Point2 = new Windows.Foundation.Point(22, 17.52),
                Point3 = new Windows.Foundation.Point(22, 12)
            });
            circleFigure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(22, 6.48),
                Point2 = new Windows.Foundation.Point(17.52, 2),
                Point3 = new Windows.Foundation.Point(12, 2)
            });
            pathGeometry.Figures.Add(circleFigure);

            // "i" body (rectangle from 11,11 to 13,17)
            var iBodyFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(11, 11),
                IsClosed = true,
                IsFilled = true
            };
            iBodyFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 11) });
            iBodyFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 17) });
            iBodyFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(11, 17) });
            pathGeometry.Figures.Add(iBodyFigure);

            // "i" dot (rectangle from 11,7 to 13,9)
            var iDotFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(11, 7),
                IsClosed = true,
                IsFilled = true
            };
            iDotFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 7) });
            iDotFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 9) });
            iDotFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(11, 9) });
            pathGeometry.Figures.Add(iDotFigure);

            // Use EvenOdd fill rule so the "i" cuts out from the circle
            pathGeometry.FillRule = FillRule.EvenOdd;

            return pathGeometry;
        }

        private static PathGeometry GetSuccessIconGeometry()
        {
            // Filled checkmark shape (thick stroke converted to filled path)
            // Creates a proper checkmark with both wings
            var pathGeometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(9, 16.17),
                IsClosed = true,
                IsFilled = true
            };
            // Standard Material Design checkmark path
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(4.83, 12) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(3.41, 13.41) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(9, 19) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(21, 7) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(19.59, 5.59) });
            pathGeometry.Figures.Add(figure);
            return pathGeometry;
        }

        private static PathGeometry GetWarningIconGeometry()
        {
            // Triangle with exclamation mark inside
            var pathGeometry = new PathGeometry();

            // Outer triangle
            var triangleFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(12, 2),
                IsClosed = true,
                IsFilled = true
            };
            triangleFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(22, 20) });
            triangleFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(2, 20) });
            pathGeometry.Figures.Add(triangleFigure);

            // Exclamation mark body (rectangle)
            var exclamBodyFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(11, 8),
                IsClosed = true,
                IsFilled = true
            };
            exclamBodyFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 8) });
            exclamBodyFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 14) });
            exclamBodyFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(11, 14) });
            pathGeometry.Figures.Add(exclamBodyFigure);

            // Exclamation mark dot (small rectangle)
            var exclamDotFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(11, 16),
                IsClosed = true,
                IsFilled = true
            };
            exclamDotFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 16) });
            exclamDotFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13, 18) });
            exclamDotFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(11, 18) });
            pathGeometry.Figures.Add(exclamDotFigure);

            // Use EvenOdd fill rule so the exclamation mark cuts out from the triangle
            pathGeometry.FillRule = FillRule.EvenOdd;

            return pathGeometry;
        }

        private static PathGeometry GetErrorIconGeometry()
        {
            // Filled X/cross shape (two crossed rectangles)
            var pathGeometry = new PathGeometry();

            // Create X as a single closed path with proper thickness
            // This creates a proper filled X shape
            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(19, 6.41),
                IsClosed = true,
                IsFilled = true
            };
            // Standard Material Design close/X icon path
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(17.59, 5) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(12, 10.59) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(6.41, 5) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(5, 6.41) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(10.59, 12) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(5, 17.59) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(6.41, 19) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(12, 13.41) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(17.59, 19) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(19, 17.59) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(13.41, 12) });
            pathGeometry.Figures.Add(figure);

            return pathGeometry;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_alertBorder == null)
                return;

            ApplySizing();
            ApplyColors();
            UpdateIcon();
            UpdateIconVisibility();
        }

        private void ApplySizing()
        {
            if (_alertBorder == null || _iconElement == null)
                return;

            var size = Size;

            // Padding scales with size
            var padding = size switch
            {
                DaisySize.ExtraSmall => new Thickness(8, 6, 8, 6),
                DaisySize.Small => new Thickness(10, 8, 10, 8),
                DaisySize.Medium => new Thickness(14, 10, 14, 10),
                DaisySize.Large => new Thickness(16, 12, 16, 12),
                DaisySize.ExtraLarge => new Thickness(18, 14, 18, 14),
                _ => new Thickness(14, 10, 14, 10)
            };
            _alertBorder.Padding = padding;

            // Corner radius scales with size
            var cornerRadius = DaisyResourceLookup.GetDefaultCornerRadius(size);
            _alertBorder.CornerRadius = cornerRadius;

            // Icon size scales
            var iconSize = size switch
            {
                DaisySize.ExtraSmall => 14d,
                DaisySize.Small => 18d,
                DaisySize.Medium => 22d,
                DaisySize.Large => 26d,
                DaisySize.ExtraLarge => 30d,
                _ => 22d
            };
            _iconElement.Width = iconSize;
            _iconElement.Height = iconSize;

            // Icon margin scales
            var iconMargin = size switch
            {
                DaisySize.ExtraSmall => new Thickness(0, 0, 6, 0),
                DaisySize.Small => new Thickness(0, 0, 8, 0),
                DaisySize.Medium => new Thickness(0, 0, 10, 0),
                DaisySize.Large => new Thickness(0, 0, 12, 0),
                DaisySize.ExtraLarge => new Thickness(0, 0, 14, 0),
                _ => new Thickness(0, 0, 10, 0)
            };
            _iconElement.Margin = iconMargin;

            // Font size
            FontSize = DaisyResourceLookup.GetDefaultFontSize(size);
        }

        private void ApplyColors()
        {
            if (_alertBorder == null || _iconElement == null)
                return;

            var variantName = Variant switch
            {
                DaisyAlertVariant.Info => "Info",
                DaisyAlertVariant.Success => "Success",
                DaisyAlertVariant.Warning => "Warning",
                DaisyAlertVariant.Error => "Error",
                _ => "Info"
            };

            var paletteBrushKey = $"Daisy{variantName}Brush";

            // Check for user-defined lightweight styling overrides first
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAlert", $"{variantName}Background");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAlert", $"{variantName}Foreground");
            var iconOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAlert", $"{variantName}IconBrush");

            if (bgOverride != null)
            {
                // User has defined a complete override
                _alertBorder.Background = bgOverride;
                _alertBorder.BorderBrush = iconOverride ?? DaisyResourceLookup.GetBrush(paletteBrushKey);
            }
            else
            {
                // Use solid variant background (like buttons) - more predictable than semi-transparent
                var paletteColor = DaisyResourceLookup.GetBrush(paletteBrushKey);
                _alertBorder.Background = paletteColor;
                _alertBorder.BorderBrush = paletteColor;
            }
            _alertBorder.BorderThickness = new Thickness(2);

            // Use variant's content brush for icon and text (matching button styling)
            var contentBrushKey = $"Daisy{variantName}ContentBrush";
            var contentBrush = iconOverride ?? DaisyResourceLookup.GetBrush(contentBrushKey);

            _iconElement.Fill = contentBrush;
            Foreground = fgOverride ?? contentBrush;
        }

        private void UpdateIconVisibility()
        {
            if (_iconElement == null) return;
            _iconElement.Visibility = ShowIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion
    }
}
