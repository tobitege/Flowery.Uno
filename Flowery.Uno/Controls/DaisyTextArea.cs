using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// A TextBox control styled after DaisyUI's Textarea component (multiline).
    /// Extends DaisyInput with textarea-specific features like character counting and auto-grow.
    /// </summary>
    public partial class DaisyTextArea : DaisyInput
    {

        public DaisyTextArea()
        {
            AcceptsReturn = true;
            TextWrapping = TextWrapping.Wrap;
            MinHeight = 80;
            VerticalContentAlignment = VerticalAlignment.Top;

            TextChanged += OnTextAreaTextChanged;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            UpdateCharacterCountDescription();
            RefreshLayout();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RefreshLayout();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            RefreshLayout();
        }

        private void OnTextAreaTextChanged(DaisyInput sender, TextChangedEventArgs e)
        {
            CharacterCount = Text?.Length ?? 0;
            UpdateCharacterCountDescription();
        }

        private void RefreshLayout()
        {
            // Ensure vertical padding exists for TextArea (DaisyInput defaults to 0 top/bottom)
            if (Padding.Top < 4)
            {
                var side = Padding.Left > 0 ? Padding.Left : 12;
                Padding = new Thickness(side, 12, side, 12);
            }

            UpdateRowHeight();
            ApplyAutoGrow();
        }

        private void UpdateCharacterCountDescription()
        {
            if (!ShowCharacterCount) return;

            if (MaxLength > 0)
            {
                Description = $"{CharacterCount} / {MaxLength}";
            }
            else
            {
                Description = $"{CharacterCount} characters";
            }
        }

        private void ApplyAutoGrow()
        {
            if (IsAutoGrow)
            {
                // Force native auto-grow by clearing explicit Height
                // (DaisyInput base class sets Height to fixed size, so we must unset it)
                Height = double.NaN;
            }
        }

        #region Character Counter Properties

        public static readonly DependencyProperty ShowCharacterCountProperty =
            DependencyProperty.Register(
                nameof(ShowCharacterCount),
                typeof(bool),
                typeof(DaisyTextArea),
                new PropertyMetadata(false, OnShowCharacterCountChanged));

        public bool ShowCharacterCount
        {
            get => (bool)GetValue(ShowCharacterCountProperty);
            set => SetValue(ShowCharacterCountProperty, value);
        }

        private static void OnShowCharacterCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTextArea ta)
            {
                if ((bool)e.NewValue)
                {
                    ta.UpdateCharacterCountDescription();
                }
                else
                {
                    ta.Description = null;
                }
            }
        }

        public static readonly DependencyProperty CharacterCountProperty =
            DependencyProperty.Register(
                nameof(CharacterCount),
                typeof(int),
                typeof(DaisyTextArea),
                new PropertyMetadata(0));

        public int CharacterCount
        {
            get => (int)GetValue(CharacterCountProperty);
            private set => SetValue(CharacterCountProperty, value);
        }

        #endregion

        #region Auto-Grow Properties

        public static readonly DependencyProperty IsAutoGrowProperty =
            DependencyProperty.Register(
                nameof(IsAutoGrow),
                typeof(bool),
                typeof(DaisyTextArea),
                new PropertyMetadata(false, OnAutoGrowChanged));

        public bool IsAutoGrow
        {
            get => (bool)GetValue(IsAutoGrowProperty);
            set => SetValue(IsAutoGrowProperty, value);
        }

        private static void OnAutoGrowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTextArea ta)
            {
                ta.RefreshLayout();
            }
        }

        #endregion

        #region Resize Control Properties

        public static readonly DependencyProperty CanResizeProperty =
            DependencyProperty.Register(
                nameof(CanResize),
                typeof(bool),
                typeof(DaisyTextArea),
                new PropertyMetadata(true));

        public bool CanResize
        {
            get => (bool)GetValue(CanResizeProperty);
            set => SetValue(CanResizeProperty, value);
        }

        #endregion

        #region Row/Line Properties

        public static readonly DependencyProperty MinRowsProperty =
            DependencyProperty.Register(
                nameof(MinRows),
                typeof(int),
                typeof(DaisyTextArea),
                new PropertyMetadata(3, OnRowsChanged));

        public int MinRows
        {
            get => (int)GetValue(MinRowsProperty);
            set => SetValue(MinRowsProperty, value);
        }

        public static readonly DependencyProperty MaxRowsProperty =
            DependencyProperty.Register(
                nameof(MaxRows),
                typeof(int),
                typeof(DaisyTextArea),
                new PropertyMetadata(0, OnRowsChanged)); // 0 means no limit

        public int MaxRows
        {
            get => (int)GetValue(MaxRowsProperty);
            set => SetValue(MaxRowsProperty, value);
        }

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTextArea ta)
            {
                ta.RefreshLayout();
            }
        }

        private void UpdateRowHeight()
        {
            // Approximate line height based on font size.
            // Using 1.55 multiplier to ensure lines don't get cut off.
            double lineHeight = FontSize * 1.55;
            double verticalPadding = Padding.Top + Padding.Bottom;
            double borderHeight = BorderThickness.Top + BorderThickness.Bottom;

            // Calculate desired min height based on MinRows
            double desiredMin = (MinRows * lineHeight) + verticalPadding + borderHeight;

            // If AutoGrow is enabled, rely on MinRows for the floor.
            // Otherwise, maintain a sensible default minimum (80) for fixed-height feel.
            if (IsAutoGrow)
            {
                MinHeight = desiredMin;
            }
            else
            {
                MinHeight = Math.Max(80, desiredMin);
            }

            // Set MaxHeight based on MaxRows
            if (MaxRows > 0)
            {
                // Add extra buffer to prevent scrollbar from appearing at exactly MaxRows.
                // +12px ensures the last line is fully visible and not cut off.
                MaxHeight = (MaxRows * lineHeight) + verticalPadding + borderHeight + 12;
            }
            else
            {
                MaxHeight = double.PositiveInfinity;
            }
        }

        #endregion
    }
}
