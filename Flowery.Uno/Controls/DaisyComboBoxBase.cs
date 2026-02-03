using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.Toolkit.UI;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using ToolkitShadow = Uno.Toolkit.UI.Shadow;
using ToolkitShadowCollection = Uno.Toolkit.UI.ShadowCollection;

namespace Flowery.Controls
{
    /// <summary>
    /// Base class for Daisy ComboBox-derived controls.
    /// Re-implemented as a custom ContentControl with Button+Popup architecture
    /// to avoid platform-specific bugs and limitations of the native ComboBox.
    /// </summary>
    [ContentProperty(Name = nameof(Items))]
    public abstract partial class DaisyComboBoxBase : ContentControl
    {
        protected readonly DaisyControlLifecycle _lifecycle;
        private Grid? _rootGrid;
        private ShadowContainer? _shadowContainer;
        private ToolkitShadowCollection? _shadowCollection;
        private ToolkitShadow? _darkShadow;
        private ToolkitShadow? _lightShadow;
        private Button? _triggerButton;
        private Popup? _popup;
        private Border? _popupBorder;
        private ListView? _listView;
        private ListBox? _listBox;
        private Microsoft.UI.Xaml.Shapes.Path? _chevron;
        protected bool _isInteracting;
        protected bool _isPointerOver;
        protected Brush? _pointerOverBorderBrush;
        protected bool _isUnloading;
        protected bool _lifecycleLoaded;
        private bool _pendingPointerSelection;
        private bool _itemsControlEventsAttached;
        private bool _applyAllQueued;
        private Thickness _menuItemPadding = DaisyResourceLookup.GetDefaultMenuPadding(DaisySize.Medium);
        private double _menuItemFontSize = DaisyResourceLookup.GetDefaultFontSize(DaisySize.Medium);
        private double _menuItemMinHeight = DaisyResourceLookup.GetDefaultHeight(DaisySize.Medium);
        private readonly TappedEventHandler _listViewItemTappedHandler;
        private static ResourceDictionary? s_daisyControlsResources;
        private static bool s_daisyControlsLoaded;
        private ObservableCollection<object>? _items;
        private bool _neumorphicSubscribed;

        protected DaisyComboBoxBase() : this(true)
        {
        }

        protected DaisyComboBoxBase(bool subscribeSizeChanges)
        {
            IsTabStop = false;
            // Initialize Items collection
            _items = new ObservableCollection<object>();
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            // Apply initial global size before creating lifecycle and building visual tree.
            // This ensures BuildVisualTree() and its internal ApplyAll() call use correct dimensions from the start.
            if (subscribeSizeChanges && FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
            {
                Size = FlowerySizeManager.CurrentSize;
            }

            _lifecycle = new DaisyControlLifecycle(
                this,
                OnThemeUpdated,
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false,
                subscribeSizeChanges: subscribeSizeChanges);

            _listViewItemTappedHandler = OnListViewItemTapped;
            Loaded += OnBaseLoaded;
            Unloaded += OnBaseUnloaded;

            // Build early so the visual tree exists before first measure (Android layout quirk).
            BuildVisualTree();
        }

        #region Properties

        public static readonly DependencyProperty SizeProperty =
             DependencyProperty.Register(nameof(Size), typeof(DaisySize), typeof(DaisyComboBoxBase), new PropertyMetadata(DaisySize.Medium, OnUnifiedPropertyChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(DaisyComboBoxBase), new PropertyMetadata(null, OnItemsSourceChanged));

        public object? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(DaisyComboBoxBase), new PropertyMetadata(null));

        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate?)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DaisyComboBoxBase), new PropertyMetadata(null, OnSelectedItemChanged));

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(DaisyComboBoxBase), new PropertyMetadata(-1, OnSelectedIndexChanged));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(DaisyComboBoxBase), new PropertyMetadata(false, OnIsDropDownOpenChanged));

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(DaisyComboBoxBase), new PropertyMetadata(null));

        public string? DisplayMemberPath
        {
            get => (string?)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(DaisyComboBoxBase), new PropertyMetadata(null, OnPlaceholderTextChanged));

        public string? PlaceholderText
        {
            get => (string?)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static readonly DependencyProperty ItemContainerStyleProperty =
           DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(DaisyComboBoxBase), new PropertyMetadata(null, OnItemContainerStyleChanged));

        public Style? ItemContainerStyle
        {
             get => (Style?)GetValue(ItemContainerStyleProperty);
             set => SetValue(ItemContainerStyleProperty, value);
        }

        public IList<object> Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new ObservableCollection<object>();
                }
                return _items;
            }
        }

        public bool? NeumorphicEnabled
        {
            get => DaisyNeumorphic.GetIsEnabled(this);
            set => DaisyNeumorphic.SetIsEnabled(this, value);
        }

        public DaisyNeumorphicMode? NeumorphicMode
        {
            get => DaisyNeumorphic.GetMode(this);
            set => DaisyNeumorphic.SetMode(this, value);
        }

        public double? NeumorphicIntensity
        {
            get => DaisyNeumorphic.GetIntensity(this);
            set => DaisyNeumorphic.SetIntensity(this, value);
        }

        public double NeumorphicBlurRadius
        {
            get => DaisyNeumorphic.GetBlurRadius(this);
            set => DaisyNeumorphic.SetBlurRadius(this, value);
        }

        public double NeumorphicOffset
        {
            get => DaisyNeumorphic.GetOffset(this);
            set => DaisyNeumorphic.SetOffset(this, value);
        }

        public Color? NeumorphicDarkShadowColor
        {
            get => DaisyNeumorphic.GetDarkShadowColor(this);
            set => DaisyNeumorphic.SetDarkShadowColor(this, value);
        }

        public Color? NeumorphicLightShadowColor
        {
            get => DaisyNeumorphic.GetLightShadowColor(this);
            set => DaisyNeumorphic.SetLightShadowColor(this, value);
        }

        public event SelectionChangedEventHandler? SelectionChanged;

        #endregion

        #region Accessibility
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyComboBoxBase),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyComboBoxBase combo)
            {
                combo.UpdateAutomationProperties();
            }
        }
        #endregion

        #region Internal Properties
        // For backwards compatibility or subclasses that check this
        protected ListView? ListView => _listView;
        protected virtual bool UseListBox => false;
        protected virtual bool UseCustomItemContainerTemplate => true;
        private Selector? SelectorControl => (Selector?)_listView ?? _listBox;
        private ItemsControl? ItemsControl => (ItemsControl?)_listView ?? _listBox;
        #endregion

        #region Event Handlers

        private static void OnUnifiedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyComboBoxBase combo) combo.RequestApplyAll();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             if (d is DaisyComboBoxBase combo)
             {
                 // Suppress selection change events during ItemsSource updates to prevent
                 // SelectedItem from being cleared and causing template-mismatch errors.
                 var wasInteracting = combo._isInteracting;
                 combo._isInteracting = true;
                 try
                 {
                     var itemsSource = e.NewValue ?? combo.Items;
                     if (combo._listView != null)
                     {
                         combo._listView.ItemsSource = itemsSource;
                         // On Android, forcing layout update helps ensure items are realized.
                         // Defer it to avoid COMException on Windows during property changed callbacks.
                         combo.DispatcherQueue?.TryEnqueue(() => combo._listView?.UpdateLayout());
                     }

                     if (combo._listBox != null)
                     {
                         combo._listBox.ItemsSource = itemsSource;
                         combo.DispatcherQueue?.TryEnqueue(() => combo._listBox?.UpdateLayout());
                     }
                 }
                 finally
                 {
                     combo._isInteracting = wasInteracting;
                 }
             }
        }

        private static void OnItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyComboBoxBase combo)
            {
                combo.UpdateItemContainerStyle();
            }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyComboBoxBase combo)
            {
                if (Equals(e.NewValue, e.OldValue)) return;

                // Guard: On Desktop/Skia, focus loss or popup close can trigger "ghost" empty selection changes.
                // If we already have a valid selection, ignore incoming null/empty string updates.
                bool isEmpty = e.NewValue == null || (e.NewValue is string s && string.IsNullOrEmpty(s));
                if (isEmpty && combo.SelectedItem != null)
                {
                    return;
                }

                // Suppress re-entrant selection changes from internal ListView
                var wasInteracting = combo._isInteracting;
                combo._isInteracting = true;
                try
                {
                    combo.SyncSelectionFromItem(e.NewValue);
                    combo.UpdateTriggerContent();
                    combo.RefreshSelectionVisualsIfOpen();
                    combo.RaiseSelectionChanged(e.OldValue, e.NewValue);
                    combo.OnSelectionChanged(e.NewValue);
                }
                finally
                {
                    combo._isInteracting = wasInteracting;
                }
            }
        }

        protected virtual void OnSelectionChanged(object? newItem) { }

        /// <summary>
        /// Override this to provide direct text for the trigger button instead of using DisplayMemberPath binding.
        /// Return null to use the default binding/ToString behavior.
        /// </summary>
        protected virtual string? GetTriggerDisplayText() => null;

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyComboBoxBase combo)
            {
                var wasInteracting = combo._isInteracting;
                combo._isInteracting = true;
                try
                {
                    combo.SyncSelectionFromIndex((int)e.NewValue);
                }
                finally
                {
                    combo._isInteracting = wasInteracting;
                }
            }
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyComboBoxBase combo)
            {
                combo.UpdateTriggerContent();
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             if (d is DaisyComboBoxBase combo)
             {
                 combo.UpdatePopupState();
                 if ((bool)e.NewValue)
                     combo.OnDropDownOpened();
                 else
                     combo.OnDropDownClosed();
             }
        }

        protected virtual void OnDropDownOpened()
        {
            DropDownOpened?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDropDownClosed()
        {
             DropDownClosed?.Invoke(this, EventArgs.Empty);

             // Force refresh trigger content after popup closes - fixes Desktop/Skia display issue
             // where the trigger button content becomes empty after popup interaction
             DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
             {
                 if (IsLoaded && !_isUnloading)
                 {
                     UpdateTriggerContent();
                 }
             });
        }

        public event EventHandler<object>? DropDownOpened;
        public event EventHandler<object>? DropDownClosed;

        #endregion

        #region Lifecycle & Visual Tree

        private void OnBaseLoaded(object sender, RoutedEventArgs e)
        {
            _isUnloading = false;
            OnBeforeLoaded();
            BuildVisualTree();
            EnsureItemsControlSubscriptions();
            if (!_neumorphicSubscribed)
            {
                DaisyBaseContentControl.GlobalNeumorphicChanged += OnGlobalNeumorphicChanged;
                _neumorphicSubscribed = true;
            }
            if (!_lifecycleLoaded)
            {
                _lifecycleLoaded = true;
                _lifecycle.HandleLoaded();
            }

            // Critical for WinUI 3 / Windows App SDK: Popup must have XamlRoot set
            if (_popup != null && this.XamlRoot != null)
            {
                _popup.XamlRoot = this.XamlRoot;
            }
            if (_popup != null)
            {
                _popup.PlacementTarget = (FrameworkElement?)_triggerButton ?? this;
            }

            // Always call UpdateTriggerContent after loading to ensure selection display is current
            // Initial selection sync
            if (SelectedIndex >= 0) SyncSelectionFromIndex(SelectedIndex);
            else if (SelectedItem != null) SyncSelectionFromItem(SelectedItem);
            else UpdateTriggerContent();

            OnAfterLoaded();
            RequestNeumorphicRefresh();
        }

        private void EnsureItemsControlSubscriptions()
        {
            if (_itemsControlEventsAttached)
            {
                return;
            }

            if (_listView != null)
            {
                _listView.SelectionChanged += OnListViewSelectionChanged;
                _listView.KeyDown += OnItemsControlKeyDown;
                _listView.ContainerContentChanging += OnContainerContentChanging;
                _listView.ItemClick += OnListViewItemClick;
                _listView.PointerPressed += OnItemsControlPointerPressed;
                _listView.PointerReleased += OnItemsControlPointerReleased;
                _listView.PointerCanceled += OnItemsControlPointerCanceled;
                _listView.PointerCaptureLost += OnItemsControlPointerCanceled;
            }
            if (_listBox != null)
            {
                _listBox.SelectionChanged += OnListViewSelectionChanged;
                _listBox.KeyDown += OnItemsControlKeyDown;
                _listBox.PointerPressed += OnItemsControlPointerPressed;
                _listBox.PointerReleased += OnItemsControlPointerReleased;
                _listBox.PointerCanceled += OnItemsControlPointerCanceled;
                _listBox.PointerCaptureLost += OnItemsControlPointerCanceled;
            }

            _itemsControlEventsAttached = true;
        }

        private void OnBaseUnloaded(object sender, RoutedEventArgs e)
        {
            _isUnloading = true;

            // Unsubscribe from internal events to prevent leaks
            if (_listView != null)
            {
                _listView.SelectionChanged -= OnListViewSelectionChanged;
                _listView.KeyDown -= OnItemsControlKeyDown;
                _listView.ContainerContentChanging -= OnContainerContentChanging;
                _listView.ItemClick -= OnListViewItemClick;
                _listView.PointerPressed -= OnItemsControlPointerPressed;
                _listView.PointerReleased -= OnItemsControlPointerReleased;
                _listView.PointerCanceled -= OnItemsControlPointerCanceled;
                _listView.PointerCaptureLost -= OnItemsControlPointerCanceled;
            }
            if (_listBox != null)
            {
                _listBox.SelectionChanged -= OnListViewSelectionChanged;
                _listBox.KeyDown -= OnItemsControlKeyDown;
                _listBox.PointerPressed -= OnItemsControlPointerPressed;
                _listBox.PointerReleased -= OnItemsControlPointerReleased;
                _listBox.PointerCanceled -= OnItemsControlPointerCanceled;
                _listBox.PointerCaptureLost -= OnItemsControlPointerCanceled;
            }
            _itemsControlEventsAttached = false;

            if (_neumorphicSubscribed)
            {
                DaisyBaseContentControl.GlobalNeumorphicChanged -= OnGlobalNeumorphicChanged;
                _neumorphicSubscribed = false;
            }

            OnBeforeUnloaded();
            if (_lifecycleLoaded)
            {
                _lifecycleLoaded = false;
                _lifecycle.HandleUnloaded();
            }
            if (_popup != null) _popup.IsOpen = false;
            DetachPopupContentForUnload();
            OnAfterUnloaded();
        }

        protected virtual void OnBeforeLoaded() { }
        protected virtual void OnAfterLoaded() { }
        protected virtual void OnBeforeUnloaded() { }
        protected virtual void OnAfterUnloaded() { }

        internal void EnsureBuiltForDebug(string context)
        {
            if (_isUnloading)
            {
                return;
            }

            BuildVisualTree();
        }

        public void EnsureInitializedForSidebar(string context)
        {
            if (_isUnloading)
            {
                return;
            }

            BuildVisualTree();

            // Force lifecycle loaded if not already handled
            if (_lifecycle != null && !_lifecycleLoaded)
            {
                _lifecycleLoaded = true;
                _lifecycle.HandleLoaded();
            }

            // Always call OnBeforeLoaded to ensure items are populated
            OnBeforeLoaded();

            // Force layout update for Android
            InvalidateMeasure();
            InvalidateArrange();

            if (_popup != null && XamlRoot != null && _popup.XamlRoot == null)
            {
                _popup.XamlRoot = XamlRoot;
            }

            if (SelectedIndex >= 0) SyncSelectionFromIndex(SelectedIndex);
            else if (SelectedItem != null) SyncSelectionFromItem(SelectedItem);
            else UpdateTriggerContent();

            OnAfterLoaded();
        }

        private void BuildVisualTree()
        {
            if (_isUnloading)
            {
                return;
            }
            if (_rootGrid != null)
            {
                var popupBorder = _popupBorder;
                if (popupBorder != null && popupBorder.Child == null)
                {
                    var existingItems = (UIElement?)_listView ?? _listBox;
                    if (existingItems != null)
                    {
                        DetachFromParentIfNeeded(existingItems);
                        popupBorder.Child = existingItems;
                    }
                }
                return;
            }

            try
            {
                // 1. Create Trigger Button
                _triggerButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Padding = new Thickness(12, 0, 30, 0)
                };
                var triggerTemplate = TryGetItemContainerTemplate("DaisyComboBoxTriggerButtonTemplate");
                if (triggerTemplate != null)
                {
                    _triggerButton.Template = triggerTemplate;
                }
                _triggerButton.SetBinding(Button.FontSizeProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontSize)) });
                _triggerButton.SetBinding(Button.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
                _triggerButton.SetBinding(Button.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
                _triggerButton.SetBinding(Button.FontWeightProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontWeight)) });

                _triggerButton.PointerEntered += OnTriggerPointerEntered;
                _triggerButton.PointerExited += OnTriggerPointerExited;
                _triggerButton.PointerCanceled += OnTriggerPointerExited;
                _triggerButton.KeyDown += OnTriggerKeyDown;
                _triggerButton.Click += (s, e) =>
                {
                    if (_isInteracting) return;
                    try
                    {
                        _isInteracting = true;
                        IsDropDownOpen = !IsDropDownOpen;
                    }
                    finally
                    {
                        _isInteracting = false;
                    }
                };

                // 2. Create Chevron
                _chevron = new Microsoft.UI.Xaml.Shapes.Path
                {
                    Width = 12,
                    Height = 12,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 12, 0),
                    IsHitTestVisible = false // Ensure clicks pass through to the button
                };

                var chevronPath = FloweryPathHelpers.GetIconPathData("DaisyIconChevronDown") ?? FloweryPathHelpers.ChevronDownPath;
                FloweryPathHelpers.TrySetPathData(_chevron, () => FloweryPathHelpers.ParseGeometry(chevronPath));

                // 3. Root Container
                _rootGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                _rootGrid.Children.Add(_triggerButton);
                _rootGrid.Children.Add(_chevron);

                // Root grid renders the border; keep background transparent for ShadowContainer content.
                _rootGrid.Background = new SolidColorBrush(Colors.Transparent);
                _rootGrid.SetBinding(Grid.BorderBrushProperty, new Binding { Source = this, Path = new PropertyPath(nameof(BorderBrush)) });
                _rootGrid.SetBinding(Grid.BorderThicknessProperty, new Binding { Source = this, Path = new PropertyPath(nameof(BorderThickness)) });
                _rootGrid.SetBinding(Grid.CornerRadiusProperty, new Binding { Source = this, Path = new PropertyPath(nameof(CornerRadius)) });

                _shadowContainer = new ShadowContainer
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Content = _rootGrid
                };
                _shadowContainer.SetBinding(ShadowContainer.BackgroundProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Background)) });
                _shadowContainer.SetBinding(ShadowContainer.CornerRadiusProperty, new Binding { Source = this, Path = new PropertyPath(nameof(CornerRadius)) });

                // 4. Popup & List
                ItemsControl? itemsControl;
                if (UseListBox)
                {
                    _listBox = new ListBox
                    {
                        SelectionMode = SelectionMode.Single,
                        ItemsSource = ItemsSource ?? Items,
                        Padding = new Thickness(0)
                    };
                    _listBox.SelectionChanged += OnListViewSelectionChanged;
                    _listBox.KeyDown += OnItemsControlKeyDown;
                    _listBox.PointerPressed += OnItemsControlPointerPressed;
                    _listBox.PointerReleased += OnItemsControlPointerReleased;
                    _listBox.PointerCanceled += OnItemsControlPointerCanceled;
                    _listBox.PointerCaptureLost += OnItemsControlPointerCanceled;
                    _itemsControlEventsAttached = true;
                    itemsControl = _listBox;
                }
                else
                {
                    _listView = new ListView
                    {
                        SelectionMode = ListViewSelectionMode.Single,
                        IsItemClickEnabled = true,
                        ItemsSource = ItemsSource ?? Items,
                        Padding = new Thickness(0)
                    };
                    _listView.SelectionChanged += OnListViewSelectionChanged;
                    _listView.ContainerContentChanging += OnContainerContentChanging;
                    _listView.KeyDown += OnItemsControlKeyDown;
                    _listView.ItemClick += OnListViewItemClick;
                    _listView.PointerPressed += OnItemsControlPointerPressed;
                    _listView.PointerReleased += OnItemsControlPointerReleased;
                    _listView.PointerCanceled += OnItemsControlPointerCanceled;
                    _listView.PointerCaptureLost += OnItemsControlPointerCanceled;
                    _itemsControlEventsAttached = true;

                    // Ensure content stretches to fill width so click targets are large
                    _listView.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    itemsControl = _listView;
                }

                var itemsPanelTemplate = TryGetListViewItemsPanelTemplate();
                if (itemsPanelTemplate != null && itemsControl != null)
                {
                    itemsControl.ItemsPanel = itemsPanelTemplate;
                }
                // Bind properties
                itemsControl?.SetBinding(ItemsControl.ItemTemplateProperty, new Binding { Source = this, Path = new PropertyPath(nameof(ItemTemplate)) });
                itemsControl?.SetBinding(ItemsControl.DisplayMemberPathProperty, new Binding { Source = this, Path = new PropertyPath(nameof(DisplayMemberPath)) });

                // Explicitly bind font properties as Popup content does not inherit them
                itemsControl?.SetBinding(Control.FontSizeProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontSize)) });
                itemsControl?.SetBinding(Control.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
                itemsControl?.SetBinding(Control.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
                itemsControl?.SetBinding(Control.FontWeightProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontWeight)) });

                // Prevent infinite expansion crash
                if (itemsControl != null)
                {
                    itemsControl.MaxHeight = 400;
                    itemsControl.VerticalAlignment = VerticalAlignment.Top;
                }

                if (itemsControl != null)
                {
                    DetachFromParentIfNeeded(itemsControl);
                }

                _popupBorder = new Border
                {
                    Child = itemsControl,
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4),
                    VerticalAlignment = VerticalAlignment.Top, // Align to top of Popup
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                _popup = new Popup
                {
                    Child = _popupBorder,
                    IsLightDismissEnabled = true,
                    PlacementTarget = (FrameworkElement?)_triggerButton ?? this,
                    DesiredPlacement = PopupPlacementMode.Bottom
                };
                _popup.Opened += (s, e) =>
                {
                    if (_isUnloading || !IsLoaded)
                    {
                        return;
                    }

                    var itemsControl = ItemsControl;
                    if (itemsControl == null)
                    {
                        return;
                    }

                    itemsControl.UpdateLayout();

                    // Clear stuck pointer-over/pressed states from reused containers when reopening the popup.
                    var items = itemsControl.Items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (itemsControl.ContainerFromIndex(i) is SelectorItem container)
                        {
                            var commonState = container.IsEnabled ? "Normal" : "Disabled";
                            VisualStateManager.GoToState(container, commonState, false);
                            VisualStateManager.GoToState(container, container.IsSelected ? "Selected" : "Unselected", false);
                        }
                    }
                };
                // Guard Closed event too
                _popup.Closed += (s, e) =>
                {
                    if (!_isInteracting) IsDropDownOpen = false;
                };

                this.Content = _shadowContainer;

                ApplyAll();

                // Force a layout pass if we were built after the initial measure.
                InvalidateMeasure();
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region Logic

        private void OnTriggerPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOver = true;
            UpdatePointerOverBorder();
        }

        private void OnTriggerPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOver = false;
            UpdatePointerOverBorder();
        }

        private void OnTriggerKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isInteracting) return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Down:
                    handled = TryMoveSelection(1);
                    break;
                case VirtualKey.Up:
                    handled = TryMoveSelection(-1);
                    break;
                case VirtualKey.Home:
                    handled = TrySetSelectionIndex(0);
                    break;
                case VirtualKey.End:
                    handled = TrySetSelectionIndex(GetItemsCount() - 1);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                case VirtualKey.F4:
                    IsDropDownOpen = !IsDropDownOpen;
                    break;
                case VirtualKey.Escape:
                    if (IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        RestoreTriggerFocusAfterSelection();
                    }
                    else
                    {
                        handled = false;
                    }
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        private void OnItemsControlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isInteracting) return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Escape:
                    if (IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        RestoreTriggerFocusAfterSelection();
                    }
                    else
                    {
                        handled = false;
                    }
                    break;
                case VirtualKey.Enter:
                case VirtualKey.F4:
                    if (IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        RestoreTriggerFocusAfterSelection();
                    }
                    else
                    {
                        handled = false;
                    }
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        private void OnItemsControlPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_isInteracting) return;

            if (sender is UIElement element && e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
            {
                _pendingPointerSelection = true;
            }
        }

        private void OnItemsControlPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pendingPointerSelection = false;
        }

        private void OnItemsControlPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _pendingPointerSelection = false;
        }

        private bool TryMoveSelection(int delta)
        {
            if (_isInteracting) return false;

            var count = GetItemsCount();
            if (count <= 0) return false;

            var index = SelectedIndex;
            if (index < 0 && SelectedItem != null)
            {
                index = GetItemIndex(SelectedItem);
            }

            if (index < 0)
            {
                index = delta > 0 ? -1 : count;
            }

            var nextIndex = index + delta;
            if (nextIndex < 0) nextIndex = 0;
            if (nextIndex >= count) nextIndex = count - 1;

            if (nextIndex == SelectedIndex) return false;

            SelectedIndex = nextIndex;
            return true;
        }

        private bool TrySetSelectionIndex(int index)
        {
            if (_isInteracting) return false;

            var count = GetItemsCount();
            if (count <= 0) return false;

            if (index < 0) index = 0;
            if (index >= count) index = count - 1;

            if (index == SelectedIndex) return false;

            SelectedIndex = index;
            return true;
        }

        private void UpdatePointerOverBorder()
        {
            if (_rootGrid == null) return;

            if (_isPointerOver && _pointerOverBorderBrush != null)
            {
                _rootGrid.SetValue(Grid.BorderBrushProperty, _pointerOverBorderBrush);
            }
            else
            {
                _rootGrid.SetBinding(Grid.BorderBrushProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(BorderBrush))
                });
            }
        }

        private void DetachPopupContentForUnload()
        {
            if (_popupBorder?.Child is UIElement child)
            {
                DetachFromParentIfNeeded(child);
                _popupBorder.Child = null;
            }
        }

        private static void DetachFromParentIfNeeded(UIElement element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            if (parent is Panel panel)
            {
                panel.Children.Remove(element);
                return;
            }

            if (parent is Border border)
            {
                if (ReferenceEquals(border.Child, element))
                {
                    border.Child = null;
                }
                return;
            }

            if (parent is ContentControl contentControl)
            {
                if (ReferenceEquals(contentControl.Content, element))
                {
                    contentControl.Content = null;
                }
                return;
            }

            if (parent is ContentPresenter presenter)
            {
                if (ReferenceEquals(presenter.Content, element))
                {
                    presenter.Content = null;
                }
            }
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.ItemContainer is ListViewItem container)
            {
                container.RemoveHandler(UIElement.TappedEvent, _listViewItemTappedHandler);
                container.AddHandler(UIElement.TappedEvent, _listViewItemTappedHandler, true);
            }

            if (args.InRecycleQueue) return;

            if (args.Item is FrameworkElement fe)
            {
                // Disable hit testing on content to ensure ListViewItem handles clicks
                fe.IsHitTestVisible = false;

                // Do NOT set properties here as it triggers layout loops/crashes on Windows
                // fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                // c.Foreground = fg;
            }
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = SelectorControl;
            if (selector == null || _isInteracting) return;
            if (Equals(selector.SelectedItem, SelectedItem)) return;

            // Guard: Never propagate null selection from ListView if we already have a valid selection.
            // On Desktop/Skia, the ListView can lose selection when the popup closes,
            // which would incorrectly clear the trigger button's content.
            if (selector.SelectedItem == null && SelectedItem != null)
            {
                return;
            }

            SelectedItem = selector.SelectedItem;
            if (IsDropDownOpen && _pendingPointerSelection)
            {
                _pendingPointerSelection = false;
                IsDropDownOpen = false;
                RestoreTriggerFocusAfterSelection();
            }
        }

        private void OnListViewItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_listView == null || _isInteracting) return;

            if (sender is ListViewItem listViewItem)
            {
                var item = listViewItem.DataContext ?? listViewItem.Content;
                CommitSelection(item);
            }
        }

        private void OnListViewItemClick(object sender, ItemClickEventArgs e)
        {
            if (_listView == null || _isInteracting) return;
            CommitSelection(e.ClickedItem);
        }

        private void CommitSelection(object? item)
        {
            var selector = SelectorControl;

            // Guard: Ignore null or empty string selections that come from ghost focus/popup events on Skia
            bool isEmpty = item == null || (item is string s && string.IsNullOrEmpty(s));
            if (isEmpty && SelectedItem != null)
            {
                IsDropDownOpen = false;
                return;
            }

            // Ensure the item is actually in our collection before committing
            if (item != null && GetItemIndex(item) < 0)
            {
                IsDropDownOpen = false;
                return;
            }

            if (selector == null || _isInteracting) return;

            try
            {
                _isInteracting = true;
                // Sync selection from ListView to control
                // Do not check for equality here, force update to sync TriggerButton
                selector.SelectedItem = item;
                SelectedItem = item;
                IsDropDownOpen = false;
            }
            finally
            {
                _isInteracting = false;
            }

            RestoreTriggerFocusAfterSelection();
        }

        private void RestoreTriggerFocusAfterSelection()
        {
            if (_triggerButton == null || _isUnloading || !IsLoaded) return;

            if (DispatcherQueue is { } dispatcherQueue)
            {
                // Defer focus restore to avoid popup-close timing issues.
                dispatcherQueue.TryEnqueue(() => _triggerButton?.Focus(FocusState.Programmatic));
                return;
            }

            _triggerButton.Focus(FocusState.Programmatic);
        }

        private void RefreshSelectionVisualsIfOpen()
        {
            if (_popup == null || !_popup.IsOpen)
            {
                return;
            }

            var itemsControl = ItemsControl;
            if (itemsControl == null)
            {
                return;
            }

            if (DispatcherQueue is { } dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(() => RefreshSelectionVisuals(itemsControl));
                return;
            }

            RefreshSelectionVisuals(itemsControl);
        }

        private static void RefreshSelectionVisuals(ItemsControl itemsControl)
        {
            itemsControl.UpdateLayout();
            var items = itemsControl.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (itemsControl.ContainerFromIndex(i) is SelectorItem container)
                {
                    var commonState = container.IsEnabled ? "Normal" : "Disabled";
                    VisualStateManager.GoToState(container, commonState, false);
                    VisualStateManager.GoToState(container, container.IsSelected ? "Selected" : "Unselected", false);
                }
            }
        }

        private void SyncSelectionFromIndex(int index)
        {
            var count = GetItemsCount();
            if (index < 0 || index >= count)
            {
                SelectedItem = null;
                UpdateTriggerContent();
                return;
            }

            var item = GetItemAt(index);
            if (SelectedItem != item)
            {
                SelectedItem = item;
            }
            UpdateTriggerContent();
        }

        private void SyncSelectionFromItem(object? item)
        {
            if (item == null)
            {
                SelectedIndex = -1;
                UpdateTriggerContent();
                return;
            }

            var index = GetItemIndex(item);
            if (index != SelectedIndex)
            {
                SelectedIndex = index;
            }

            var selector = SelectorControl;
            if (selector != null && !Equals(selector.SelectedItem, item))
            {
                selector.SelectedItem = item;
            }

            UpdateTriggerContent();
        }

        private int GetItemsCount()
        {
            if (ItemsSource is IEnumerable enumerable)
            {
                 int count = 0;
                 if (ItemsSource is IList list) return list.Count;
                 try
                 {
                     foreach (var _ in enumerable) count++;
                     return count;
                 }
                 catch (Exception)
                 {
                }
            }
            return Items?.Count ?? 0;
        }

        private object? GetItemAt(int index)
        {
             if (ItemsSource is IList list)
             {
                 if (index < 0 || index >= list.Count) return null;
                 return list[index];
             }
             if (ItemsSource is IEnumerable enumerable)
             {
                 try
                 {
                     int i = 0;
                     foreach (var item in enumerable)
                     {
                         if (i == index) return item;
                         i++;
                     }
                 }
                 catch (Exception)
                 {
                 }
                 return null;
             }
             if (index >= 0 && index < Items.Count) return Items[index];
             return null;
        }

        private int GetItemIndex(object item)
        {
             if (ItemsSource is IList list) return list.IndexOf(item);
             if (ItemsSource is IEnumerable enumerable)
             {
                 try
                 {
                     int i = 0;
                     foreach (var entry in enumerable)
                     {
                         if (Equals(entry, item)) return i;
                         i++;
                     }
                 }
                 catch (Exception)
                 {
                 }
             }
             return Items.IndexOf(item);
        }

        private void RaiseSelectionChanged(object? oldItem, object? newItem)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(
                new List<object> { oldItem ?? new object() },
                new List<object> { newItem ?? new object() }
            ));
        }

        protected virtual void UpdateTriggerContent()
        {
            if (_triggerButton == null) return;

            try
            {
                var item = SelectedItem;

                // Check if subclass provides direct text (more reliable cross-platform than binding)
                var directText = GetTriggerDisplayText();
                if (directText != null)
                {
                    // Clear previous values to avoid template-mismatch errors
                    _triggerButton.ClearValue(ContentControl.ContentTemplateProperty);
                    _triggerButton.ClearValue(ContentControl.ContentTemplateSelectorProperty);
                    _triggerButton.Content = directText;
                    _triggerButton.DataContext = item;

                    return;
                }

                // Clear previous values to avoid template-mismatch errors when switching to null/string content.
                // This MUST happen before we potentially return if item is null, because the trigger button
                // might have a template set from a previous non-null item.
                _triggerButton.ClearValue(ContentControl.ContentProperty);
                _triggerButton.ClearValue(ContentControl.ContentTemplateProperty);
                _triggerButton.ClearValue(ContentControl.ContentTemplateSelectorProperty);

                if (item == null)
                {
                   _triggerButton.Content = PlaceholderText ?? string.Empty;
                   _triggerButton.DataContext = null;
                   return;
                }

                object? displayItem = item;
                DataTemplate? displayTemplate = null;
                var displayMemberPath = DisplayMemberPath;

                if (displayItem is UIElement uiElement)
                {
                    displayItem = (uiElement as FrameworkElement)?.DataContext ?? uiElement.ToString() ?? string.Empty;
                    displayTemplate = null;
                    displayMemberPath = null;
                }

                displayTemplate ??= ItemTemplate;

                _triggerButton.DataContext = displayItem;

                if (displayTemplate != null)
                {
                    _triggerButton.ContentTemplate = displayTemplate;
                    _triggerButton.Content = displayItem;
                    return;
                }

                if (!string.IsNullOrEmpty(displayMemberPath))
                {
                    // Use ToString() which item classes should override for proper display.
                    // The deferred UpdateTriggerContent after dropdown close handles any timing issues.
                    var displayText = displayItem?.ToString();
                    if (!string.IsNullOrEmpty(displayText))
                    {
                        _triggerButton.Content = displayText;
                        return;
                    }
                    // Fallback to binding
                    _triggerButton.SetBinding(ContentControl.ContentProperty, new Binding
                    {
                        Source = displayItem,
                        Path = new PropertyPath(displayMemberPath)
                    });
                    return;
                }

                // Note: Setting Content property directly automatically clears the binding
                _triggerButton.Content = displayItem ?? string.Empty;
            }
            finally
            {
                UpdateAutomationProperties();
            }
        }

        private void UpdateAutomationProperties()
        {
            if (_triggerButton == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(AccessibleText))
            {
                _triggerButton.ClearValue(AutomationProperties.NameProperty);
                DaisyAccessibility.SetAutomationNameOrClear(_triggerButton, AccessibleText);
                return;
            }

            var item = SelectedItem;
            var displayMemberPath = DisplayMemberPath;
            if (item is UIElement uiElement)
            {
                item = (uiElement as FrameworkElement)?.DataContext ?? uiElement.ToString();
                displayMemberPath = null;
            }

            if (item != null && !string.IsNullOrWhiteSpace(displayMemberPath))
            {
                _triggerButton.ClearValue(AutomationProperties.NameProperty);
                _triggerButton.SetBinding(AutomationProperties.NameProperty, new Binding
                {
                    Source = item,
                    Path = new PropertyPath(displayMemberPath)
                });
                return;
            }

            _triggerButton.ClearValue(AutomationProperties.NameProperty);
            var name = GetAccessibleNameFromSelection(item);
            DaisyAccessibility.SetAutomationNameOrClear(_triggerButton, name);
        }

        private string? GetAccessibleNameFromSelection(object? item)
        {
            var directText = GetTriggerDisplayText();
            if (!string.IsNullOrWhiteSpace(directText))
            {
                return directText;
            }

            if (item == null)
            {
                return PlaceholderText;
            }

            var contentText = DaisyAccessibility.GetAccessibleNameFromContent(item);
            if (!string.IsNullOrWhiteSpace(contentText))
            {
                return contentText;
            }

            var itemText = item.ToString();
            return string.IsNullOrWhiteSpace(itemText) ? PlaceholderText : itemText;
        }


        private void UpdatePopupState()
        {
            if (_popup == null || _rootGrid == null) return;
            if (_isUnloading || !IsLoaded)
            {
                SetPopupIsOpen(false);
                return;
            }

            if (IsDropDownOpen)
            {
                // Ensure XamlRoot is set before opening
                try
                {
                    if (_popup.XamlRoot == null && this.XamlRoot != null)
                    {
                        _popup.XamlRoot = this.XamlRoot;
                    }
                }
                catch { /* Ignore if XamlRoot access fails (unlikely but safe) */ }

                var popupWidth = this.ActualWidth;
                if (popupWidth <= 0) popupWidth = _rootGrid.ActualWidth;

                if (popupWidth > 0)
                {
                    _popup.Width = popupWidth;
                    if (_popupBorder != null)
                    {
                        _popupBorder.MinWidth = popupWidth;
                        _popupBorder.Width = popupWidth;
                    }
                }

                SetPopupIsOpen(true);

                var selector = SelectorControl;
                if (selector != null && SelectedItem != null && !Equals(selector.SelectedItem, SelectedItem))
                {
                    var wasInteracting = _isInteracting;
                    _isInteracting = true;
                    try
                    {
                        selector.SelectedItem = SelectedItem;
                    }
                    finally
                    {
                        _isInteracting = wasInteracting;
                    }
                    // _listView.ScrollIntoView(SelectedItem); // Potential crash/hang source on Windows if item not realized
                }
            }
            else
            {
                SetPopupIsOpen(false);
            }
        }

        private void SetPopupIsOpen(bool isOpen)
        {
            if (_popup == null) return;

            if (DispatcherQueue is { } dispatcherQueue)
            {
                // Defer to avoid "Child collection must not be modified during measure or arrange."
                dispatcherQueue.TryEnqueue(() =>
                {
                    if (_popup == null) return;
                    if (_isUnloading || !IsLoaded)
                    {
                        _popup.IsOpen = false;
                        return;
                    }
                    if (_popup.IsOpen == isOpen) return;
                    _popup.IsOpen = isOpen;
                });
                return;
            }

            if (_popup.IsOpen == isOpen) return;
            _popup.IsOpen = isOpen;
        }

        #endregion

        #region Theming

        protected virtual void OnThemeUpdated()
        {
            RequestApplyAll();
        }

        private void OnGlobalNeumorphicChanged(object? sender, EventArgs e)
        {
            RequestNeumorphicRefresh();
        }

        protected void RequestApplyAll()
        {
            if (_isUnloading)
            {
                return;
            }

            if (_applyAllQueued)
            {
                return;
            }

            _applyAllQueued = true;
            if (DispatcherQueue is { } dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    _applyAllQueued = false;
                    ApplyAll();
                });
                return;
            }

            _applyAllQueued = false;
            ApplyAll();
        }

        protected void ApplyAll()
        {
            if (_isUnloading)
            {
                return;
            }

            if (_rootGrid == null)
            {
                // Build early if requested before Loaded/Ctor completed fully
                BuildVisualTree();
                return; // BuildVisualTree calls ApplyAll again
            }

            if (DispatcherQueue is { HasThreadAccess: false } dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(() => ApplyAll());
                return;
            }

            var resources = Application.Current?.Resources;
            if (resources != null) DaisyTokenDefaults.EnsureDefaults(resources);

            ApplyTheme(resources);
            ApplySizing(resources);
            ApplyDropdownTheme(resources);
            OnAfterApplyAll(resources);
            RequestNeumorphicRefresh();
        }

        protected virtual void OnAfterApplyAll(ResourceDictionary? resources) { }

        protected virtual void ApplyTheme(ResourceDictionary? resources)
        {
            // Fallback strategy:
            // If resource lookup fails completely, we use these defaults.
            // Critical: Use Dark background for fallback because if Foreground lookup SUCCEEDS (e.g. White for Dark theme)
            // but Background lookup FAILS (defaults to White), we get White-on-White.
            // Dark fallback for background ensures text remains visible in that mismatch scenario.

            var fallbackBg = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)); // Dark Gray/Almost Black
            var fallbackFg = new SolidColorBrush(Colors.LightGray);

            // Base impl - subclasses override
            var base100 = GetBrush(resources, "DaisyBase100Brush", fallbackBg);

            // Double-check: If we got the fallback, try Application.Current directly just in case the passed dictionary was limited
            if (ReferenceEquals(base100, fallbackBg)
                && Application.Current?.Resources?.TryGetValue("DaisyBase100Brush", out var sysBrush) == true
                && sysBrush is Brush b)
            {
                base100 = b;
            }

            var baseContent = GetBrush(resources, "DaisyBaseContentBrush", fallbackFg);
            var base200 = GetBrush(resources, "DaisyBase200Brush", base100);
            var base300 = GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)));
            _pointerOverBorderBrush = DaisyResourceLookup.TryGetControlBrush(this, GetType().Name, "BorderBrushPointerOver")
                ?? DaisyResourceLookup.TryGetControlBrush(this, "DaisySelect", "BorderBrushPointerOver")
                ?? baseContent;

            // Set Control Foreground (propagates via bindings to items)
            this.Foreground = baseContent;

             if (_triggerButton != null) _triggerButton.Foreground = baseContent;
             if (_chevron != null) _chevron.Fill = baseContent;

             if (_popupBorder != null)
             {
                 _popupBorder.Background = base100;
                 _popupBorder.BorderBrush = base300;
             }
             if (_listView != null)
             {
                 _listView.Background = new SolidColorBrush(Colors.Transparent);
                 _listView.Foreground = baseContent;
                 UpdateItemContainerStyle(baseContent, base100, base200);
             }
             if (_listBox != null)
             {
                 _listBox.Background = new SolidColorBrush(Colors.Transparent);
                 _listBox.Foreground = baseContent;
                 UpdateItemContainerStyle(baseContent, base100, base200);
             }

             UpdatePointerOverBorder();

             // One-shot layout pass for Android
             if (IsLoaded)
             {
                 InvalidateMeasure();
             }
        }

        protected virtual void ApplyDropdownTheme(ResourceDictionary? resources) { }

        protected virtual void ApplySizing(ResourceDictionary? resources)
        {
             // Apply Height, FontSize based on daisy tokens
             double height = DaisyResourceLookup.GetDefaultHeight(Size);
             this.Height = height;
             this.FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
             this.CornerRadius = new CornerRadius(4);

             if (_rootGrid != null) _rootGrid.Height = height;
             UpdateMenuItemSizing(resources);
             UpdateChevronSizing();
             UpdateItemContainerStyle();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var baseSize = base.MeasureOverride(availableSize);

            if (HorizontalAlignment == HorizontalAlignment.Stretch && !double.IsInfinity(availableSize.Width))
            {
                double width = availableSize.Width;
                if (!double.IsInfinity(MaxWidth) && width > MaxWidth) width = MaxWidth;
                return new Size((float)Math.Max(width, baseSize.Width), (float)baseSize.Height);
            }

            return baseSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            _rootGrid?.Arrange(new Rect(0, 0, (float)finalSize.Width, (float)finalSize.Height));
            return size;
        }

        protected static Brush GetBrush(ResourceDictionary? resources, string key, Brush fallback)
        {
            return DaisyResourceLookup.GetBrush(resources, key, fallback);
        }

        protected static Thickness GetThickness(ResourceDictionary? resources, string key, Thickness fallback)
        {
            return DaisyResourceLookup.GetThickness(resources, key, fallback);
        }

        // Helper specifically required by subclasses currently
        protected static double GetSizeDouble(ResourceDictionary? resources, string prefix, DaisySize size, string suffix, double fallback)
        {
            return DaisyResourceLookup.GetSizeDouble(resources, prefix, size, suffix, fallback);
        }

        #endregion

        #region Neumorphic

        public void RefreshNeumorphicEffect()
        {
            if (DaisyBaseContentControl.DisableNeumorphicAutoRefresh)
            {
                return;
            }

            if (_isUnloading || !IsLoaded)
            {
                return;
            }

            if (!IsNeumorphicEffectivelyEnabled())
            {
                ClearNeumorphicShadows();
                return;
            }

            if (_shadowContainer == null)
            {
                BuildVisualTree();
            }

            if (_shadowContainer == null)
            {
                return;
            }

            EnsureShadowResources();

            var mode = GetEffectiveNeumorphicMode();
            var intensity = GetEffectiveNeumorphicIntensity();
            var elevation = DaisyResourceLookup.GetDefaultElevation(Size);
            var (offset, blur) = DaisyNeumorphicHelper.GetShadowMetrics(this, elevation, mode);

            var darkShadowColor = DaisyNeumorphic.GetDarkShadowColor(this)
                ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor;
            var lightShadowColor = DaisyNeumorphic.GetLightShadowColor(this)
                ?? DaisyBaseContentControl.GlobalNeumorphicLightShadowColor;

            if (_shadowCollection == null || _darkShadow == null || _lightShadow == null)
            {
                return;
            }

            if (mode == DaisyNeumorphicMode.Inset)
            {
                _darkShadow.IsInner = true;
                _lightShadow.IsInner = true;
                _darkShadow.OffsetX = -offset;
                _darkShadow.OffsetY = -offset;
                _lightShadow.OffsetX = offset;
                _lightShadow.OffsetY = offset;
            }
            else
            {
                _darkShadow.IsInner = false;
                _lightShadow.IsInner = false;
                _darkShadow.OffsetX = offset;
                _darkShadow.OffsetY = offset;
                _lightShadow.OffsetX = -offset;
                _lightShadow.OffsetY = -offset;
            }

            _darkShadow.Color = DaisyNeumorphicHelper.ApplyIntensity(darkShadowColor, intensity);
            _lightShadow.Color = DaisyNeumorphicHelper.ApplyIntensity(lightShadowColor, intensity);
            _darkShadow.BlurRadius = blur;
            _lightShadow.BlurRadius = blur;
            _darkShadow.Spread = 0;
            _lightShadow.Spread = 0;
            _darkShadow.Opacity = 1;
            _lightShadow.Opacity = 1;

            _shadowCollection.Clear();
            _shadowCollection.Add(_darkShadow);
            if (mode == DaisyNeumorphicMode.Raised || mode == DaisyNeumorphicMode.Inset)
            {
                _shadowCollection.Add(_lightShadow);
            }

            _shadowContainer.Shadows = _shadowCollection;
        }

        internal void RequestNeumorphicRefresh()
        {
            if (DaisyBaseContentControl.DisableNeumorphicAutoRefresh)
            {
                return;
            }

            if (_isUnloading || !IsLoaded)
                return;
            DaisyNeumorphicRefreshHelper.QueueRefresh(this);
        }

        private void EnsureShadowResources()
        {
            _shadowCollection ??= new ToolkitShadowCollection();
            _darkShadow ??= new ToolkitShadow();
            _lightShadow ??= new ToolkitShadow();
        }

        private void ClearNeumorphicShadows()
        {
            if (_shadowContainer == null)
            {
                return;
            }

            _shadowContainer.Shadows = new ToolkitShadowCollection();
        }

        private bool IsNeumorphicEffectivelyEnabled()
        {
            var scope = DaisyNeumorphic.GetScopeEnabled(this);
            bool isActuallyEnabled = scope ?? NeumorphicEnabled ?? DaisyBaseContentControl.GlobalNeumorphicEnabled;

            return isActuallyEnabled && GetEffectiveNeumorphicMode() != DaisyNeumorphicMode.None;
        }

        private DaisyNeumorphicMode GetEffectiveNeumorphicMode()
        {
            return NeumorphicMode ?? DaisyBaseContentControl.GlobalNeumorphicMode;
        }

        private double GetEffectiveNeumorphicIntensity()
        {
            return Math.Clamp(NeumorphicIntensity ?? DaisyBaseContentControl.GlobalNeumorphicIntensity, 0.0, 1.0);
        }

        #endregion

        private void UpdateItemContainerStyle()
        {
            if (_listView == null && _listBox == null) return;

            var resources = Application.Current?.Resources;
            var baseContent = Foreground as Brush
                ?? GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.Black));

            var baseBackground = GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)));
            var selectedBackground = GetBrush(resources, "DaisyBase200Brush", baseBackground);

            UpdateItemContainerStyle(baseContent, baseBackground, selectedBackground);
        }

        private void UpdateItemContainerStyle(Brush baseContent, Brush baseBackground, Brush selectedBackground)
        {
            if (_listView == null && _listBox == null) return;

            var itemContainerType = _listBox != null ? typeof(ListBoxItem) : typeof(ListViewItem);
            var style = CreateMergedItemContainerStyle(baseContent, baseBackground, selectedBackground, ItemContainerStyle, itemContainerType);
            if (_listView != null)
            {
                _listView.ItemContainerStyle = style;
            }
            if (_listBox != null)
            {
                _listBox.ItemContainerStyle = style;
            }
        }

        private Style CreateMergedItemContainerStyle(Brush baseContent, Brush baseBackground, Brush selectedBackground, Style? userStyle, Type itemContainerType)
        {
            if (userStyle?.TargetType is { } targetType && !targetType.IsAssignableFrom(itemContainerType))
            {
                userStyle = null;
            }

            var style = new Style(itemContainerType) { BasedOn = userStyle };
            AddSetterIfMissing(style, userStyle, Control.ForegroundProperty, baseContent);
            AddSetterIfMissing(style, userStyle, FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            AddSetterIfMissing(style, userStyle, Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
            AddSetterIfMissing(style, userStyle, Control.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            AddSetterIfMissing(style, userStyle, Control.PaddingProperty, _menuItemPadding);
            AddSetterIfMissing(style, userStyle, Control.FontSizeProperty, _menuItemFontSize);
            AddSetterIfMissing(style, userStyle, FrameworkElement.MinHeightProperty, 0d);
            AddSetterIfMissing(style, userStyle, FrameworkElement.MarginProperty, new Thickness(0));
            AddSetterIfMissing(style, userStyle, Control.BorderThicknessProperty, new Thickness(0));
            AddSetterIfMissing(style, userStyle, Control.BorderBrushProperty, selectedBackground);
            AddSetterIfMissing(style, userStyle, Control.BackgroundProperty, baseBackground);
            if (UseCustomItemContainerTemplate)
            {
                var template = itemContainerType == typeof(ListBoxItem)
                    ? TryGetListBoxItemTemplate()
                    : TryGetListViewItemTemplate();
                if (template != null)
                {
                    AddSetterIfMissing(style, userStyle, Control.TemplateProperty, template);
                }
            }
            return style;
        }

        private static void AddSetterIfMissing(Style targetStyle, Style? baseStyle, DependencyProperty property, object value)
        {
            if (StyleDefinesSetter(baseStyle, property)) return;
            targetStyle.Setters.Add(new Setter(property, value));
        }

        private static bool StyleDefinesSetter(Style? style, DependencyProperty property)
        {
            while (style != null)
            {
                foreach (var setterBase in style.Setters)
                {
                    if (setterBase is Setter setter && setter.Property == property)
                        return true;
                }
                style = style.BasedOn;
            }

            return false;
        }

        private ControlTemplate? TryGetListViewItemTemplate()
        {
            return TryGetItemContainerTemplate("DaisyComboBoxListViewItemTemplate");
        }

        private ControlTemplate? TryGetListBoxItemTemplate()
        {
            return TryGetItemContainerTemplate("DaisyComboBoxListBoxItemTemplate");
        }

        private ControlTemplate? TryGetItemContainerTemplate(string key)
        {
            if (Resources != null && Resources.TryGetValue(key, out var local) && local is ControlTemplate localTemplate)
            {
                return localTemplate;
            }

            if (Application.Current?.Resources is { } appResources
                && DaisyResourceLookup.TryGetResource(appResources, key, out var appValue)
                && appValue is ControlTemplate appTemplate)
            {
                return appTemplate;
            }

            var daisyResources = GetDaisyControlsResources();
            if (daisyResources != null
                && DaisyResourceLookup.TryGetResource(daisyResources, key, out var daisyValue)
                && daisyValue is ControlTemplate daisyTemplate)
            {
                return daisyTemplate;
            }

            return null;
        }

        private ItemsPanelTemplate? TryGetListViewItemsPanelTemplate()
        {
            const string key = "DaisyComboBoxItemsPanelTemplate";

            if (Resources != null && Resources.TryGetValue(key, out var local) && local is ItemsPanelTemplate localTemplate)
            {
                return localTemplate;
            }

            if (Application.Current?.Resources is { } appResources
                && DaisyResourceLookup.TryGetResource(appResources, key, out var appValue)
                && appValue is ItemsPanelTemplate appTemplate)
            {
                return appTemplate;
            }

            var daisyResources = GetDaisyControlsResources();
            if (daisyResources != null
                && DaisyResourceLookup.TryGetResource(daisyResources, key, out var daisyValue)
                && daisyValue is ItemsPanelTemplate daisyTemplate)
            {
                return daisyTemplate;
            }

            return null;
        }

        protected static ResourceDictionary? GetDaisyControlsResources()
        {
            if (s_daisyControlsLoaded)
            {
                return s_daisyControlsResources;
            }

            s_daisyControlsLoaded = true;
            try
            {
                s_daisyControlsResources = new ResourceDictionary
                {
                    Source = new Uri("ms-appx:///Flowery.Uno/Themes/DaisyControls.xaml")
                };
            }
            catch
            {
                s_daisyControlsResources = null;
            }

            return s_daisyControlsResources;
        }

        private void UpdateMenuItemSizing(ResourceDictionary? resources)
        {
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            var defaultPadding = DaisyResourceLookup.GetDefaultPadding(Size);
            var defaultFontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
            var defaultHeight = DaisyResourceLookup.GetDefaultHeight(Size);

            _menuItemPadding = resources != null
                ? DaisyResourceLookup.GetThickness(resources, $"DaisyButton{sizeKey}Padding", defaultPadding)
                : defaultPadding;

            _menuItemFontSize = resources != null
                ? DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}FontSize", defaultFontSize)
                : defaultFontSize;

            _menuItemMinHeight = resources != null
                ? DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}Height", defaultHeight)
                : defaultHeight;
        }

        private void UpdateChevronSizing()
        {
            if (_chevron == null) return;

            var chevronSize = Math.Max(8, _menuItemFontSize - 2);
            _chevron.Width = chevronSize;
            _chevron.Height = chevronSize;
        }

        // Stubs for methods referenced by subclasses
        protected virtual void PrepareContainerForItemOverride(DependencyObject element, object item) { }
    }
}
