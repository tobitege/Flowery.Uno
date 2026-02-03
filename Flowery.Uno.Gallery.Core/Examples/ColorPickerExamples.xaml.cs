using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Flowery.Controls.ColorPicker;
using PickerColorChangedEventArgs = Flowery.Controls.ColorPicker.ColorChangedEventArgs;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class ColorPickerExamples : ScrollableExamplePage
    {
        private bool _isDialogUpdating;
        private Color _dialogSelectedColor = Colors.Red;

        public bool IsScreenPickerSupported => !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS();

        public ColorPickerExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private static string FormatRgbHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void ColorWheel1_ColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            if (ColorWheelValue1 != null)
                ColorWheelValue1.Text = $"Color: {FormatRgbHex(e.Color)}";
        }

        private void ColorGrid1_ColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            if (ColorGridValue1 != null)
                ColorGridValue1.Text = $"Selected: {FormatRgbHex(e.Color)}";
        }

        private void ScreenPicker1_ColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            if (ScreenPickerValue1 != null)
                ScreenPickerValue1.Text = $"Picked: {FormatRgbHex(e.Color)}";
        }

        private void OpenDialogButton_Click(object? sender, RoutedEventArgs e)
        {
            var currentColor = SelectedColorPreview?.Background is SolidColorBrush brush
                ? brush.Color
                : Colors.Red;

            InitializeDialogColors(currentColor);

            if (ColorPickerModal != null)
                ColorPickerModal.IsOpen = true;
        }

        private void DialogOkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedColorPreview != null)
                SelectedColorPreview.Background = new SolidColorBrush(_dialogSelectedColor);

            if (SelectedColorText != null)
                SelectedColorText.Text = $"Selected: {FormatRgbHex(_dialogSelectedColor)}";

            if (ColorPickerModal != null)
                ColorPickerModal.IsOpen = false;
        }

        private void DialogCancelButton_Click(object? sender, RoutedEventArgs e)
        {
            if (ColorPickerModal != null)
                ColorPickerModal.IsOpen = false;
        }

        private void DialogColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            SetDialogColor(e.Color);
        }

        private void InitializeDialogColors(Color color)
        {
            if (DialogOriginalColorPreview != null)
                DialogOriginalColorPreview.Background = new SolidColorBrush(color);

            if (DialogSelectedColorPreview != null)
                DialogSelectedColorPreview.Background = new SolidColorBrush(color);

            if (DialogSelectedColorText != null)
                DialogSelectedColorText.Text = FormatRgbHex(color);

            SetDialogColor(color);
        }

        private void SetDialogColor(Color color)
        {
            if (_isDialogUpdating)
                return;

            _isDialogUpdating = true;
            try
            {
                _dialogSelectedColor = color;

                if (DialogColorWheel != null && DialogColorWheel.Color != color)
                    DialogColorWheel.Color = color;

                if (DialogColorGrid != null && DialogColorGrid.Color != color)
                    DialogColorGrid.Color = color;

                if (DialogColorEditor != null && DialogColorEditor.Color != color)
                    DialogColorEditor.Color = color;

                if (DialogScreenPicker != null && DialogScreenPicker.Color != color)
                    DialogScreenPicker.Color = color;

                if (DialogSelectedColorPreview != null)
                    DialogSelectedColorPreview.Background = new SolidColorBrush(color);

                if (DialogSelectedColorText != null)
                    DialogSelectedColorText.Text = FormatRgbHex(color);
            }
            finally
            {
                _isDialogUpdating = false;
            }
        }
    }
}
