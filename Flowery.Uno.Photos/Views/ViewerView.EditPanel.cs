using Flowery.Uno.Photos.Editing;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Edit side panel: Crop, Adjust, Filters, Markup and Info tabs.
    /// </summary>
    public sealed partial class ViewerView
    {
        private enum EditTab
        {
            Crop,
            Adjust,
            Filters,
            Markup,
            Info
        }

        private sealed class AdjustRow
        {
            public required string Key { get; init; }
            public required DaisyRange Range { get; init; }
            public required TextBlock ValueText { get; init; }
            public required Func<EditState, double> Get { get; init; }
            public required Func<EditState, double, EditState> Set { get; init; }
        }

        private readonly Dictionary<EditTab, (DaisyButton Button, Border Underline)> _tabButtons = [];
        private readonly List<AdjustRow> _adjustRows = [];
        private readonly List<(string Id, Border Frame, Image Image)> _filterTiles = [];
        private readonly List<(double? Aspect, DaisyButton Button)> _aspectButtons = [];
        private EditTab _tab = EditTab.Adjust;
        private EditTab _lastEditTab = EditTab.Adjust;
        private bool _panelOpen;
        private bool _syncing;
        private DaisyRange? _straightenRange;
        private TextBlock? _straightenValue;
        private DaisyRange? _filterIntensityRange;
        private DaisyRange? _penSizeRange;
        private DaisyButton? _penButton;
        private DaisyButton? _highlighterButton;
        private readonly List<(Color Color, Border Swatch)> _swatches = [];
        private int _infoVersion;
        private int _filterThumbVersion;

        private void BuildEditPanel()
        {
            BuildTabs();
            BuildCropPanel();
            BuildAdjustPanel();
            BuildFilterPanel();
            BuildMarkupPanel();
            UpdateTabVisuals();
            SyncPanelsFromState();
        }

        private void SetPanelOpen(bool open, EditTab? tab = null)
        {
            _panelOpen = open;
            if (tab is { } t)
                _tab = t;
            if (_tab != EditTab.Info)
                _lastEditTab = _tab;

            EditPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            PhotosSettings.EditPanelOpen = open;

            var editing = open && _tab != EditTab.Info;
            EditButton.Variant = editing ? DaisyButtonVariant.Primary : DaisyButtonVariant.Neutral;
            EditButton.ButtonStyle = DaisyButtonStyle.Default;
            InfoButton.Variant = open && _tab == EditTab.Info ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost;
            InfoButton.ButtonStyle = open && _tab == EditTab.Info ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;

            if (open && _tab == EditTab.Crop)
                _pan = Vector2.Zero;

            // Mode changed: drop any crop/markup cursor until the pointer moves again.
            CanvasHost.SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Arrow);

            UpdateTabVisuals();
            OnTabShown();
            Canvas.Invalidate();
        }

        private void SelectTab(EditTab tab)
        {
            SetPanelOpen(true, tab);
        }

        private void BuildTabs()
        {
            TabBar.Children.Clear();
            TabBar.ColumnDefinitions.Clear();
            _tabButtons.Clear();

            (EditTab Tab, string Icon, string Key)[] tabs =
            [
                (EditTab.Crop, "PhotosIconCrop", "Photos_Tab_Crop"),
                (EditTab.Adjust, "PhotosIconTune", "Photos_Tab_Adjust"),
                (EditTab.Filters, "PhotosIconStylize", "Photos_Tab_Filters"),
                (EditTab.Markup, "PhotosIconMarkup", "Photos_Tab_Markup"),
                (EditTab.Info, "PhotosIconInfo", "Photos_Tab_Info")
            ];

            for (var i = 0; i < tabs.Length; i++)
            {
                var (tab, icon, key) = tabs[i];
                TabBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var label = L(key);
                var button = new DaisyButton
                {
                    Variant = DaisyButtonVariant.Ghost,
                    IconData = Icon(icon),
                    Size = DaisySize.Small,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(2, 0, 2, 0)
                };
                FlowerySizeManager.SetIgnoreGlobalSize(button, true);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, label);
                ToolTipService.SetToolTip(button, label);
                var t = tab;
                button.Click += (_, _) => SelectTab(t);

                var caption = new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 3, 0, 6),
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };

                var underline = new Border
                {
                    Height = 3,
                    Width = 22,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                    Opacity = 0
                };

                // The whole tab (icon and label) is clickable, not only the icon button.
                var stack = new StackPanel { Background = new SolidColorBrush(Colors.Transparent) };
                stack.Children.Add(button);
                stack.Children.Add(caption);
                stack.Tapped += (_, _) => SelectTab(t);
                stack.PointerEntered += (_, _) => caption.Opacity = 1;
                stack.PointerExited += (_, _) => caption.Opacity = 0.85;
                caption.Opacity = 0.85;
                var cell = new Grid { Padding = new Thickness(0, 0, 0, 4) };
                cell.Children.Add(stack);
                cell.Children.Add(underline);
                Grid.SetColumn(cell, i);
                TabBar.Children.Add(cell);
                _tabButtons[tab] = (button, underline);
            }
        }

        private void UpdateTabVisuals()
        {
            foreach (var (tab, (button, underline)) in _tabButtons)
            {
                var active = tab == _tab;
                underline.Opacity = active ? 1 : 0;
                underline.Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                button.Variant = active ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost;
                button.ButtonStyle = active ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;
            }

            CropPanel.Visibility = _tab == EditTab.Crop ? Visibility.Visible : Visibility.Collapsed;
            AdjustPanel.Visibility = _tab == EditTab.Adjust ? Visibility.Visible : Visibility.Collapsed;
            FilterPanel.Visibility = _tab == EditTab.Filters ? Visibility.Visible : Visibility.Collapsed;
            MarkupPanel.Visibility = _tab == EditTab.Markup ? Visibility.Visible : Visibility.Collapsed;
            InfoPanel.Visibility = _tab == EditTab.Info ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTabShown()
        {
            if (!_panelOpen)
                return;
            if (_tab == EditTab.Filters)
                _ = RenderFilterThumbnailsAsync();
            else if (_tab == EditTab.Info)
                _ = RefreshInfoAsync();
            UpdateZoomUi();
        }

        private void OnImageLoadedForPanel()
        {
            SyncPanelsFromState();
            OnTabShown();
        }

        /// <summary>Pushes the current edit state into all panel controls without feedback loops.</summary>
        private void SyncPanelsFromState()
        {
            _syncing = true;
            try
            {
                var state = State;
                foreach (var row in _adjustRows)
                {
                    var value = row.Get(state);
                    if (Math.Abs(row.Range.Value - value) > 0.01)
                        row.Range.Value = value;
                    row.ValueText.Text = FormatSigned(value);
                }

                if (_straightenRange != null && Math.Abs(_straightenRange.Value - state.Straighten) > 0.01)
                    _straightenRange.Value = state.Straighten;
                if (_straightenValue != null)
                    _straightenValue.Text = $"{state.Straighten:0.#}°";
                if (_filterIntensityRange != null && Math.Abs(_filterIntensityRange.Value - state.FilterIntensity) > 0.01)
                    _filterIntensityRange.Value = state.FilterIntensity;

                foreach (var (id, frame, _) in _filterTiles)
                {
                    var selected = string.Equals(id, state.Filter, StringComparison.OrdinalIgnoreCase);
                    frame.BorderBrush = selected ? WindowsAccent.Brush : new SolidColorBrush(Colors.Transparent);
                }

                foreach (var (aspect, button) in _aspectButtons)
                {
                    var active = Nullable.Equals(aspect, _cropAspect);
                    button.Variant = active ? DaisyButtonVariant.Primary : DaisyButtonVariant.Neutral;
                    button.ButtonStyle = DaisyButtonStyle.Default;
                }
            }
            finally
            {
                _syncing = false;
            }
        }

        private static string FormatSigned(double value) =>
            value > 0 ? $"+{value:0}" : value.ToString("0", CultureInfo.CurrentCulture);

        #region Crop

        private void BuildCropPanel()
        {
            CropPanel.Children.Clear();
            _aspectButtons.Clear();

            CropPanel.Children.Add(Caption("Photos_RotateFlip", new Thickness(0)));
            var transforms = new Grid { ColumnSpacing = 6 };
            for (var i = 0; i < 4; i++)
                transforms.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            (string Icon, string Key, Action Action)[] buttons =
            [
                ("PhotosIconRotateLeft", "Photos_RotateLeft", () => Rotate(-1)),
                ("PhotosIconRotateRight", "Photos_RotateRight", () => Rotate(1)),
                ("PhotosIconFlipH", "Photos_FlipHorizontal", () => ApplyState(State with { FlipH = !State.FlipH, Crop = CropMath.Mirror(State.Crop, true) })),
                ("PhotosIconFlipV", "Photos_FlipVertical", () => ApplyState(State with { FlipV = !State.FlipV, Crop = CropMath.Mirror(State.Crop, false) }))
            ];
            for (var i = 0; i < buttons.Length; i++)
            {
                var (icon, key, action) = buttons[i];
                var button = new DaisyButton
                {
                    IconData = Icon(icon),
                    Variant = DaisyButtonVariant.Neutral,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                ToolTipService.SetToolTip(button, L(key));
                button.Click += (_, _) => action();
                Grid.SetColumn(button, i);
                transforms.Children.Add(button);
            }
            CropPanel.Children.Add(transforms);

            var straightenHeader = new Grid();
            straightenHeader.Children.Add(Caption("Photos_Straighten"));
            _straightenValue = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Opacity = 0.7,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
            straightenHeader.Children.Add(_straightenValue);
            CropPanel.Children.Add(straightenHeader);
            _straightenRange = Range(-45, 45, State.Straighten, 0.5, v =>
            {
                if (_syncing)
                    return;
                ApplyState(State with { Straighten = Math.Round(v, 1) }, "straighten");
            });
            CropPanel.Children.Add(_straightenRange);

            CropPanel.Children.Add(Caption("Photos_AspectRatio"));
            var aspects = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 92, ItemHeight = 40 };
            (double? Aspect, string Label)[] presets =
            [
                (null, L("Photos_AspectFree")),
                (-1, L("Photos_AspectOriginal")),
                (1, "1:1"),
                (4.0 / 3, "4:3"),
                (3.0 / 4, "3:4"),
                (3.0 / 2, "3:2"),
                (2.0 / 3, "2:3"),
                (16.0 / 9, "16:9"),
                (9.0 / 16, "9:16")
            ];
            foreach (var (aspect, label) in presets)
            {
                var button = new DaisyButton
                {
                    Content = label,
                    Size = DaisySize.Small,
                    Variant = DaisyButtonVariant.Neutral,
                    Margin = new Thickness(0, 0, 6, 6),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                var a = aspect;
                button.Click += (_, _) => SetCropAspect(a);
                aspects.Children.Add(button);
                _aspectButtons.Add((aspect, button));
            }
            CropPanel.Children.Add(aspects);

            var reset = new DaisyButton
            {
                Content = L("Photos_ResetCrop"),
                Variant = DaisyButtonVariant.Primary,
                IconSymbol = Symbol.Refresh,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            reset.Click += (_, _) =>
            {
                _cropAspect = null;
                ApplyState(State.ResetGeometry() with { Strokes = State.Strokes });
            };
            CropPanel.Children.Add(reset);

            CropPanel.Children.Add(new TextBlock
            {
                Text = L("Photos_CropHint"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            });
        }

        private void SetCropAspect(double? aspect)
        {
            if (_image == null)
                return;
            if (aspect is < 0)
            {
                // Original: aspect of the transformed source.
                var t = EditRenderer.GetTransformedSize(new Size(_image.Width, _image.Height), State);
                aspect = t.Width / t.Height;
            }
            _cropAspect = aspect;
            if (aspect != null)
                ApplyState(State with { Crop = CreateAspectCrop(aspect) });
            SyncPanelsFromState();
            Canvas.Invalidate();
        }

        #endregion

        #region Adjust

        private void BuildAdjustPanel()
        {
            AdjustPanel.Children.Clear();
            _adjustRows.Clear();

            var auto = new DaisyButton
            {
                Content = L("Photos_AutoEnhance"),
                IconData = Icon("PhotosIconAutoFix"),
                Variant = DaisyButtonVariant.Primary,
                ButtonStyle = DaisyButtonStyle.Soft,
                Shape = DaisyButtonShape.Block,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            auto.Click += (_, _) => RunAutoEnhance();
            AdjustPanel.Children.Add(auto);

            AdjustPanel.Children.Add(CreateSection("Photos_Section_Light", "PhotosIconSun", true,
            [
                CreateAdjustRow("brightness", "Photos_Adj_Brightness", "PhotosIconSun", s => s.Brightness, (s, v) => s with { Brightness = v }),
                CreateAdjustRow("exposure", "Photos_Adj_Exposure", "PhotosIconContrast", s => s.Exposure, (s, v) => s with { Exposure = v }),
                CreateAdjustRow("contrast", "Photos_Adj_Contrast", "PhotosIconContrast", s => s.Contrast, (s, v) => s with { Contrast = v }),
                CreateAdjustRow("highlights", "Photos_Adj_Highlights", "PhotosIconSun", s => s.Highlights, (s, v) => s with { Highlights = v }),
                CreateAdjustRow("shadows", "Photos_Adj_Shadows", "PhotosIconVignette", s => s.Shadows, (s, v) => s with { Shadows = v })
            ]));

            AdjustPanel.Children.Add(CreateSection("Photos_Section_Color", "DaisyIconPalette", true,
            [
                CreateAdjustRow("saturation", "Photos_Adj_Saturation", "PhotosIconDrop", s => s.Saturation, (s, v) => s with { Saturation = v }),
                CreateAdjustRow("warmth", "Photos_Adj_Warmth", "PhotosIconThermometer", s => s.Warmth, (s, v) => s with { Warmth = v }),
                CreateAdjustRow("tint", "Photos_Adj_Tint", "PhotosIconDrop", s => s.Tint, (s, v) => s with { Tint = v })
            ]));

            AdjustPanel.Children.Add(CreateSection("Photos_Section_Detail", "PhotosIconDiamond", false,
            [
                CreateAdjustRow("sharpness", "Photos_Adj_Sharpness", "PhotosIconDiamond", s => s.Sharpness, (s, v) => s with { Sharpness = v }),
                CreateAdjustRow("clarity", "Photos_Adj_Clarity", "PhotosIconDiamond", s => s.Clarity, (s, v) => s with { Clarity = v })
            ]));

            AdjustPanel.Children.Add(CreateSection("Photos_Section_Effects", "PhotosIconVignette", false,
            [
                CreateAdjustRow("vignette", "Photos_Adj_Vignette", "PhotosIconVignette", s => s.Vignette, (s, v) => s with { Vignette = v }),
                CreateAdjustRow("fade", "Photos_Adj_Fade", "PhotosIconFilter", s => s.Fade, (s, v) => s with { Fade = v }, min: 0)
            ]));

            var reset = new DaisyButton
            {
                Content = L("Photos_ResetAdjustments"),
                Variant = DaisyButtonVariant.Primary,
                IconSymbol = Symbol.Refresh,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            reset.Click += (_, _) => ApplyState(State.ResetAdjustments());
            AdjustPanel.Children.Add(reset);

            AdjustPanel.Children.Add(new TextBlock
            {
                Text = L("Photos_AdjustHint"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            });
        }

        private static DaisyCollapse CreateSection(string titleKey, string iconKey, bool expanded, IEnumerable<UIElement> rows)
        {
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            header.Children.Add(new Viewbox
            {
                Width = 16,
                Height = 16,
                Child = new Path
                {
                    Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), Icon(iconKey) ?? string.Empty),
                    Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }
            });
            header.Children.Add(new TextBlock { Text = L(titleKey), FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });

            var content = new StackPanel { Spacing = 14, Padding = new Thickness(2, 4, 2, 8) };
            foreach (var row in rows)
                content.Children.Add(row);

            return new DaisyCollapse
            {
                Header = header,
                ExpanderContent = content,
                IsExpanded = expanded,
                Variant = DaisyCollapseVariant.Arrow,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private UIElement CreateAdjustRow(string key, string labelKey, string iconKey, Func<EditState, double> get, Func<EditState, double, EditState> set, double min = -100)
        {
            var grid = new Grid { RowSpacing = 2 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var icon = new Viewbox
            {
                Width = 14,
                Height = 14,
                Margin = new Thickness(0, 0, 8, 0),
                Opacity = 0.75,
                Child = new Path
                {
                    Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), Icon(iconKey) ?? string.Empty),
                    Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }
            };
            var label = new TextBlock { Text = L(labelKey), VerticalAlignment = VerticalAlignment.Center };
            var value = new TextBlock { Text = "0", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
            var range = Range(min, 100, get(State), 1, _ => { });
            range.Size = DaisySize.Small;
            FlowerySizeManager.SetIgnoreGlobalSize(range, true);

            var row = new AdjustRow { Key = key, Range = range, ValueText = value, Get = get, Set = set };
            range.RegisterPropertyChangedCallback(DaisyRange.ValueProperty, (_, _) =>
            {
                value.Text = FormatSigned(range.Value);
                if (_syncing)
                    return;
                ApplyState(row.Set(State, Math.Round(range.Value)), row.Key);
            });

            // Double-click the label to reset the slider.
            label.DoubleTapped += (_, _) => ApplyState(row.Set(State, 0));
            ToolTipService.SetToolTip(label, L("Photos_DoubleClickReset"));

            Grid.SetColumn(label, 1);
            Grid.SetColumn(value, 2);
            Grid.SetRow(range, 1);
            Grid.SetColumnSpan(range, 3);
            grid.Children.Add(icon);
            grid.Children.Add(label);
            grid.Children.Add(value);
            grid.Children.Add(range);
            _adjustRows.Add(row);
            return grid;
        }

        private void RunAutoEnhance()
        {
            if (_image == null)
                return;
            try
            {
                var enhanced = AutoEnhance.Apply(_image.Preview, State);
                ApplyState(enhanced);
                _history.Commit();
                _shell?.ShowToast(L("Photos_AutoEnhanceApplied"), DaisyAlertVariant.Success);
            }
            catch (Exception ex)
            {
                _shell?.ShowToast(ex.Message, DaisyAlertVariant.Error);
            }
        }

        #endregion

        #region Filters

        private void BuildFilterPanel()
        {
            FilterPanel.Children.Clear();
            _filterTiles.Clear();

            var grid = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 96, ItemHeight = 112 };
            foreach (var preset in FilterPresets.All)
            {
                var image = new Image { Stretch = Stretch.UniformToFill, Width = 84, Height = 72 };
                var imageFrame = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Width = 84,
                    Height = 72,
                    Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                    Child = image
                };
                var frame = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    Child = imageFrame,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                var label = new TextBlock
                {
                    Text = L(preset.LocalizationKey),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };
                var stack = new StackPanel { Background = new SolidColorBrush(Colors.Transparent) };
                stack.Children.Add(frame);
                stack.Children.Add(label);
                var id = preset.Id;
                stack.Tapped += (_, _) => ApplyState(State with { Filter = id, FilterIntensity = State.Filter == id ? State.FilterIntensity : 100 });
                grid.Children.Add(stack);
                _filterTiles.Add((preset.Id, frame, image));
            }
            FilterPanel.Children.Add(grid);

            var header = new Grid();
            header.Children.Add(Caption("Photos_FilterIntensity"));
            FilterPanel.Children.Add(header);
            _filterIntensityRange = Range(0, 100, State.FilterIntensity, 1, v =>
            {
                if (_syncing)
                    return;
                ApplyState(State with { FilterIntensity = Math.Round(v) }, "filterIntensity");
            });
            FilterPanel.Children.Add(_filterIntensityRange);
        }

        /// <summary>
        /// Renders small previews of every filter look applied to the current (cropped, adjusted) image.
        /// </summary>
        private async Task RenderFilterThumbnailsAsync()
        {
            if (_image == null || _filterTiles.Count == 0)
                return;

            var version = ++_filterThumbVersion;
            await Task.Yield();
            if (version != _filterThumbVersion || _image == null)
                return;

            try
            {
                var source = _image.Preview;
                var state = State with { Filter = FilterPresets.NoneId, Strokes = [] };
                var output = EditRenderer.GetOutputSize(new Size(source.Width, source.Height), state);
                var scale = Math.Min(1, 168.0 / Math.Max(output.Width, output.Height));

                using var adjusted = EditRenderer.Render(source, state, scale: scale, includeFilter: false);

                foreach (var (id, _, image) in _filterTiles)
                {
                    if (version != _filterThumbVersion)
                        return;
                    using var tile = EditRenderer.RenderFilterTile(adjusted, FilterPresets.Get(id).Matrix);
                    image.Source = ThumbnailLoader.ToImageSource(tile);
                }
            }
            catch
            {
                // Thumbnails are cosmetic.
            }
        }

        #endregion

        #region Markup

        private void BuildMarkupPanel()
        {
            MarkupPanel.Children.Clear();
            _swatches.Clear();

            MarkupPanel.Children.Add(Caption("Photos_Tool", new Thickness(0)));
            var tools = new Grid { ColumnSpacing = 6 };
            tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _penButton = new DaisyButton { Content = L("Photos_Pen"), IconData = Icon("PhotosIconMarkup"), HorizontalAlignment = HorizontalAlignment.Stretch };
            _highlighterButton = new DaisyButton { Content = L("Photos_Highlighter"), IconSymbol = Symbol.Highlight, HorizontalAlignment = HorizontalAlignment.Stretch };
            _penButton.Click += (_, _) => SetHighlighter(false);
            _highlighterButton.Click += (_, _) => SetHighlighter(true);
            Grid.SetColumn(_highlighterButton, 1);
            tools.Children.Add(_penButton);
            tools.Children.Add(_highlighterButton);
            MarkupPanel.Children.Add(tools);

            MarkupPanel.Children.Add(BuildColorSection());

            MarkupPanel.Children.Add(Caption("Photos_PenSize"));
            _penSizeRange = Range(1, 40, _penSize, 1, v => _penSize = v);
            MarkupPanel.Children.Add(_penSizeRange);

            var actions = new Grid { ColumnSpacing = 6, Margin = new Thickness(0, 6, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var undoStroke = new DaisyButton { Content = L("Photos_UndoStroke"), IconSymbol = Symbol.Undo, Variant = DaisyButtonVariant.Primary, HorizontalAlignment = HorizontalAlignment.Stretch };
            undoStroke.Click += (_, _) =>
            {
                if (!State.Strokes.IsEmpty)
                    ApplyState(State with { Strokes = State.Strokes.RemoveAt(State.Strokes.Count - 1) });
            };
            var clear = new DaisyButton { Content = L("Photos_ClearMarkup"), IconSymbol = Symbol.Clear, Variant = DaisyButtonVariant.Error, ButtonStyle = DaisyButtonStyle.Soft, HorizontalAlignment = HorizontalAlignment.Stretch };
            clear.Click += (_, _) =>
            {
                if (!State.Strokes.IsEmpty)
                    ApplyState(State with { Strokes = [] });
            };
            Grid.SetColumn(clear, 1);
            actions.Children.Add(undoStroke);
            actions.Children.Add(clear);
            MarkupPanel.Children.Add(actions);

            MarkupPanel.Children.Add(new TextBlock
            {
                Text = L("Photos_MarkupHint"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            });

            // Color management ("Advanced") spans the full width below all other markup controls.
            MarkupPanel.Children.Add(BuildAdvancedColorSection());

            UpdateMarkupTools();
        }

        private void SetHighlighter(bool highlighter)
        {
            _highlighter = highlighter;
            if (highlighter && _penSize < 14)
            {
                _penSize = 18;
                if (_penSizeRange != null)
                    _penSizeRange.Value = _penSize;
            }
            UpdateMarkupTools();
        }

        private void UpdateMarkupTools()
        {
            if (_penButton != null)
            {
                _penButton.Variant = _highlighter ? DaisyButtonVariant.Neutral : DaisyButtonVariant.Primary;
                _penButton.ButtonStyle = DaisyButtonStyle.Default;
            }
            if (_highlighterButton != null)
            {
                _highlighterButton.Variant = _highlighter ? DaisyButtonVariant.Primary : DaisyButtonVariant.Neutral;
                _highlighterButton.ButtonStyle = DaisyButtonStyle.Default;
            }
            UpdateColorVisuals();
        }

        #endregion

        #region Info

        private async Task RefreshInfoAsync()
        {
            var version = ++_infoVersion;
            InfoPanel.Children.Clear();
            if (Current is not { } item)
                return;

            var culture = PhotosLocalization.CurrentCulture;
            AddInfoHeader(item.FileName);
            AddInfoRow("Photos_Info_Folder", item.FolderPath, () => FileOperations.ShowInExplorer(item.Path));
            AddInfoRow("Photos_Info_FileSize", FileOperations.FormatFileSize(item.FileSize));
            var w = _image?.Width ?? Math.Max(0, item.PixelWidth);
            var h = _image?.Height ?? Math.Max(0, item.PixelHeight);
            if (w > 0)
                AddInfoRow("Photos_Info_Dimensions", $"{w} × {h}  ·  {(long)w * h / 1_000_000.0:0.0} MP");
            AddInfoRow("Photos_Info_Modified", item.DateModified.ToString("f", culture));

            // Skia: StorageFile.Properties is not implemented on the Uno desktop heads, so the EXIF block is parsed directly.
            var exif = await Task.Run(() => ExifDetails.Read(item.Path));
            if (version != _infoVersion)
                return;

            if (exif != null)
            {
                if (exif.DateTaken is { } taken)
                    AddInfoRow("Photos_Info_DateTaken", taken.ToString("f", culture));
                if (!string.IsNullOrWhiteSpace(exif.Camera))
                    AddInfoRow("Photos_Info_Camera", exif.Camera);
                if (!string.IsNullOrWhiteSpace(exif.Lens))
                    AddInfoRow("Photos_Info_Lens", exif.Lens);

                var exposure = ViewerFormat.Exposure(exif, culture);
                if (exposure.Length > 0)
                    AddInfoRow("Photos_Info_Exposure", exposure);

                if (exif.Latitude is { } lat && exif.Longitude is { } lon)
                {
                    var coords = string.Format(CultureInfo.InvariantCulture, "{0:0.00000}, {1:0.00000}", lat, lon);
                    AddInfoRow("Photos_Info_Location", coords, () =>
                        FileOperations.OpenUri(string.Format(CultureInfo.InvariantCulture, "https://www.openstreetmap.org/?mlat={0}&mlon={1}#map=15/{0}/{1}", lat, lon)));
                }
            }

            var albums = PhotoLibrary.Instance.Albums.Where(a => a.Paths.Contains(item.Path, PathRules.Comparer)).Select(a => a.Name).ToList();
            if (albums.Count > 0)
                AddInfoRow("Photos_Info_Albums", string.Join(", ", albums));
        }

        private void AddInfoHeader(string text)
        {
            InfoPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            });
        }

        private void AddInfoRow(string labelKey, string value, Action? onClick = null)
        {
            var panel = new StackPanel { Spacing = 2 };
            panel.Children.Add(Caption(labelKey, new Thickness(0)));
            if (onClick != null)
            {
                // A TextBlock hyperlink wraps and grows with its text (fixed-height buttons would clip).
                var hyperlink = new Microsoft.UI.Xaml.Documents.Hyperlink();
                // Skia: the default hyperlink color is unreadable on the dark themes.
                hyperlink.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                hyperlink.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = value });
                hyperlink.Click += (_, _) => onClick();
                var linkText = new TextBlock { TextWrapping = TextWrapping.Wrap };
                linkText.Inlines.Add(hyperlink);
                FlowerySizeManager.SetResponsiveFont(linkText, ResponsiveFontTier.Primary);
                panel.Children.Add(linkText);
            }
            else
            {
                panel.Children.Add(new TextBlock
                {
                    Text = value,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                });
            }
            InfoPanel.Children.Add(panel);
        }

        #endregion
    }
}
