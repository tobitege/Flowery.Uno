using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A numeric text display that animates value changes with a slide transition.
    /// </summary>
    public partial class DaisyAnimatedNumber : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private TextBlock? _prevText;
        private TextBlock? _currentText;
        private TranslateTransform? _prevTransform;
        private TranslateTransform? _currentTransform;
        private int _lastValue;
        private CancellationTokenSource? _cts;

        public DaisyAnimatedNumber()
        {
            DefaultStyleKey = typeof(DaisyAnimatedNumber);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int),
                typeof(DaisyAnimatedNumber),
                new PropertyMetadata(0, OnValueChanged));

        /// <summary>
        /// Gets or sets the value displayed by the control.
        /// </summary>
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinDigitsProperty =
            DependencyProperty.Register(
                nameof(MinDigits),
                typeof(int),
                typeof(DaisyAnimatedNumber),
                new PropertyMetadata(0, OnFormatChanged));

        /// <summary>
        /// Gets or sets the minimum digit count (pads with leading zeros).
        /// </summary>
        public int MinDigits
        {
            get => (int)GetValue(MinDigitsProperty);
            set => SetValue(MinDigitsProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(DaisyAnimatedNumber),
                new PropertyMetadata(TimeSpan.FromMilliseconds(250)));

        /// <summary>
        /// Gets or sets the animation duration.
        /// </summary>
        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty SlideDistanceProperty =
            DependencyProperty.Register(
                nameof(SlideDistance),
                typeof(double),
                typeof(DaisyAnimatedNumber),
                new PropertyMetadata(18.0));

        /// <summary>
        /// Gets or sets the slide distance used during transition.
        /// </summary>
        public double SlideDistance
        {
            get => (double)GetValue(SlideDistanceProperty);
            set => SetValue(SlideDistanceProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyAnimatedNumber),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAnimatedNumber num)
            {
                num.AnimateValueChange((int)e.OldValue, (int)e.NewValue);
            }
        }

        private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAnimatedNumber num)
            {
                num.RefreshText();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAnimatedNumber num)
            {
                num.ApplyAll();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyAll();
                return;
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

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            _rootGrid = new Grid();

            // Previous text (for outgoing animation)
            _prevTransform = new TranslateTransform();
            _prevText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = _prevTransform,
                Opacity = 0
            };

            // Current text
            _currentTransform = new TranslateTransform();
            _currentText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = _currentTransform,
                Opacity = 1
            };

            _rootGrid.Children.Add(_prevText);
            _rootGrid.Children.Add(_currentText);

            Content = _rootGrid;

            _lastValue = Value;
            RefreshText();
        }

        #endregion

        #region Animation

        private void RefreshText()
        {
            var text = Format(Value);
            if (_currentText != null)
            {
                _currentText.Text = text;
            }
            if (_prevText != null)
            {
                _prevText.Text = text;
                _prevText.Opacity = 0;
            }
        }

        private async void AnimateValueChange(int oldValue, int newValue)
        {
            if (_currentText == null || _prevText == null ||
                _currentTransform == null || _prevTransform == null)
            {
                _lastValue = Value;
                return;
            }

            if (oldValue == newValue)
                return;

            // Cancel any existing animation
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _lastValue = newValue;

            var isIncreasing = newValue > oldValue;
            var distance = SlideDistance;

            _prevText.Text = Format(oldValue);
            _currentText.Text = Format(newValue);

            _prevText.Opacity = 1;
            _currentText.Opacity = 0;

            _prevTransform.Y = 0;
            _currentTransform.Y = isIncreasing ? distance : -distance;

            try
            {
                await AnimateAsync(isIncreasing, distance, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested) return;

            _prevText.Opacity = 0;
            _currentText.Opacity = 1;
            _prevTransform.Y = 0;
            _currentTransform.Y = 0;
        }

        private async Task AnimateAsync(bool isIncreasing, double distance, CancellationToken ct)
        {
            var startTime = DateTime.Now;

            while (DateTime.Now - startTime < Duration)
            {
                ct.ThrowIfCancellationRequested();

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var t = Math.Min(1.0, elapsed / Duration.TotalMilliseconds);

                // Cubic ease out
                var easedT = 1 - Math.Pow(1 - t, 3);

                if (_prevText != null && _currentText != null &&
                    _prevTransform != null && _currentTransform != null)
                {
                    _prevText.Opacity = 1.0 - easedT;
                    _currentText.Opacity = easedT;

                    _prevTransform.Y = Lerp(0, isIncreasing ? -distance : distance, easedT);
                    _currentTransform.Y = Lerp(isIncreasing ? distance : -distance, 0, easedT);
                }

                await Task.Delay(16, ct);
            }
        }

        private static double Lerp(double from, double to, double t)
        {
            return from + (to - from) * t;
        }

        private string Format(int value)
        {
            var text = value.ToString();
            if (MinDigits > 0)
            {
                text = text.PadLeft(MinDigits, '0');
            }
            return text;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_currentText == null || _prevText == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            // Animated numbers use header-style font sizes
            double fontSize = resources == null
                ? DaisyResourceLookup.GetDefaultHeaderFontSize(Size)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}HeaderFontSize",
                    DaisyResourceLookup.GetDefaultHeaderFontSize(Size));

            _currentText.FontSize = fontSize;
            _prevText.FontSize = fontSize;
            _currentText.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            _prevText.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        }

        private void ApplyColors()
        {
            // Check for lightweight styling overrides
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAnimatedNumber", "Foreground");

            var foreground = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            if (_currentText != null)
            {
                _currentText.Foreground = foreground;
            }
            if (_prevText != null)
            {
                _prevText.Foreground = foreground;
            }
        }

        #endregion
    }
}
