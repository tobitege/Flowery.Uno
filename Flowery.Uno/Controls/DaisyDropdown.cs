using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Flowery.Controls
{
    /// <summary>
    /// Event args for dropdown selection changes.
    /// </summary>
    public partial class DaisyDropdownSelectionChangedEventArgs(object? selectedItem) : EventArgs
    {
        public object? SelectedItem { get; } = selectedItem;
    }

    /// <summary>
    /// A lightweight dropdown menu control built on a Popup.
    /// </summary>
    public partial class DaisyDropdown : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Button? _triggerButton;
        private Popup? _popup;
        private Border? _popupBorder;
        private ListView? _menuList;
        private TextBlock? _triggerText;
        private Microsoft.UI.Xaml.Shapes.Path? _chevron;

        public DaisyDropdown()
        {
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

        #region Accessibility
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyDropdown),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDropdown dropdown)
            {
                dropdown.UpdateAutomationProperties();
            }
        }
        #endregion

        #region ItemsSource
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(DaisyDropdown),
                new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>
        /// Gets or sets the dropdown items.
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDropdown dropdown)
                dropdown.UpdateMenuItems();
        }
        #endregion

        #region SelectedItem
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(DaisyDropdown),
                new PropertyMetadata(null, OnSelectedItemChanged));

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDropdown dropdown)
                dropdown.UpdateTriggerText();
        }
        #endregion

        #region PlaceholderText
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(DaisyDropdown),
                new PropertyMetadata("Select", OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the placeholder text displayed when no item is selected.
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }
        #endregion

        #region IsOpen
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(DaisyDropdown),
                new PropertyMetadata(false, OnIsOpenChanged));

        /// <summary>
        /// Gets or sets whether the dropdown is open.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDropdown dropdown)
            {
                if (dropdown._popup != null)
                {
                    if (dropdown.IsOpen)
                    {
                        try
                        {
                            if (dropdown._popup.XamlRoot == null && dropdown.XamlRoot != null)
                            {
                                dropdown._popup.XamlRoot = dropdown.XamlRoot;
                            }
                        }
                        catch
                        {
                            // Ignore if XamlRoot is not accessible on this backend.
                        }
                    }
                    dropdown._popup.IsOpen = dropdown.IsOpen;
                    dropdown.UpdateChevron();
                }
            }
        }
        #endregion

        #region CloseOnSelection
        public static readonly DependencyProperty CloseOnSelectionProperty =
            DependencyProperty.Register(
                nameof(CloseOnSelection),
                typeof(bool),
                typeof(DaisyDropdown),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether selecting an item closes the dropdown.
        /// </summary>
        public bool CloseOnSelection
        {
            get => (bool)GetValue(CloseOnSelectionProperty);
            set => SetValue(CloseOnSelectionProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyDropdown),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of this dropdown.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyColor),
                typeof(DaisyDropdown),
                new PropertyMetadata(DaisyColor.Default, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the color variant.
        /// </summary>
        public DaisyColor Variant
        {
            get => (DaisyColor)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        /// <summary>
        /// Raised when the selected item changes.
        /// </summary>
        public event EventHandler<DaisyDropdownSelectionChangedEventArgs>? SelectedItemChanged;

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDropdown dropdown)
                dropdown.ApplyAll();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid();

            // Trigger button
            _triggerButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            var buttonContent = new Grid();
            buttonContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _triggerText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(_triggerText, 0);
            buttonContent.Children.Add(_triggerText);

            _chevron = new Microsoft.UI.Xaml.Shapes.Path
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
            };
            Grid.SetColumn(_chevron, 1);
            buttonContent.Children.Add(_chevron);
            UpdateChevron();

            _triggerButton.Content = buttonContent;
            _triggerButton.Click += OnTriggerClick;
            _rootGrid.Children.Add(_triggerButton);

            // Popup
            _popup = new Popup
            {
                IsLightDismissEnabled = true
            };

            _popupBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4),
                MinWidth = 150
            };

            _menuList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single
            };
            _menuList.SelectionChanged += OnMenuSelectionChanged;

            _popupBorder.Child = _menuList;
            _popup.Child = _popupBorder;
            _popup.Closed += OnPopupClosed;
            _rootGrid.Children.Add(_popup);

            Content = _rootGrid;
            UpdateMenuItems();
        }

        private void OnTriggerClick(object sender, RoutedEventArgs e)
        {
            IsOpen = !IsOpen;
        }

        private void OnPopupClosed(object? sender, object e)
        {
            if (IsOpen)
            {
                IsOpen = false;
            }
        }

        private void OnMenuSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_menuList == null) return;

            var item = _menuList.SelectedItem;
            if (!Equals(SelectedItem, item) && item != null)
            {
                SelectedItem = item;
                SelectedItemChanged?.Invoke(this, new DaisyDropdownSelectionChangedEventArgs(item));
            }

            if (CloseOnSelection)
            {
                IsOpen = false;
            }
        }

        private void UpdateMenuItems()
        {
            if (_menuList == null) return;

            _menuList.Items.Clear();
            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                {
                    _menuList.Items.Add(item);
                }
            }
        }

        private void UpdateTriggerText()
        {
            if (_triggerText == null) return;

            _triggerText.Text = SelectedItem?.ToString() ?? PlaceholderText;
            UpdateAutomationProperties();
        }

        private void UpdateChevron()
        {
            if (_chevron == null) return;
            var iconKey = IsOpen ? "DaisyIconChevronUp" : "DaisyIconChevronDown";
            var pathData = FloweryPathHelpers.GetIconPathData(iconKey)
                ?? (IsOpen ? FloweryPathHelpers.ChevronUpPath : FloweryPathHelpers.ChevronDownPath);
            FloweryPathHelpers.TrySetPathData(_chevron, () => FloweryPathHelpers.ParseGeometry(pathData));
        }

        private void ApplyAll()
        {
            if (_rootGrid == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            var effectiveSize = FlowerySizeManager.ShouldIgnoreGlobalSize(this) 
                ? Size 
                : FlowerySizeManager.CurrentSize;

            // Get sizing from centralized defaults (tokens guaranteed by EnsureDefaults)
            double height = DaisyResourceLookup.GetDefaultHeight(effectiveSize);
            double fontSize = DaisyResourceLookup.GetDefaultFontSize(effectiveSize);
            var cornerRadius = DaisyResourceLookup.GetDefaultCornerRadius(effectiveSize);
            var padding = DaisyResourceLookup.GetDefaultPadding(effectiveSize);

            // Get theme colors
            var base100Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush");
            var base200Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush");
            var base300Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
            var baseContentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");

            // Get variant brush
            Brush buttonBg = Variant switch
            {
                DaisyColor.Primary => DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush"),
                DaisyColor.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush"),
                DaisyColor.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush"),
                DaisyColor.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush"),
                DaisyColor.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush"),
                DaisyColor.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush"),
                DaisyColor.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush"),
                _ => base200Brush
            };

            Brush buttonFg = Variant switch
            {
                DaisyColor.Primary => DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryContentBrush"),
                DaisyColor.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryContentBrush"),
                DaisyColor.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentContentBrush"),
                DaisyColor.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoContentBrush"),
                DaisyColor.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessContentBrush"),
                DaisyColor.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningContentBrush"),
                DaisyColor.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorContentBrush"),
                _ => baseContentBrush
            };

            // Apply to trigger button
            if (_triggerButton != null)
            {
                _triggerButton.Height = height;
                _triggerButton.Padding = padding;
                _triggerButton.CornerRadius = cornerRadius;
                _triggerButton.Background = buttonBg;
                _triggerButton.Foreground = buttonFg;
                _triggerButton.BorderBrush = base300Brush;
                _triggerButton.BorderThickness = new Thickness(1);
                UpdateAutomationProperties();
            }

            if (_triggerText != null)
            {
                _triggerText.FontSize = fontSize;
                _triggerText.Foreground = buttonFg;
                UpdateTriggerText();
            }

            if (_chevron != null)
            {
                _chevron.Fill = buttonFg;
            }

            // Apply to popup
            if (_popupBorder != null)
            {
                _popupBorder.Background = base100Brush;
                _popupBorder.BorderBrush = base300Brush;
                _popupBorder.BorderThickness = new Thickness(1);
                _popupBorder.CornerRadius = cornerRadius;
            }

            // Apply sizing to menu items in the ListView
            if (_menuList != null)
            {
                // Get menu-specific sizing tokens
                var sizeKey = DaisyResourceLookup.GetSizeKeyFull(effectiveSize);
                double menuFontSize = DaisyResourceLookup.GetDouble($"DaisyMenu{sizeKey}FontSize", fontSize);
                var menuPadding = DaisyResourceLookup.GetThickness($"DaisyMenu{sizeKey}Padding", new Thickness(8, 4, 8, 4));

                _menuList.FontSize = menuFontSize;
                _menuList.Foreground = baseContentBrush;
                _menuList.Padding = new Thickness(0);

                // Apply padding to list items via ItemContainerStyle
                var itemStyle = new Style(typeof(ListViewItem));
                itemStyle.Setters.Add(new Setter(ListViewItem.PaddingProperty, menuPadding));
                itemStyle.Setters.Add(new Setter(ListViewItem.MinHeightProperty, height));
                itemStyle.Setters.Add(new Setter(ListViewItem.ForegroundProperty, baseContentBrush));
                _menuList.ItemContainerStyle = itemStyle;
            }
        }

        private void UpdateAutomationProperties()
        {
            if (_triggerButton == null)
                return;

            var name = !string.IsNullOrWhiteSpace(AccessibleText)
                ? AccessibleText
                : (SelectedItem?.ToString() ?? PlaceholderText);

            DaisyAccessibility.SetAutomationNameOrClear(_triggerButton, name);
        }

    }
}
