using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Flowery.Localization;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Flowery.Controls
{
    /// <summary>
    /// A named view supplied by the application.
    /// </summary>
    public sealed class DaisyTabView
    {
        /// <summary>
        /// Gets the view identifier. The active view is matched through object equality.
        /// </summary>
        public required object Id { get; init; }

        /// <summary>
        /// Gets the name shown in the view menu.
        /// </summary>
        public required string Name { get; init; }
    }

    /// <summary>
    /// Identifies the view requested by the user.
    /// </summary>
    public sealed class DaisyTabViewEventArgs(object id, string name) : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the requested view.
        /// </summary>
        public object Id { get; } = id;

        /// <summary>
        /// Gets the name of the requested view.
        /// </summary>
        public string Name { get; } = name;
    }

    /// <summary>
    /// Provides optional navigation buttons and a view menu for a tab header or toolbar.
    /// The application owns navigation, view selection, and view management.
    /// </summary>
    public partial class DaisyTabsHeaderButtons : DaisyBaseContentControl, IScalableControl
    {
        private readonly MenuFlyout _viewMenu = new() { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
        private readonly DaisyButton _previousButton;
        private readonly DaisyButton _nextButton;
        private readonly DaisyButton _viewMenuButton;
        private EventHandler<CancelEventArgs>? _viewMenuOpening;
        private INotifyCollectionChanged? _observedViews;
        private double _scaleFactor = 1.0;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ShowNavigationButtons"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowNavigationButtonsProperty =
            DependencyProperty.Register(nameof(ShowNavigationButtons), typeof(bool), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(false, OnButtonVisibilityChanged));

        /// <summary>
        /// Gets or sets whether the previous and next buttons are visible.
        /// </summary>
        public bool ShowNavigationButtons
        {
            get => (bool)GetValue(ShowNavigationButtonsProperty);
            set => SetValue(ShowNavigationButtonsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowViewMenuButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowViewMenuButtonProperty =
            DependencyProperty.Register(nameof(ShowViewMenuButton), typeof(bool), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(false, OnButtonVisibilityChanged));

        /// <summary>
        /// Gets or sets whether the view menu button is visible.
        /// </summary>
        public bool ShowViewMenuButton
        {
            get => (bool)GetValue(ShowViewMenuButtonProperty);
            set => SetValue(ShowViewMenuButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Views"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewsProperty =
            DependencyProperty.Register(nameof(Views), typeof(IList<DaisyTabView>), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(null, OnViewsChanged));

        /// <summary>
        /// Gets or sets the views read each time the menu opens.
        /// Observable collections also update the menu button's enabled state immediately.
        /// </summary>
        public IList<DaisyTabView>? Views
        {
            get => (IList<DaisyTabView>?)GetValue(ViewsProperty);
            set => SetValue(ViewsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ActiveViewId"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActiveViewIdProperty =
            DependencyProperty.Register(nameof(ActiveViewId), typeof(object), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the identifier of the checked view.
        /// </summary>
        public object? ActiveViewId
        {
            get => GetValue(ActiveViewIdProperty);
            set => SetValue(ActiveViewIdProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowManageViewsEntry"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowManageViewsEntryProperty =
            DependencyProperty.Register(nameof(ShowManageViewsEntry), typeof(bool), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(true, OnMenuAvailabilityChanged));

        /// <summary>
        /// Gets or sets whether the menu includes the view management request.
        /// </summary>
        public bool ShowManageViewsEntry
        {
            get => (bool)GetValue(ShowManageViewsEntryProperty);
            set => SetValue(ShowManageViewsEntryProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(DaisySize), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(DaisySize.Auto, OnSizePropertyChanged));

        /// <summary>
        /// Gets or sets the size of the buttons and icons.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviousCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviousCommandProperty =
            DependencyProperty.Register(nameof(PreviousCommand), typeof(ICommand), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(null, OnNavigationCommandChanged));

        /// <summary>
        /// Gets or sets the previous request command. Its parameter is null.
        /// </summary>
        public ICommand? PreviousCommand
        {
            get => (ICommand?)GetValue(PreviousCommandProperty);
            set => SetValue(PreviousCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NextCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NextCommandProperty =
            DependencyProperty.Register(nameof(NextCommand), typeof(ICommand), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(null, OnNavigationCommandChanged));

        /// <summary>
        /// Gets or sets the next request command. Its parameter is null.
        /// </summary>
        public ICommand? NextCommand
        {
            get => (ICommand?)GetValue(NextCommandProperty);
            set => SetValue(NextCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ViewSelectedCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewSelectedCommandProperty =
            DependencyProperty.Register(nameof(ViewSelectedCommand), typeof(ICommand), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the view selection command. Its parameter is the view identifier.
        /// </summary>
        public ICommand? ViewSelectedCommand
        {
            get => (ICommand?)GetValue(ViewSelectedCommandProperty);
            set => SetValue(ViewSelectedCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ManageViewsCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ManageViewsCommandProperty =
            DependencyProperty.Register(nameof(ManageViewsCommand), typeof(ICommand), typeof(DaisyTabsHeaderButtons),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the view management command. Its parameter is null.
        /// </summary>
        public ICommand? ManageViewsCommand
        {
            get => (ICommand?)GetValue(ManageViewsCommandProperty);
            set => SetValue(ManageViewsCommandProperty, value);
        }

        #endregion

        /// <summary>
        /// Raised when the previous button is clicked.
        /// </summary>
        public event EventHandler? PreviousRequested;

        /// <summary>
        /// Raised when the next button is clicked.
        /// </summary>
        public event EventHandler? NextRequested;

        /// <summary>
        /// Raised when a view is chosen in the view menu. The control does not change <see cref="ActiveViewId"/>.
        /// </summary>
        public event EventHandler<DaisyTabViewEventArgs>? ViewSelected;

        /// <summary>
        /// Raised when the view management entry is chosen.
        /// </summary>
        public event EventHandler? ManageViewsRequested;

        /// <summary>
        /// Raised before the menu entries are built. The handler can load Views, update permissions,
        /// or cancel opening. A subscribed handler keeps an initially empty menu button available.
        /// </summary>
        public event EventHandler<CancelEventArgs>? ViewMenuOpening
        {
            add
            {
                _viewMenuOpening += value;
                UpdateViewMenuAvailability();
            }
            remove
            {
                _viewMenuOpening -= value;
                UpdateViewMenuAvailability();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DaisyTabsHeaderButtons"/> class.
        /// </summary>
        public DaisyTabsHeaderButtons()
        {
            IsTabStop = false;
            VerticalAlignment = VerticalAlignment.Center;

            _previousButton = CreateButton("PART_PreviousButton", Symbol.Back);
            _nextButton = CreateButton("PART_NextButton", Symbol.Forward);
            _viewMenuButton = CreateButton("PART_ViewMenuButton", Symbol.Setting);

            _previousButton.Click += OnPreviousClick;
            _nextButton.Click += OnNextClick;
            _viewMenuButton.Click += OnViewMenuButtonClick;

            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _previousButton, _nextButton, _viewMenuButton }
            };

            UpdateButtonVisibility();
            UpdateButtonSizes();
            UpdateButtonLabels();
            UpdateViewMenuAvailability();
        }

        /// <summary>
        /// Gets the view menu. The button opens it after <see cref="ViewMenuOpening"/>.
        /// </summary>
        internal MenuFlyout ViewMenu => _viewMenu;

        /// <inheritdoc/>
        public void ApplyScaleFactor(double scaleFactor)
        {
            _scaleFactor = scaleFactor;
            UpdateButtonSizes();
        }

        #region Lifecycle

        /// <inheritdoc/>
        protected override void OnLoaded()
        {
            base.OnLoaded();
            FloweryLocalization.CultureChanged += OnCultureChanged;
            LayoutUpdated += OnLayoutUpdated;
            ObserveViews();
            UpdateButtonLabels();
            UpdateViewMenuAvailability();
        }

        /// <inheritdoc/>
        protected override void OnUnloaded()
        {
            FloweryLocalization.CultureChanged -= OnCultureChanged;
            LayoutUpdated -= OnLayoutUpdated;
            StopObservingViews();
            _viewMenu.Hide();
            base.OnUnloaded();
        }

        /// <inheritdoc/>
        protected override void OnResolvedSizeChanged()
        {
            base.OnResolvedSizeChanged();
            UpdateButtonSizes();
        }

        /// <inheritdoc/>
        protected override DaisySize? GetControlSize() => Size;

        /// <inheritdoc/>
        protected override DaisySize DefaultSize => DaisySize.Small;

        #endregion

        #region Property Callbacks

        private static void OnButtonVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabsHeaderButtons buttons)
            {
                buttons.UpdateButtonVisibility();
                buttons.UpdateViewMenuAvailability();
            }
        }

        private static void OnViewsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabsHeaderButtons buttons)
            {
                if (buttons.IsLoaded)
                {
                    buttons.ObserveViews();
                }

                buttons.UpdateViewMenuAvailability();
            }
        }

        private static void OnMenuAvailabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabsHeaderButtons buttons)
            {
                buttons.UpdateViewMenuAvailability();
            }
        }

        private static void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabsHeaderButtons buttons)
            {
                buttons.UpdateButtonSizes();
            }
        }

        private static void OnNavigationCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabsHeaderButtons buttons)
            {
                buttons._previousButton.Command = buttons.PreviousCommand;
                buttons._nextButton.Command = buttons.NextCommand;
            }
        }

        #endregion

        private static DaisyButton CreateButton(string name, Symbol symbol) => new()
        {
            Name = name,
            Variant = DaisyButtonVariant.Default,
            Shape = DaisyButtonShape.Square,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new DaisyIconText { IconSymbol = symbol }
        };

        private void UpdateButtonVisibility()
        {
            var navigationVisibility = ShowNavigationButtons ? Visibility.Visible : Visibility.Collapsed;
            _previousButton.Visibility = navigationVisibility;
            _nextButton.Visibility = navigationVisibility;
            _viewMenuButton.Visibility = ShowViewMenuButton ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateButtonSizes()
        {
            var size = ResolvedSize;
            var iconSize = FloweryScaleManager.ApplyScale(DaisyResourceLookup.GetDefaultIconSize(size), _scaleFactor);

            foreach (var button in new[] { _previousButton, _nextButton, _viewMenuButton })
            {
                button.Size = size;
                if (button.Content is DaisyIconText icon)
                {
                    icon.Size = size;
                    icon.IconSize = iconSize;
                }
            }
        }

        private void ObserveViews()
        {
            StopObservingViews();
            if (Views is INotifyCollectionChanged observable)
            {
                _observedViews = observable;
                observable.CollectionChanged += OnViewsCollectionChanged;
            }
        }

        private void StopObservingViews()
        {
            if (_observedViews is not null)
            {
                _observedViews.CollectionChanged -= OnViewsCollectionChanged;
            }

            _observedViews = null;
        }

        private void OnViewsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateViewMenuAvailability();

        // IList does not require change notifications. Recheck its count after layout as well.
        private void OnLayoutUpdated(object? sender, object e) => UpdateViewMenuAvailability();

        private void UpdateViewMenuAvailability()
        {
            var hasEntries = Views is { Count: > 0 } || ShowManageViewsEntry || _viewMenuOpening is not null;
            if (_viewMenuButton.IsEnabled != hasEntries)
            {
                _viewMenuButton.IsEnabled = hasEntries;
            }

            if (!hasEntries || !ShowViewMenuButton)
            {
                _viewMenu.Hide();
            }
        }

        /// <summary>
        /// Raises <see cref="ViewMenuOpening"/> and rebuilds the menu entries.
        /// </summary>
        /// <returns>True when the menu has entries and may open.</returns>
        internal bool PrepareViewMenu()
        {
            var args = new CancelEventArgs();
            _viewMenuOpening?.Invoke(this, args);
            if (args.Cancel)
            {
                return false;
            }

            _viewMenu.Items.Clear();
            if (Views is { } views)
            {
                foreach (var view in views)
                {
                    var item = new ToggleMenuFlyoutItem
                    {
                        Text = view.Name,
                        Tag = view,
                        IsChecked = Equals(view.Id, ActiveViewId),
                        Command = ViewSelectedCommand,
                        CommandParameter = view.Id
                    };
                    item.Click += OnViewClick;
                    _viewMenu.Items.Add(item);
                }
            }

            if (ShowManageViewsEntry)
            {
                if (_viewMenu.Items.Count > 0)
                {
                    _viewMenu.Items.Add(new MenuFlyoutSeparator());
                }

                var manageItem = new MenuFlyoutItem
                {
                    Text = FloweryLocalization.GetStringInternal("Tabs_ManageViews", "Manage views…"),
                    Command = ManageViewsCommand
                };
                manageItem.Click += OnManageViewsClick;
                _viewMenu.Items.Add(manageItem);
            }

            return _viewMenu.Items.Count > 0 && ShowViewMenuButton && IsEnabled;
        }

        private void OnPreviousClick(object sender, RoutedEventArgs e)
        {
            if (_previousButton.IsEnabled)
            {
                PreviousRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            if (_nextButton.IsEnabled)
            {
                NextRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnViewMenuButtonClick(object sender, RoutedEventArgs e)
        {
            // WinUI's FlyoutBase.Opening cannot be cancelled, so the button opens the menu itself.
            if (PrepareViewMenu())
            {
                _viewMenu.ShowAt(_viewMenuButton);
            }
        }

        internal void OnViewClick(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem { IsEnabled: true, Tag: DaisyTabView view })
            {
                ViewSelected?.Invoke(this, new DaisyTabViewEventArgs(view.Id, view.Name));
            }
        }

        internal void OnManageViewsClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem { IsEnabled: true })
            {
                ManageViewsRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            if (DispatcherQueue is { HasThreadAccess: false } queue)
            {
                queue.TryEnqueue(UpdateButtonLabels);
                return;
            }

            UpdateButtonLabels();
        }

        private void UpdateButtonLabels()
        {
            SetButtonLabel(_previousButton, FloweryLocalization.GetStringInternal("Accessibility_PreviousItem", "Previous item"));
            SetButtonLabel(_nextButton, FloweryLocalization.GetStringInternal("Accessibility_NextItem", "Next item"));
            SetButtonLabel(_viewMenuButton, FloweryLocalization.GetStringInternal("Tabs_Views", "Views"));
        }

        private static void SetButtonLabel(Button button, string label)
        {
            AutomationProperties.SetName(button, label);
            ToolTipService.SetToolTip(button, label);
        }
    }
}
