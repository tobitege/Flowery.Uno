using Flowery.Controls.ColorPicker;
using Flowery.Uno.Photos.Editing;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using SkiaSharp;
using Windows.Storage.Pickers;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Markup color management in the spirit of Paint Shop Pro's materials palette:
    /// primary/secondary colors (left/right mouse button) with swap, presets, persistent custom colors,
    /// recent colors, eyedropper, and an "Advanced" section with color adjustments, harmonies,
    /// palette extraction from the photo, JASC-PAL import/export and a full color editor.
    /// </summary>
    public sealed partial class ViewerView
    {
        private static readonly Color White = Color.FromArgb(255, 255, 255, 255);

        private static readonly Color[] PresetColors =
        [
            Color.FromArgb(255, 239, 68, 68),
            Color.FromArgb(255, 249, 115, 22),
            Color.FromArgb(255, 250, 204, 21),
            Color.FromArgb(255, 34, 197, 94),
            Color.FromArgb(255, 14, 165, 233),
            Color.FromArgb(255, 99, 102, 241),
            Color.FromArgb(255, 236, 72, 153),
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(255, 15, 23, 42)
        ];

        private readonly List<(int Index, Border Swatch)> _customSwatches = [];
        private readonly List<Color> _recentColors = [];
        private DaisyButton? _eyedropperButton;
        private DaisyColorWheel? _colorWheel;
        private DaisyColorEditor? _colorEditor;
        private Border? _primaryPreview;
        private Border? _secondaryPreview;
        private VariableSizedWrapGrid? _recentPanel;
        private TextBlock? _recentCaption;
        private bool _eyedropperActive;
        private bool _syncingColor;
        private int _lastCustomIndex = -1;
        private Color _secondaryColor = PhotosSettings.SecondaryColor;

        private bool IsEyedropperActive => _eyedropperActive && IsMarkupMode;

        private UIElement BuildColorSection()
        {
            var section = new StackPanel { Spacing = 8 };

            // Header: caption, primary/secondary pair with swap, eyedropper
            var header = new Grid { ColumnSpacing = 8 };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var caption = Caption("Photos_Color");
            caption.VerticalAlignment = VerticalAlignment.Center;

            _secondaryPreview = CreateColorSquare();
            _secondaryPreview.Margin = new Thickness(12, 12, 0, 0);
            _primaryPreview = CreateColorSquare();
            _primaryPreview.Margin = new Thickness(0, 0, 12, 12);
            Tip(_primaryPreview, L("Photos_PrimaryColor"));
            Tip(_secondaryPreview, L("Photos_SecondaryColor"));
            _primaryPreview.Tapped += (_, e) =>
            {
                e.Handled = true;
                _ = EditPrimarySecondaryAsync(primary: true);
            };
            _secondaryPreview.Tapped += (_, e) =>
            {
                e.Handled = true;
                _ = EditPrimarySecondaryAsync(primary: false);
            };
            var pair = new Grid { Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center };
            pair.Children.Add(_secondaryPreview);
            pair.Children.Add(_primaryPreview);

            var swap = new DaisyButton
            {
                IconData = Icon("PhotosIconSwap"),
                Shape = DaisyButtonShape.Square,
                Size = DaisySize.Small,
                Variant = DaisyButtonVariant.Ghost,
                VerticalAlignment = VerticalAlignment.Center
            };
            Tip(swap, L("Photos_SwapColors"));
            swap.Click += (_, _) => SwapColors();

            _eyedropperButton = new DaisyButton
            {
                IconData = Icon("PhotosIconEyedropper"),
                Shape = DaisyButtonShape.Square,
                Size = DaisySize.Small,
                Variant = DaisyButtonVariant.Neutral,
                VerticalAlignment = VerticalAlignment.Center
            };
            Tip(_eyedropperButton, L("Photos_Eyedropper"));
            _eyedropperButton.Click += (_, _) => SetEyedropper(!_eyedropperActive);

            Grid.SetColumn(pair, 1);
            Grid.SetColumn(swap, 2);
            Grid.SetColumn(_eyedropperButton, 3);
            header.Children.Add(caption);
            header.Children.Add(pair);
            header.Children.Add(swap);
            header.Children.Add(_eyedropperButton);
            section.Children.Add(header);

            // Presets
            _swatches.Clear();
            var presets = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 36, ItemHeight = 36 };
            foreach (var color in PresetColors)
            {
                var swatch = CreateSwatch(color);
                var c = color;
                swatch.Tapped += (_, _) => SetPenColor(c);
                swatch.RightTapped += (_, e) =>
                {
                    e.Handled = true;
                    SetSecondaryColor(c);
                };
                presets.Children.Add(swatch);
                _swatches.Add((color, swatch));
            }
            section.Children.Add(presets);

            // Custom colors
            var customCaption = Caption("Photos_CustomColors");
            ToolTipService.SetToolTip(customCaption, L("Photos_CustomColorHint"));
            section.Children.Add(customCaption);
            _customSwatches.Clear();
            var custom = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 36, ItemHeight = 36 };
            for (var i = 0; i < PhotosSettings.CustomColors.Length; i++)
            {
                var index = i;
                var swatch = CreateSwatch(PhotosSettings.CustomColors[i]);
                Tip(swatch, L("Photos_CustomColorHint"));
                swatch.Tapped += (_, _) =>
                {
                    _lastCustomIndex = index;
                    SetPenColor(PhotosSettings.CustomColors[index]);
                };
                swatch.DoubleTapped += (_, e) =>
                {
                    e.Handled = true;
                    _ = EditCustomColorAsync(index);
                };
                swatch.RightTapped += (s, e) =>
                {
                    e.Handled = true;
                    var menu = new MenuFlyout();
                    menu.Items.Add(MenuItem("Photos_EditColor", null, () => _ = EditCustomColorAsync(index), Icon("DaisyIconPalette")));
                    menu.Items.Add(MenuItem("Photos_StoreCurrentColor", Symbol.Save, () => StoreCustomColor(index, _penColor)));
                    menu.Items.Add(MenuItem("Photos_SecondaryColor", null, () => SetSecondaryColor(PhotosSettings.CustomColors[index]), Icon("PhotosIconSwap")));
                    menu.Items.Add(MenuItem("Photos_ResetColor", Symbol.Refresh, () => StoreCustomColor(index, White)));
                    menu.ShowAt((FrameworkElement)s, new FlyoutShowOptions { Position = e.GetPosition((UIElement)s) });
                };
                custom.Children.Add(swatch);
                _customSwatches.Add((index, swatch));
            }
            section.Children.Add(custom);

            // Recent colors (filled while drawing)
            _recentCaption = Caption("Photos_RecentColors");
            _recentPanel = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 36, ItemHeight = 36 };
            section.Children.Add(_recentCaption);
            section.Children.Add(_recentPanel);
            RebuildRecentColors();

            UpdateColorVisuals();
            return section;
        }

        /// <summary>
        /// Full-width "Advanced" section placed below all other markup controls.
        /// </summary>
        private UIElement BuildAdvancedColorSection()
        {
            var content = new StackPanel { Spacing = 10, Padding = new Thickness(2, 4, 2, 8) };

            content.Children.Add(Caption("Photos_ColorAdjust", new Thickness(0)));
            content.Children.Add(ButtonGrid(
            [
                ("Photos_Color_Lighter", () => SetPenColor(ColorTools.AdjustLightness(_penColor, 0.08))),
                ("Photos_Color_Darker", () => SetPenColor(ColorTools.AdjustLightness(_penColor, -0.08))),
                ("Photos_Color_MoreSaturation", () => SetPenColor(ColorTools.AdjustSaturation(_penColor, 0.1))),
                ("Photos_Color_LessSaturation", () => SetPenColor(ColorTools.AdjustSaturation(_penColor, -0.1))),
                ("Photos_Color_Complement", () => SetPenColor(ColorTools.RotateHue(_penColor, 180))),
                ("Photos_Color_Invert", () => SetPenColor(ColorTools.Invert(_penColor))),
                ("Photos_Color_Grayscale", () => SetPenColor(ColorTools.Grayscale(_penColor))),
                ("Photos_Color_Warmer", () => SetPenColor(ColorTools.Warmer(_penColor)))
            ]));

            content.Children.Add(Caption("Photos_Harmonies"));
            content.Children.Add(ButtonGrid(
            [
                ("Photos_Harmony_Complementary", () => ApplyHarmony(ColorTools.Harmony.Complementary)),
                ("Photos_Harmony_Triad", () => ApplyHarmony(ColorTools.Harmony.Triad)),
                ("Photos_Harmony_Analogous", () => ApplyHarmony(ColorTools.Harmony.Analogous)),
                ("Photos_Harmony_Monochrome", () => ApplyHarmony(ColorTools.Harmony.Monochrome))
            ]));

            content.Children.Add(Caption("Photos_Palette"));
            // The palette actions have the longest labels ("Effacer les couleurs personnalisées"): one per row, so none is cut off.
            content.Children.Add(ButtonGrid(
            [
                ("Photos_Palette_Extract", ExtractPaletteFromPhoto),
                ("Photos_Palette_AddCurrent", () => StoreCustomColor(NextCustomSlot(), _penColor)),
                ("Photos_Palette_Import", () => _ = ImportPaletteAsync()),
                ("Photos_Palette_Export", () => _ = ExportPaletteAsync()),
                ("Photos_Palette_Clear", ClearCustomColors)
            ], columns: 1));

            content.Children.Add(Caption("Photos_ColorEditor"));
            _colorWheel = new DaisyColorWheel
            {
                Width = 180,
                Height = 180,
                HorizontalAlignment = HorizontalAlignment.Center,
                Color = _penColor
            };
            _colorWheel.ColorChanged += (_, e) =>
            {
                if (_syncingColor)
                    return;
                SetPenColor(Color.FromArgb(_penColor.A, e.Color.R, e.Color.G, e.Color.B), source: _colorWheel);
            };
            _colorEditor = new DaisyColorEditor
            {
                ShowAlphaChannel = true,
                ShowHexInput = true,
                ShowRgbSliders = true,
                ShowHslSliders = true,
                EditingMode = ColorEditingMode.Hsl,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Color = _penColor
            };
            _colorEditor.ColorChanged += (_, e) =>
            {
                if (_syncingColor)
                    return;
                SetPenColor(e.Color, source: _colorEditor);
            };
            content.Children.Add(_colorWheel);
            content.Children.Add(_colorEditor);

            return new DaisyCollapse
            {
                Header = new TextBlock { Text = L("Photos_Advanced"), FontWeight = FontWeights.SemiBold },
                ExpanderContent = content,
                IsExpanded = false,
                Variant = DaisyCollapseVariant.Arrow,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };
        }

        private static Grid ButtonGrid(IReadOnlyList<(string Key, Action Action)> items, int columns = 2)
        {
            var grid = new Grid { ColumnSpacing = 6, RowSpacing = 6 };
            for (var c = 0; c < columns; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var i = 0; i < items.Count; i++)
            {
                if (i % columns == 0)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var (key, action) = items[i];
                var button = new DaisyButton
                {
                    Content = L(key),
                    Size = DaisySize.Small,
                    Variant = DaisyButtonVariant.Primary,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                Tip(button, L(key));
                button.Click += (_, _) => action();
                Grid.SetRow(button, i / columns);
                Grid.SetColumn(button, i % columns);
                grid.Children.Add(button);
            }
            return grid;
        }

        private static Border CreateSwatch(Color color) => new()
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(color),
            BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Colors.Transparent)
        };

        private static Border CreateColorSquare() => new()
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(2),
            BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase100Brush"),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        private void SetPenColor(Color color, object? source = null)
        {
            _penColor = color;
            _syncingColor = true;
            try
            {
                if (_colorWheel != null && !ReferenceEquals(source, _colorWheel))
                    _colorWheel.Color = Color.FromArgb(255, color.R, color.G, color.B);
                if (_colorEditor != null && !ReferenceEquals(source, _colorEditor))
                    _colorEditor.Color = color;
            }
            finally
            {
                _syncingColor = false;
            }
            UpdateColorVisuals();
        }

        private void SetSecondaryColor(Color color)
        {
            _secondaryColor = color;
            PhotosSettings.SecondaryColor = color;
            PhotosSettings.SaveOptions();
            UpdateColorVisuals();
        }

        private void SwapColors()
        {
            var primary = _penColor;
            SetPenColor(_secondaryColor);
            SetSecondaryColor(primary);
        }

        private async Task EditPrimarySecondaryAsync(bool primary)
        {
            if (XamlRoot == null)
                return;
            var result = await DaisyColorPickerDialog.ShowAsync(XamlRoot, primary ? _penColor : _secondaryColor);
            if (result is not { } color)
                return;
            if (primary)
                SetPenColor(color);
            else
                SetSecondaryColor(color);
        }

        private void UpdateColorVisuals()
        {
            var selectedBrush = WindowsAccent.Brush;
            var normalBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            foreach (var (color, swatch) in _swatches)
                swatch.BorderBrush = color == _penColor ? selectedBrush : normalBrush;
            foreach (var (index, swatch) in _customSwatches)
            {
                var color = PhotosSettings.CustomColors[index];
                swatch.Background = new SolidColorBrush(color);
                swatch.BorderBrush = color == _penColor && index == _lastCustomIndex ? selectedBrush : normalBrush;
            }
            if (_primaryPreview != null)
                _primaryPreview.Background = new SolidColorBrush(_penColor);
            if (_secondaryPreview != null)
                _secondaryPreview.Background = new SolidColorBrush(_secondaryColor);
            if (_eyedropperButton != null)
            {
                _eyedropperButton.Variant = _eyedropperActive ? DaisyButtonVariant.Primary : DaisyButtonVariant.Neutral;
                _eyedropperButton.ButtonStyle = DaisyButtonStyle.Default;
            }
        }

        /// <summary>Remembers a color used for drawing (most recent first, max 9).</summary>
        private void AddRecentColor(Color color)
        {
            _recentColors.Remove(color);
            _recentColors.Insert(0, color);
            if (_recentColors.Count > 9)
                _recentColors.RemoveRange(9, _recentColors.Count - 9);
            RebuildRecentColors();
        }

        private void RebuildRecentColors()
        {
            if (_recentPanel == null || _recentCaption == null)
                return;
            _recentPanel.Children.Clear();
            foreach (var color in _recentColors)
            {
                var swatch = CreateSwatch(color);
                swatch.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                var c = color;
                swatch.Tapped += (_, _) => SetPenColor(c);
                swatch.RightTapped += (_, e) =>
                {
                    e.Handled = true;
                    SetSecondaryColor(c);
                };
                _recentPanel.Children.Add(swatch);
            }
            var visible = _recentColors.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            _recentCaption.Visibility = visible;
            _recentPanel.Visibility = visible;
        }

        private int NextCustomSlot()
        {
            var free = Array.FindIndex(PhotosSettings.CustomColors, c => c == White);
            if (free >= 0)
                return free;
            return _lastCustomIndex >= 0 ? _lastCustomIndex : 0;
        }

        private void StoreCustomColor(int index, Color color)
        {
            PhotosSettings.CustomColors[index] = color;
            PhotosSettings.SaveOptions();
            _lastCustomIndex = index;
            UpdateColorVisuals();
        }

        private void StoreCustomColors(IReadOnlyList<Color> colors)
        {
            for (var i = 0; i < PhotosSettings.CustomColors.Length; i++)
                PhotosSettings.CustomColors[i] = i < colors.Count ? colors[i] : White;
            PhotosSettings.SaveOptions();
            _lastCustomIndex = -1;
            UpdateColorVisuals();
        }

        private void ClearCustomColors() => StoreCustomColors([]);

        private void ApplyHarmony(ColorTools.Harmony harmony)
        {
            StoreCustomColors(ColorTools.CreateHarmony(_penColor, harmony));
            _shell?.ShowToast(L("Photos_HarmonyApplied"), DaisyAlertVariant.Success);
        }

        private void ExtractPaletteFromPhoto()
        {
            if (_image == null)
                return;
            try
            {
                var source = _image.Preview;
                var output = EditRenderer.GetOutputSize(new Size(source.Width, source.Height), State);
                using var edited = EditRenderer.Render(source, State, scale: Math.Min(1, 256 / Math.Max(output.Width, output.Height)));
                var colors = ColorTools.ExtractDominantColors(edited, PhotosSettings.CustomColors.Length);
                StoreCustomColors(colors);
                _shell?.ShowToast(PhotosLocalization.Format("Photos_Palette_Extracted", colors.Length), DaisyAlertVariant.Success);
            }
            catch (Exception ex)
            {
                _shell?.ShowToast(ex.Message, DaisyAlertVariant.Error);
            }
        }

        private async Task ExportPaletteAsync()
        {
            if (_shell == null)
                return;
            try
            {
                var picker = new FileSavePicker { SuggestedFileName = "Flowery Photos", DefaultFileExtension = ".pal" };
                picker.FileTypeChoices.Add("Paint Shop Pro (JASC-PAL)", [".pal"]);
                var result = await picker.PickSaveFileAsync();
                if (result?.Path is not { Length: > 0 } picked)
                    return;
                // Skia: the Linux picker omits the extension.
                var path = FileOperations.EnsureExtension(picked, ".pal");
                List<Color> colors = [_penColor, _secondaryColor, .. PhotosSettings.CustomColors, .. _recentColors];
                ColorTools.WriteJascPalette(path, colors.Distinct());
                _shell.ShowToast(PhotosLocalization.Format("Photos_Palette_Exported", System.IO.Path.GetFileName(path)), DaisyAlertVariant.Success);
            }
            catch (Exception ex)
            {
                _shell.ShowToast(ex.Message, DaisyAlertVariant.Error);
            }
        }

        private async Task ImportPaletteAsync()
        {
            if (_shell == null)
                return;
            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".pal");
                picker.FileTypeFilter.Add(".gpl");
                var result = await picker.PickSingleFileAsync();
                if (result?.Path is not { Length: > 0 } path)
                    return;
                var colors = ColorTools.ReadPalette(path);
                if (colors.Count == 0)
                {
                    _shell.ShowToast(L("Photos_ActionFailed"), DaisyAlertVariant.Error);
                    return;
                }
                StoreCustomColors(colors);
                _shell.ShowToast(PhotosLocalization.Format("Photos_Palette_Imported", Math.Min(colors.Count, PhotosSettings.CustomColors.Length)), DaisyAlertVariant.Success);
            }
            catch (Exception ex)
            {
                _shell.ShowToast(ex.Message, DaisyAlertVariant.Error);
            }
        }

        private async Task EditCustomColorAsync(int index)
        {
            if (XamlRoot == null)
                return;
            var result = await DaisyColorPickerDialog.ShowAsync(XamlRoot, PhotosSettings.CustomColors[index]);
            if (result is not { } color)
                return;
            StoreCustomColor(index, color);
            SetPenColor(color);
        }

        private void SetEyedropper(bool active)
        {
            _eyedropperActive = active;
            UpdateColorVisuals();
            if (active)
                _shell?.ShowToast(L("Photos_EyedropperHint"), DaisyAlertVariant.Info);
            CanvasHost.SetCursor(active ? Microsoft.UI.Input.InputSystemCursorShape.Cross : Microsoft.UI.Input.InputSystemCursorShape.Arrow);
        }

        /// <summary>
        /// Samples the edited image color under a screen point.
        /// </summary>
        private Color? SampleColor(ViewTransform t, Point p)
        {
            var r = t.ScreenRect;
            if (p.X < r.X || p.Y < r.Y || p.X > r.Right || p.Y > r.Bottom)
                return null;

            // Skia: one render at the size on screen, read back on the CPU.
            var state = DisplayState;
            var scale = EditRenderer.GetPreviewScale(t.Scale * RasterScale);
            using var edited = EditRenderer.Render(t.Source, state, applyCrop: !IsCropMode, scale: scale);
            using var bitmap = SKBitmap.FromImage(edited);
            var x = Math.Clamp((int)((p.X - r.X) / r.Width * bitmap.Width), 0, bitmap.Width - 1);
            var y = Math.Clamp((int)((p.Y - r.Y) / r.Height * bitmap.Height), 0, bitmap.Height - 1);
            var pixel = bitmap.GetPixel(x, y);
            return Color.FromArgb(255, pixel.Red, pixel.Green, pixel.Blue);
        }
    }
}
