using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Helpers;
using Flowery.Localization;
using Flowery.Uno.Kanban.Controls.Users;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// User management surface for FlowKanban.
    /// </summary>
    public partial class FlowKanbanUserManagement : DaisyBaseContentControl
    {
        private IUserProvider? _trackedProvider;
        private FrameworkElement? _rootElement;
        private Border? _userListPanel;
        private Border? _userDetailsPanel;
        private Border? _userDetailsCard;
        private Border? _userDetailsEmptyCard;
        private CancellationTokenSource? _refreshCts;
        private bool _isLocalizationSubscribed;
        private const string GitHubProviderKey = "github";
        private const string LocalProviderKey = "local";
        private readonly UserIdentityLinkStore _identityLinkStore = new(StateStorageProvider.Instance);

        public FlowKanbanUserManagement()
        {
            DefaultStyleKey = typeof(FlowKanbanUserManagement);
            RefreshCommand = new RelayCommand(ExecuteRefreshUsers);
            AddUserCommand = new RelayCommand(ExecuteAddUser, CanExecuteAddUser);
            RemoveUserCommand = new RelayCommand(ExecuteRemoveUser, CanExecuteRemoveUser);
            ConnectGitHubCommand = new RelayCommand<string>(ExecuteConnectGitHub, CanExecuteConnectGitHub);
            DisconnectProviderCommand = new RelayCommand<string>(ExecuteDisconnectProvider, CanExecuteDisconnectProvider);
            LinkLocalUserCommand = new RelayCommand(ExecuteLinkLocalUser, CanExecuteLinkLocalUser);
            UnlinkLocalUserCommand = new RelayCommand(ExecuteUnlinkLocalUser, CanExecuteUnlinkLocalUser);
            _ = EnsureUsers();
            _ = EnsureFilteredUsers();
            _ = EnsureProviders();
            Loaded += OnViewLoaded;
            Unloaded += OnViewUnloaded;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rootElement = GetTemplateChild("PART_UsersRoot") as FrameworkElement;
            _userListPanel = GetTemplateChild("PART_UserListPanel") as Border;
            _userDetailsPanel = GetTemplateChild("PART_UserDetailsPanel") as Border;
            _userDetailsCard = GetTemplateChild("PART_UserDetailsCard") as Border;
            _userDetailsEmptyCard = GetTemplateChild("PART_UserDetailsEmptyCard") as Border;
            BindRootDataContext();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RefreshTheme();
        }

        #region Localization
        public static readonly DependencyProperty LocalizationProperty =
            DependencyProperty.Register(
                nameof(Localization),
                typeof(FloweryLocalization),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(FloweryLocalization.Instance));

        public FloweryLocalization Localization
        {
            get => (FloweryLocalization)GetValue(LocalizationProperty);
            set => SetValue(LocalizationProperty, value);
        }
        #endregion

        #region UserProvider
        public static readonly DependencyProperty UserProviderProperty =
            DependencyProperty.Register(
                nameof(UserProvider),
                typeof(IUserProvider),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(null, OnUserProviderChanged));

        public IUserProvider? UserProvider
        {
            get => (IUserProvider?)GetValue(UserProviderProperty);
            set => SetValue(UserProviderProperty, value);
        }

        private static void OnUserProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanUserManagement view)
            {
                view.AttachProvider(e.NewValue as IUserProvider);
                view.RefreshProviderSummaries();
                _ = view.RefreshUsersAsync(forceReload: true);
            }
        }
        #endregion

        #region IsActive
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false, OnIsActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanUserManagement view && e.NewValue is true)
            {
                view.RefreshTheme();
                _ = view.RefreshUsersAsync(forceReload: false);
            }
        }
        #endregion

        #region SearchText
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanUserManagement view)
            {
                view.ApplySearchFilter();
            }
        }
        #endregion
        #region Users
        public static readonly DependencyProperty UsersProperty =
            DependencyProperty.Register(
                nameof(Users),
                typeof(ObservableCollection<FlowKanbanUserItem>),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(null));

        public ObservableCollection<FlowKanbanUserItem> Users
        {
            get => EnsureUsers();
            private set => SetValue(UsersProperty, value);
        }
        #endregion

        #region FilteredUsers
        public static readonly DependencyProperty FilteredUsersProperty =
            DependencyProperty.Register(
                nameof(FilteredUsers),
                typeof(ObservableCollection<FlowKanbanUserItem>),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(null));

        public ObservableCollection<FlowKanbanUserItem> FilteredUsers
        {
            get => EnsureFilteredUsers();
            private set => SetValue(FilteredUsersProperty, value);
        }
        #endregion

        #region Providers
        public static readonly DependencyProperty ProvidersProperty =
            DependencyProperty.Register(
                nameof(Providers),
                typeof(ObservableCollection<FlowKanbanProviderSummary>),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(null));

        public ObservableCollection<FlowKanbanProviderSummary> Providers
        {
            get => EnsureProviders();
            private set => SetValue(ProvidersProperty, value);
        }
        #endregion

        #region SelectedUser
        public static readonly DependencyProperty SelectedUserProperty =
            DependencyProperty.Register(
                nameof(SelectedUser),
                typeof(FlowKanbanUserItem),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(null, OnSelectedUserChanged));

        public FlowKanbanUserItem? SelectedUser
        {
            get => (FlowKanbanUserItem?)GetValue(SelectedUserProperty);
            set => SetValue(SelectedUserProperty, value);
        }

        private static void OnSelectedUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanUserManagement view)
            {
                var hasSelection = e.NewValue is FlowKanbanUserItem;
                view.HasSelection = hasSelection;
                view.IsSelectionPromptVisible = !hasSelection;
                view.UpdateSelectedUserLinkState();
                view.NotifyUserCommandsChanged();
            }
        }
        #endregion

        #region Link State
        public static readonly DependencyProperty CanLinkSelectedUserProperty =
            DependencyProperty.Register(
                nameof(CanLinkSelectedUser),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool CanLinkSelectedUser
        {
            get => (bool)GetValue(CanLinkSelectedUserProperty);
            private set => SetValue(CanLinkSelectedUserProperty, value);
        }

        public static readonly DependencyProperty HasLinkedLocalUserProperty =
            DependencyProperty.Register(
                nameof(HasLinkedLocalUser),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool HasLinkedLocalUser
        {
            get => (bool)GetValue(HasLinkedLocalUserProperty);
            private set => SetValue(HasLinkedLocalUserProperty, value);
        }

        public static readonly DependencyProperty LinkedLocalUserNameProperty =
            DependencyProperty.Register(
                nameof(LinkedLocalUserName),
                typeof(string),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(string.Empty));

        public string LinkedLocalUserName
        {
            get => (string)GetValue(LinkedLocalUserNameProperty);
            private set => SetValue(LinkedLocalUserNameProperty, value);
        }
        #endregion

        #region IsLoading
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            private set => SetValue(IsLoadingProperty, value);
        }
        #endregion

        #region Visibility State
        public static readonly DependencyProperty HasAnyUsersProperty =
            DependencyProperty.Register(
                nameof(HasAnyUsers),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool HasAnyUsers
        {
            get => (bool)GetValue(HasAnyUsersProperty);
            private set => SetValue(HasAnyUsersProperty, value);
        }

        public static readonly DependencyProperty HasFilteredUsersProperty =
            DependencyProperty.Register(
                nameof(HasFilteredUsers),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool HasFilteredUsers
        {
            get => (bool)GetValue(HasFilteredUsersProperty);
            private set => SetValue(HasFilteredUsersProperty, value);
        }

        public static readonly DependencyProperty IsEmptyStateVisibleProperty =
            DependencyProperty.Register(
                nameof(IsEmptyStateVisible),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(true));

        public bool IsEmptyStateVisible
        {
            get => (bool)GetValue(IsEmptyStateVisibleProperty);
            private set => SetValue(IsEmptyStateVisibleProperty, value);
        }

        public static readonly DependencyProperty IsSearchEmptyStateVisibleProperty =
            DependencyProperty.Register(
                nameof(IsSearchEmptyStateVisible),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool IsSearchEmptyStateVisible
        {
            get => (bool)GetValue(IsSearchEmptyStateVisibleProperty);
            private set => SetValue(IsSearchEmptyStateVisibleProperty, value);
        }
        #endregion

        #region Provider State
        public static readonly DependencyProperty ProviderCountProperty =
            DependencyProperty.Register(
                nameof(ProviderCount),
                typeof(int),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(0));

        public int ProviderCount
        {
            get => (int)GetValue(ProviderCountProperty);
            private set => SetValue(ProviderCountProperty, value);
        }

        public static readonly DependencyProperty IsLocalProviderProperty =
            DependencyProperty.Register(
                nameof(IsLocalProvider),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool IsLocalProvider
        {
            get => (bool)GetValue(IsLocalProviderProperty);
            private set => SetValue(IsLocalProviderProperty, value);
        }

        public static readonly DependencyProperty IsCompositeProviderProperty =
            DependencyProperty.Register(
                nameof(IsCompositeProvider),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool IsCompositeProvider
        {
            get => (bool)GetValue(IsCompositeProviderProperty);
            private set => SetValue(IsCompositeProviderProperty, value);
        }

        public static readonly DependencyProperty SupportsAvatarsProperty =
            DependencyProperty.Register(
                nameof(SupportsAvatars),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool SupportsAvatars
        {
            get => (bool)GetValue(SupportsAvatarsProperty);
            private set => SetValue(SupportsAvatarsProperty, value);
        }

        public static readonly DependencyProperty SupportsPresenceProperty =
            DependencyProperty.Register(
                nameof(SupportsPresence),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool SupportsPresence
        {
            get => (bool)GetValue(SupportsPresenceProperty);
            private set => SetValue(SupportsPresenceProperty, value);
        }

        public static readonly DependencyProperty SupportsRealtimeProperty =
            DependencyProperty.Register(
                nameof(SupportsRealtime),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool SupportsRealtime
        {
            get => (bool)GetValue(SupportsRealtimeProperty);
            private set => SetValue(SupportsRealtimeProperty, value);
        }

        public static readonly DependencyProperty IsGitHubProviderAvailableProperty =
            DependencyProperty.Register(
                nameof(IsGitHubProviderAvailable),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool IsGitHubProviderAvailable
        {
            get => (bool)GetValue(IsGitHubProviderAvailableProperty);
            private set => SetValue(IsGitHubProviderAvailableProperty, value);
        }
        #endregion

        #region User Count
        public static readonly DependencyProperty UserCountProperty =
            DependencyProperty.Register(
                nameof(UserCount),
                typeof(int),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(0));

        public int UserCount
        {
            get => (int)GetValue(UserCountProperty);
            private set => SetValue(UserCountProperty, value);
        }
        #endregion

        #region Selection
        public static readonly DependencyProperty HasSelectionProperty =
            DependencyProperty.Register(
                nameof(HasSelection),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(false));

        public bool HasSelection
        {
            get => (bool)GetValue(HasSelectionProperty);
            private set => SetValue(HasSelectionProperty, value);
        }

        public static readonly DependencyProperty IsSelectionPromptVisibleProperty =
            DependencyProperty.Register(
                nameof(IsSelectionPromptVisible),
                typeof(bool),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(true));

        public bool IsSelectionPromptVisible
        {
            get => (bool)GetValue(IsSelectionPromptVisibleProperty);
            private set => SetValue(IsSelectionPromptVisibleProperty, value);
        }
        #endregion

        #region Add User Form
        public static readonly DependencyProperty NewUserNameProperty =
            DependencyProperty.Register(
                nameof(NewUserName),
                typeof(string),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(string.Empty, OnNewUserFieldChanged));

        public string NewUserName
        {
            get => (string)GetValue(NewUserNameProperty);
            set => SetValue(NewUserNameProperty, value);
        }

        public static readonly DependencyProperty NewUserEmailProperty =
            DependencyProperty.Register(
                nameof(NewUserEmail),
                typeof(string),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(string.Empty, OnNewUserFieldChanged));

        public string NewUserEmail
        {
            get => (string)GetValue(NewUserEmailProperty);
            set => SetValue(NewUserEmailProperty, value);
        }

        private static void OnNewUserFieldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanUserManagement view)
            {
                view.NotifyUserCommandsChanged();
            }
        }
        #endregion

        #region GitHub Connect
        public static readonly DependencyProperty GitHubTokenProperty =
            DependencyProperty.Register(
                nameof(GitHubToken),
                typeof(string),
                typeof(FlowKanbanUserManagement),
                new PropertyMetadata(string.Empty, OnGitHubTokenChanged));

        public string GitHubToken
        {
            get => (string)GetValue(GitHubTokenProperty);
            set => SetValue(GitHubTokenProperty, value);
        }

        private static void OnGitHubTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanbanUserManagement view)
            {
                view.NotifyGitHubCommandsChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand RefreshCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand RemoveUserCommand { get; }
        public ICommand ConnectGitHubCommand { get; }
        public ICommand DisconnectProviderCommand { get; }
        public ICommand LinkLocalUserCommand { get; }
        public ICommand UnlinkLocalUserCommand { get; }
        #endregion
        private ObservableCollection<FlowKanbanUserItem> EnsureUsers()
        {
            if (GetValue(UsersProperty) is not ObservableCollection<FlowKanbanUserItem> users)
            {
                users = new ObservableCollection<FlowKanbanUserItem>();
                users.CollectionChanged += OnUsersCollectionChanged;
                SetValue(UsersProperty, users);
            }

            return users;
        }

        private ObservableCollection<FlowKanbanUserItem> EnsureFilteredUsers()
        {
            if (GetValue(FilteredUsersProperty) is not ObservableCollection<FlowKanbanUserItem> users)
            {
                users = new ObservableCollection<FlowKanbanUserItem>();
                users.CollectionChanged += OnFilteredUsersCollectionChanged;
                SetValue(FilteredUsersProperty, users);
            }

            return users;
        }

        private ObservableCollection<FlowKanbanProviderSummary> EnsureProviders()
        {
            if (GetValue(ProvidersProperty) is not ObservableCollection<FlowKanbanProviderSummary> providers)
            {
                providers = new ObservableCollection<FlowKanbanProviderSummary>();
                providers.CollectionChanged += OnProvidersCollectionChanged;
                SetValue(ProvidersProperty, providers);
            }

            return providers;
        }

        private void OnUsersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateUserVisibilityState();
        }

        private void OnFilteredUsersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateUserVisibilityState();
        }

        private void OnProvidersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateProviderState();
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            AttachProvider(UserProvider);
            RefreshProviderSummaries();
            if (IsActive)
            {
                _ = RefreshUsersAsync(forceReload: false);
            }

            if (!_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged += OnLocalizationCultureChanged;
                _isLocalizationSubscribed = true;
            }
        }

        private void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            DetachProvider();
            _refreshCts?.Cancel();
            _refreshCts = null;

            if (_isLocalizationSubscribed)
            {
                FloweryLocalization.CultureChanged -= OnLocalizationCultureChanged;
                _isLocalizationSubscribed = false;
            }
        }

        private void OnLocalizationCultureChanged(object? sender, string cultureName)
        {
            RebindLocalizationDataContext();
            _ = RefreshUsersAsync(forceReload: true);
        }

        private void BindRootDataContext()
        {
            if (_rootElement == null)
                return;

            BindingOperations.SetBinding(_rootElement, FrameworkElement.DataContextProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Localization))
            });
        }

        private void RebindLocalizationDataContext()
        {
            if (_rootElement == null)
                return;

            _rootElement.DataContext = null;
            BindRootDataContext();
        }

        internal void RefreshTheme()
        {
            Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            RefreshThemeVisuals();
        }

        private void RefreshThemeVisuals()
        {
            var root = _rootElement ?? this;

            foreach (var element in EnumerateVisualTree(root))
            {
                if (element is DaisyCard card)
                {
                    ApplyCardTheme(card);
                }
            }

            ApplyPanelTheme(_userListPanel, "DaisyBase200Brush", "DaisyBase300Brush");
            ApplyPanelTheme(_userDetailsPanel, "DaisyBase200Brush", "DaisyBase300Brush");
            ApplyPanelTheme(_userDetailsCard, "DaisyBase100Brush", "DaisyBase300Brush");
            ApplyPanelTheme(_userDetailsEmptyCard, "DaisyBase100Brush", "DaisyBase300Brush");
        }

        private static void ApplyCardTheme(DaisyCard card)
        {
            var variant = card.ColorVariant;
            if (variant == DaisyColor.Default)
            {
                card.Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
                card.Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
                return;
            }

            var variantName = variant.ToString();
            var contentVariantName = variant is DaisyColor.Base200 or DaisyColor.Base300 ? "Base" : variantName;

            card.Background = DaisyResourceLookup.GetBrush($"Daisy{variantName}Brush");
            card.Foreground = DaisyResourceLookup.GetBrush($"Daisy{contentVariantName}ContentBrush");
        }

        private static void ApplyPanelTheme(Border? panel, string backgroundKey, string? borderKey)
        {
            if (panel == null)
                return;

            panel.Background = DaisyResourceLookup.GetBrush(backgroundKey);

            if (!string.IsNullOrWhiteSpace(borderKey))
            {
                panel.BorderBrush = DaisyResourceLookup.GetBrush(borderKey);
            }
        }

        private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            yield return root;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                foreach (var descendant in EnumerateVisualTree(child))
                {
                    yield return descendant;
                }
            }
        }

        private void AttachProvider(IUserProvider? provider)
        {
            if (ReferenceEquals(_trackedProvider, provider))
                return;

            DetachProvider();

            if (provider == null)
                return;

            _trackedProvider = provider;
            _trackedProvider.UsersChanged += OnProviderUsersChanged;
        }

        private void DetachProvider()
        {
            if (_trackedProvider == null)
                return;

            _trackedProvider.UsersChanged -= OnProviderUsersChanged;
            _trackedProvider = null;
        }

        private void OnProviderUsersChanged()
        {
            _ = RefreshUsersAsync(forceReload: true, refreshProvider: false);
        }

        private async void ExecuteRefreshUsers()
        {
            await RefreshUsersAsync(forceReload: true);
        }

        private bool CanExecuteAddUser()
        {
            return IsLocalProvider && !string.IsNullOrWhiteSpace(NewUserName);
        }

        private void ExecuteAddUser()
        {
            if (!IsLocalProvider)
                return;

#pragma warning disable CA1416 // LocalUserProvider is supported across Uno heads used by this control.
            var provider = UserProvider as LocalUserProvider;
            if (provider == null)
                return;

            var name = NewUserName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            var email = string.IsNullOrWhiteSpace(NewUserEmail) ? null : NewUserEmail.Trim();
            provider.AddUser(name, email);
#pragma warning restore CA1416
            NewUserName = string.Empty;
            NewUserEmail = string.Empty;
        }

        private bool CanExecuteRemoveUser()
        {
            return IsLocalProvider && SelectedUser != null && !SelectedUser.IsCurrentUser;
        }

        private void ExecuteRemoveUser()
        {
            if (!IsLocalProvider)
                return;

#pragma warning disable CA1416 // LocalUserProvider is supported across Uno heads used by this control.
            var provider = UserProvider as LocalUserProvider;
            if (provider == null)
                return;

            var selected = SelectedUser;
            if (selected == null)
                return;

            provider.RemoveUser(selected.RawId);
#pragma warning restore CA1416
        }

        private bool CanExecuteConnectGitHub(string? providerKey)
        {
            return !string.IsNullOrWhiteSpace(providerKey);
        }

        private void ExecuteConnectGitHub(string? providerKey)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
                return;

            _ = ConnectProviderAsync(providerKey);
        }

        private bool CanExecuteDisconnectProvider(string? providerKey)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                return false;
            }

            var provider = ResolveProviderByKey(providerKey);
            return provider is ITokenSaveProvider && TryGetProviderHasToken(provider);
        }

        private void ExecuteDisconnectProvider(string? providerKey)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
                return;

            _ = DisconnectProviderAsync(providerKey);
        }

        private bool CanExecuteLinkLocalUser()
        {
            return CanLinkSelectedUser && SelectedUser != null && XamlRoot != null;
        }

        private async void ExecuteLinkLocalUser()
        {
            if (XamlRoot == null)
                return;

            var selected = SelectedUser;
            if (selected == null)
                return;

            if (string.Equals(selected.ProviderKey, LocalProviderKey, StringComparison.Ordinal))
                return;

            var localProvider = ResolveLocalProvider();
            if (localProvider == null)
                return;

            var localUsers = (await localProvider.GetAllUsersAsync()).ToList();
            if (localUsers.Count == 0)
                return;

            var message = FloweryLocalization.GetStringInternal("Kanban_Users_LinkLocal_Message");
            var buttonText = FloweryLocalization.GetStringInternal("Kanban_Users_LinkLocal_Button");
            var linkedUser = await LinkLocalUserDialog.ShowAsync(message, buttonText, localUsers, XamlRoot);
            if (linkedUser == null)
                return;

            _identityLinkStore.SetLink(
                selected.ProviderKey,
                selected.RawId,
                linkedUser.RawId,
                linkedUser.DisplayName);

            UpdateSelectedUserLinkState();
        }

        private bool CanExecuteUnlinkLocalUser()
        {
            return CanLinkSelectedUser && HasLinkedLocalUser && SelectedUser != null;
        }

        private void ExecuteUnlinkLocalUser()
        {
            var selected = SelectedUser;
            if (selected == null)
                return;

            if (!CanLinkSelectedUser || !HasLinkedLocalUser)
                return;

            if (_identityLinkStore.RemoveLink(selected.ProviderKey, selected.RawId))
            {
                UpdateSelectedUserLinkState();
            }
        }

        private void NotifyUserCommandsChanged()
        {
            if (AddUserCommand is RelayCommand addUser)
                addUser.RaiseCanExecuteChanged();
            if (RemoveUserCommand is RelayCommand removeUser)
                removeUser.RaiseCanExecuteChanged();
            if (LinkLocalUserCommand is RelayCommand linkLocal)
                linkLocal.RaiseCanExecuteChanged();
            if (UnlinkLocalUserCommand is RelayCommand unlinkLocal)
                unlinkLocal.RaiseCanExecuteChanged();
        }

        private void NotifyGitHubCommandsChanged()
        {
            if (ConnectGitHubCommand is RelayCommand<string> connectGitHub)
                connectGitHub.RaiseCanExecuteChanged();
            if (DisconnectProviderCommand is RelayCommand<string> disconnectProvider)
                disconnectProvider.RaiseCanExecuteChanged();
        }

        private void UpdateSelectedUserLinkState()
        {
            var selected = SelectedUser;
            if (selected == null)
            {
                CanLinkSelectedUser = false;
                HasLinkedLocalUser = false;
                LinkedLocalUserName = string.Empty;
                NotifyUserCommandsChanged();
                return;
            }

            var localProvider = ResolveLocalProvider();
            var isLinkable = localProvider != null
                             && !string.Equals(selected.ProviderKey, LocalProviderKey, StringComparison.Ordinal);

            CanLinkSelectedUser = isLinkable;

            if (!isLinkable)
            {
                HasLinkedLocalUser = false;
                LinkedLocalUserName = string.Empty;
                NotifyUserCommandsChanged();
                return;
            }

            var link = _identityLinkStore.FindLink(selected.ProviderKey, selected.RawId);
            HasLinkedLocalUser = link != null;
            LinkedLocalUserName = link?.LocalDisplayName ?? string.Empty;
            NotifyUserCommandsChanged();
        }

        private async Task RefreshUsersAsync(bool forceReload, bool refreshProvider = true)
        {
            var provider = UserProvider;
            if (provider == null)
            {
                ClearUsers();
                return;
            }

            if (!forceReload && Users.Count > 0)
            {
                ApplySearchFilter();
                return;
            }

            _refreshCts?.Cancel();
            var refreshCts = new CancellationTokenSource();
            _refreshCts = refreshCts;
            var token = refreshCts.Token;

            IsLoading = true;
            try
            {
                if (forceReload && refreshProvider)
                {
                    await provider.RefreshAsync(token);
                    if (token.IsCancellationRequested)
                        return;
                }

                var users = await provider.GetAllUsersAsync(token);
                var currentUser = await provider.GetCurrentUserAsync(token);
                if (token.IsCancellationRequested)
                    return;

                var items = await BuildUserItemsAsync(users, provider, currentUser, token);
                if (token.IsCancellationRequested)
                    return;

                UpdateUsers(items);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlowKanban user refresh failed: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(_refreshCts, refreshCts))
                {
                    _refreshCts = null;
                    IsLoading = false;
                }
            }
        }

        private void UpdateUsers(IReadOnlyList<FlowKanbanUserItem> items)
        {
            var selectedId = SelectedUser?.Id;
            var users = EnsureUsers();
            users.Clear();
            foreach (var item in items)
            {
                users.Add(item);
            }

            ApplySearchFilter();

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                SelectedUser = users.FirstOrDefault(u => string.Equals(u.Id, selectedId, StringComparison.Ordinal));
            }
            else
            {
                SelectedUser = null;
            }

            UserCount = users.Count;
            UpdateUserVisibilityState();
        }

        private void ClearUsers()
        {
            EnsureUsers().Clear();
            EnsureFilteredUsers().Clear();
            SelectedUser = null;
            UserCount = 0;
            IsLoading = false;
            UpdateUserVisibilityState();
        }

        private void ApplySearchFilter()
        {
            var filter = SearchText?.Trim();
            var users = EnsureUsers();
            var filtered = EnsureFilteredUsers();
            filtered.Clear();

            if (string.IsNullOrWhiteSpace(filter))
            {
                foreach (var item in users)
                {
                    filtered.Add(item);
                }
            }
            else
            {
                foreach (var item in users)
                {
                    if (item.Matches(filter))
                    {
                        filtered.Add(item);
                    }
                }
            }

            if (SelectedUser != null && !filtered.Contains(SelectedUser))
            {
                SelectedUser = null;
            }

            UpdateUserVisibilityState();
        }

        private void UpdateUserVisibilityState()
        {
            var hasAny = Users.Count > 0;
            var hasFiltered = FilteredUsers.Count > 0;
            HasAnyUsers = hasAny;
            HasFilteredUsers = hasFiltered;
            IsEmptyStateVisible = !hasAny && string.IsNullOrWhiteSpace(SearchText);
            IsSearchEmptyStateVisible = !hasFiltered && !string.IsNullOrWhiteSpace(SearchText);
        }

        private void RefreshProviderSummaries()
        {
            var providers = EnsureProviders();
            providers.Clear();

            var provider = UserProvider;
            IsLocalProvider = provider is LocalUserProvider;
            IsCompositeProvider = provider is ICompositeUserProvider;
            SupportsAvatars = provider?.SupportsAvatars ?? false;
            SupportsPresence = provider?.SupportsPresence ?? false;
            SupportsRealtime = provider?.SupportsRealtime ?? false;
            IsGitHubProviderAvailable = ResolveGitHubProvider() != null;

            if (provider is ICompositeUserProvider composite)
            {
                foreach (var key in composite.RegisteredProviderKeys)
                {
                    var child = composite.GetProviderByKey(key);
                    if (child == null)
                        continue;

                    var supportsConnect = SupportsConnect(child);
                    var hasToken = supportsConnect && TryGetProviderHasToken(child);
                    var supportsDisconnect = child is ITokenSaveProvider;
                    providers.Add(new FlowKanbanProviderSummary(
                        child.DisplayName,
                        child.ProviderKey,
                        child.ImplementationVersion,
                        child.SupportsAvatars,
                        child.SupportsPresence,
                        child.SupportsRealtime,
                        string.Equals(child.ProviderKey, composite.DefaultProviderKey, StringComparison.Ordinal),
                        supportsConnect,
                        hasToken,
                        supportsDisconnect));
                }
            }
            else if (provider != null)
            {
                var supportsConnect = SupportsConnect(provider);
                var hasToken = supportsConnect && TryGetProviderHasToken(provider);
                var supportsDisconnect = provider is ITokenSaveProvider;
                providers.Add(new FlowKanbanProviderSummary(
                    provider.DisplayName,
                    provider.ProviderKey,
                    provider.ImplementationVersion,
                    provider.SupportsAvatars,
                    provider.SupportsPresence,
                    provider.SupportsRealtime,
                    isDefault: true,
                    supportsConnect: supportsConnect,
                    hasToken: hasToken,
                    supportsDisconnect: supportsDisconnect));
            }

            UpdateProviderState();
            NotifyUserCommandsChanged();
            NotifyGitHubCommandsChanged();
        }

        private static bool TryGetProviderHasToken(IUserProvider provider)
        {
            return provider is ITokenStateProvider tokenState && tokenState.HasToken;
        }

        private static bool SupportsConnect(IUserProvider provider)
        {
            return provider is IInteractiveAuthProvider || provider is ITokenSaveProvider;
        }

        private void UpdateProviderState()
        {
            ProviderCount = Providers.Count;
        }

        private async Task<IReadOnlyList<FlowKanbanUserItem>> BuildUserItemsAsync(
            IEnumerable<IFlowUser> users,
            IUserProvider provider,
            IFlowUser? currentUser,
            CancellationToken token)
        {
            var currentId = currentUser?.Id;
            var userList = users?.Where(u => u != null).ToList() ?? new List<IFlowUser>();
            var providerNameMap = BuildProviderNameMap(provider);

            var itemTasks = new List<Task<FlowKanbanUserItem?>>();
            foreach (var user in userList)
            {
                var displayName = ResolveProviderDisplayName(provider, providerNameMap, user.ProviderKey);
                itemTasks.Add(CreateUserItemAsync(user, provider, displayName, currentId, token));
            }
            var items = await Task.WhenAll(itemTasks);
            return items
                .Where(item => item != null)
                .Select(item => item!)
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Dictionary<string, string> BuildProviderNameMap(IUserProvider provider)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            if (provider is ICompositeUserProvider composite)
            {
                foreach (var key in composite.RegisteredProviderKeys)
                {
                    var child = composite.GetProviderByKey(key);
                    if (child == null || string.IsNullOrWhiteSpace(child.ProviderKey))
                        continue;

                    map[child.ProviderKey] = child.DisplayName;
                }
            }

            if (!string.IsNullOrWhiteSpace(provider.ProviderKey) && !map.ContainsKey(provider.ProviderKey))
            {
                map[provider.ProviderKey] = provider.DisplayName;
            }

            return map;
        }

        private static string ResolveProviderDisplayName(
            IUserProvider provider,
            IReadOnlyDictionary<string, string> providerNameMap,
            string providerKey)
        {
            if (!string.IsNullOrWhiteSpace(providerKey) && providerNameMap.TryGetValue(providerKey, out var displayName))
            {
                return displayName;
            }

            return provider.DisplayName;
        }

        private IUserProvider? ResolveGitHubProvider()
        {
            var provider = UserProvider;
            if (provider == null)
                return null;

            if (string.Equals(provider.ProviderKey, GitHubProviderKey, StringComparison.Ordinal))
                return provider;

            if (provider is ICompositeUserProvider composite)
            {
                return composite.GetProviderByKey(GitHubProviderKey);
            }

            return null;
        }

        private IUserProvider? ResolveLocalProvider()
        {
            var provider = UserProvider;
            if (provider == null)
                return null;

            if (string.Equals(provider.ProviderKey, LocalProviderKey, StringComparison.Ordinal))
                return provider;

            if (provider is ICompositeUserProvider composite)
            {
                return composite.GetProviderByKey(LocalProviderKey);
            }

            return null;
        }

        private static bool TryInvokeSaveToken(IUserProvider provider, string token)
        {
            if (provider is ITokenSaveProvider tokenProvider)
            {
                tokenProvider.SaveToken(token);
                return true;
            }

            return false;
        }

        private async Task ConnectProviderAsync(string providerKey)
        {
            try
            {
                if (XamlRoot == null)
                    return;

                var provider = ResolveProviderByKey(providerKey);
                if (provider == null)
                    return;

                if (provider is IInteractiveAuthProvider interactive)
                {
                    var success = await interactive.AuthenticateAsync();
                    if (success)
                    {
                        RefreshProviderSummaries();
                    }
                    return;
                }

                var title = FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Title");
                var message = FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Message");
                var placeholder = FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Placeholder");
                var connectText = FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Button");

                var token = await GitHubConnectDialog.ShowAsync(title, message, placeholder, connectText, XamlRoot);
                if (string.IsNullOrWhiteSpace(token))
                    return;

                if (!TryInvokeSaveToken(provider, token))
                    return;

                if (provider is ITokenValidationProvider validator &&
                    string.Equals(provider.ProviderKey, GitHubProviderKey, StringComparison.Ordinal))
                {
                    var validation = await validator.ValidateAccessAsync();
                    if (!validation.IsSuccess)
                    {
                        _ = TryInvokeSaveToken(provider, string.Empty);
                        GitHubToken = string.Empty;
                        RefreshProviderSummaries();

                        var errorTitle = FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Error_Title");
                        var errorMessage = validation.IsMissingScope
                            ? FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Error_Scopes")
                            : FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Error_Generic");

                        await ProviderErrorDialog.ShowAsync(errorTitle, errorMessage, XamlRoot);
                        return;
                    }
                }

                GitHubToken = string.Empty;
                RefreshProviderSummaries();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlowKanban connect failed: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private async Task DisconnectProviderAsync(string providerKey)
        {
            try
            {
                if (!string.Equals(providerKey, LocalProviderKey, StringComparison.Ordinal))
                {
                    var title = FloweryLocalization.GetStringInternal("Kanban_Users_Disconnect_Title");
                    var message = FloweryLocalization.GetStringInternal("Kanban_Users_Disconnect_Message");
                    var xamlRoot = XamlRoot;
                    if (xamlRoot == null)
                    {
                        return;
                    }
                    var confirmed = await ProviderConfirmDialog.ShowAsync(
                        title,
                        message,
                        FloweryLocalization.GetStringInternal("Common_Disconnect"),
                        xamlRoot);
                    if (!confirmed)
                    {
                        return;
                    }
                }

                var provider = ResolveProviderByKey(providerKey);
                if (provider == null)
                {
                    return;
                }

                if (!TryInvokeSaveToken(provider, string.Empty))
                {
                    return;
                }

                GitHubToken = string.Empty;
                _identityLinkStore.RemoveLinksForProvider(providerKey);
                if (SelectedUser != null &&
                    string.Equals(SelectedUser.ProviderKey, providerKey, StringComparison.Ordinal))
                {
                    SelectedUser = null;
                }

                await RefreshUsersAsync(forceReload: true);
                RefreshProviderSummaries();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlowKanban disconnect failed: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private IUserProvider? ResolveProviderByKey(string providerKey)
        {
            var provider = UserProvider;
            if (provider == null)
                return null;

            if (string.Equals(provider.ProviderKey, providerKey, StringComparison.Ordinal))
                return provider;

            if (provider is ICompositeUserProvider composite)
            {
                return composite.GetProviderByKey(providerKey);
            }

            return null;
        }

        private async Task<FlowKanbanUserItem?> CreateUserItemAsync(
            IFlowUser user,
            IUserProvider provider,
            string providerDisplayName,
            string? currentUserId,
            CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return null;

            var initials = BuildInitials(user.DisplayName);
            var statusLabel = GetStatusLabel(user.Status);
            var avatarStatus = provider.SupportsPresence ? MapStatus(user.Status) : DaisyStatus.None;
            var avatarSource = await LoadAvatarSourceAsync(user, provider, token);
            var isCurrent = !string.IsNullOrWhiteSpace(currentUserId)
                            && string.Equals(user.Id, currentUserId, StringComparison.Ordinal);

            return new FlowKanbanUserItem(
                user,
                providerDisplayName,
                initials,
                statusLabel,
                avatarStatus,
                avatarSource,
                isCurrent);
        }

        private static string BuildInitials(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "??";

            var parts = displayName.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
                return "??";

            if (parts.Length == 1)
            {
                var name = parts[0];
                return name.Length >= 2
                    ? $"{char.ToUpperInvariant(name[0])}{char.ToUpperInvariant(name[1])}"
                    : $"{char.ToUpperInvariant(name[0])}";
            }

            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }

        private static DaisyStatus MapStatus(FlowUserStatus status)
        {
            return status switch
            {
                FlowUserStatus.Online => DaisyStatus.Online,
                FlowUserStatus.Offline => DaisyStatus.Offline,
                _ => DaisyStatus.None
            };
        }

        private static string GetStatusLabel(FlowUserStatus status)
        {
            return status switch
            {
                FlowUserStatus.Online => FloweryLocalization.GetStringInternal("Kanban_Users_Status_Online"),
                FlowUserStatus.Away => FloweryLocalization.GetStringInternal("Kanban_Users_Status_Away"),
                FlowUserStatus.Busy => FloweryLocalization.GetStringInternal("Kanban_Users_Status_Busy"),
                FlowUserStatus.DoNotDisturb => FloweryLocalization.GetStringInternal("Kanban_Users_Status_DoNotDisturb"),
                FlowUserStatus.Offline => FloweryLocalization.GetStringInternal("Kanban_Users_Status_Offline"),
                _ => FloweryLocalization.GetStringInternal("Kanban_Users_Status_Unknown")
            };
        }

        private static async Task<ImageSource?> LoadAvatarSourceAsync(IFlowUser user, IUserProvider provider, CancellationToken token)
        {
            if (!provider.SupportsAvatars)
                return null;

            if (token.IsCancellationRequested)
                return null;

            if (user.AvatarBytes != null && user.AvatarBytes.Length > 0)
            {
                return await BytesToImageSourceAsync(user.AvatarBytes);
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl) &&
                Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out var uri))
            {
                return new BitmapImage(uri);
            }

            return null;
        }

        private static async Task<ImageSource?> BytesToImageSourceAsync(byte[] pngBytes)
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(pngBytes.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlowKanban user avatar load failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }
    }
    public sealed class FlowKanbanProviderSummary
    {
        public FlowKanbanProviderSummary(
            string displayName,
            string providerKey,
            string implementationVersion,
            bool supportsAvatars,
            bool supportsPresence,
            bool supportsRealtime,
            bool isDefault,
            bool supportsConnect,
            bool hasToken,
            bool supportsDisconnect)
        {
            DisplayName = displayName;
            ProviderKey = providerKey;
            ImplementationVersion = implementationVersion;
            SupportsAvatars = supportsAvatars;
            SupportsPresence = supportsPresence;
            SupportsRealtime = supportsRealtime;
            IsDefault = isDefault;
            SupportsConnect = supportsConnect;
            HasToken = hasToken;
            SupportsDisconnect = supportsDisconnect;
        }

        public string DisplayName { get; }
        public string ProviderKey { get; }
        public string ImplementationVersion { get; }
        public bool SupportsAvatars { get; }
        public bool SupportsPresence { get; }
        public bool SupportsRealtime { get; }
        public bool IsDefault { get; }
        public bool SupportsConnect { get; }
        public bool HasToken { get; }
        public bool SupportsDisconnect { get; }
        public bool ShouldShowConnect => SupportsConnect && !HasToken;
        public bool ShouldShowConnected => SupportsConnect && HasToken;
        public bool ShouldShowReconnect => SupportsConnect && HasToken;
        public bool ShouldShowDisconnect => SupportsDisconnect && HasToken;
    }

    internal sealed partial class GitHubConnectDialog : FloweryDialogBase
    {
        private readonly TaskCompletionSource<string?> _tcs = new();
        private readonly XamlRoot _xamlRoot;
        private Panel? _hostPanel;
        private bool _isClosing;
        private long _isOpenCallbackToken;

        private readonly DaisyPasswordBox _tokenInput;
        private readonly DaisyButton _connectButton;
        private readonly DaisyButton _cancelButton;
        private const double CompactDialogMaxWidth = 300;

        private GitHubConnectDialog(
            string title,
            string message,
            string placeholder,
            string connectText,
            XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;

            var titleStyle = Application.Current?.Resources["TitleTextBlockStyle"] as Style;
            var bodyStyle = Application.Current?.Resources["BodyTextBlockStyle"] as Style;
            var captionStyle = Application.Current?.Resources["CaptionTextBlockStyle"] as Style;
            _tokenInput = new DaisyPasswordBox
            {
                PlaceholderText = placeholder,
                Variant = Enums.DaisyInputVariant.Bordered,
                Size = Enums.DaisySize.Small,
                BorderRingBrush = DaisyResourceLookup.GetBrush("DaisyAccentBrush"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            AutomationProperties.SetName(_tokenInput, placeholder);
            _tokenInput.PasswordChanged += OnTokenChanged;

            var fieldStack = new StackPanel
            {
                Spacing = 8
            };
            fieldStack.Children.Add(_tokenInput);

            var sectionBackground = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var sectionBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            var fieldCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = fieldStack
            };

            var buttonPanel = CreateStandardButtonFooter(out _connectButton, out _cancelButton, connectText, FloweryLocalization.GetStringInternal("Common_Cancel"));
            _connectButton.IsEnabled = false;

            var footerCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = buttonPanel
            };

            var contentStack = new StackPanel
            {
                Spacing = 16,
                MinWidth = 200
            };
            contentStack.Children.Add(new TextBlock
            {
                Text = message,
                Style = bodyStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            contentStack.Children.Add(new TextBlock
            {
                Text = FloweryLocalization.GetStringInternal("Kanban_Users_GitHub_Hint"),
                Style = captionStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            contentStack.Children.Add(fieldCard);
            contentStack.Children.Add(footerCard);

            Content = contentStack;
            ApplySmartSizingWithAutoHeight(xamlRoot);
            ClampDialogWidth(CompactDialogMaxWidth);

            _connectButton.Click += OnConnectClicked;
            _cancelButton.Click += OnCancelClicked;
            _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
        }

        public static Task<string?> ShowAsync(
            string title,
            string message,
            string placeholder,
            string connectText,
            XamlRoot xamlRoot)
        {
            if (xamlRoot == null)
                return Task.FromResult<string?>(null);

            var dialog = new GitHubConnectDialog(title, message, placeholder, connectText, xamlRoot);
            return dialog.ShowInternalAsync();
        }

        private Task<string?> ShowInternalAsync()
        {
            _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
            if (_hostPanel == null)
            {
                Close(null);
                return _tcs.Task;
            }

            _hostPanel.Children.Add(this);
            IsOpen = true;
            _tokenInput.FocusInput(selectAll: true);
            return _tcs.Task;
        }

        private void OnTokenChanged(object sender, RoutedEventArgs e)
        {
            _connectButton.IsEnabled = !string.IsNullOrWhiteSpace(_tokenInput.Password);
        }

        private void OnConnectClicked(object sender, RoutedEventArgs e)
        {
            CommitAndClose();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (!IsOpen && !_isClosing)
            {
                Close(null);
            }
        }

        private void CommitAndClose()
        {
            var token = _tokenInput.Password?.Trim();
            Close(string.IsNullOrWhiteSpace(token) ? null : token);
        }

        private void Close(string? result)
        {
            if (_isClosing)
                return;

            _isClosing = true;
            IsOpen = false;

            if (_isOpenCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                _isOpenCallbackToken = 0;
            }

            if (_hostPanel != null)
            {
                _hostPanel.Children.Remove(this);
                _hostPanel = null;
            }

            _tcs.TrySetResult(result);
        }
    }

    internal sealed partial class ProviderErrorDialog : FloweryDialogBase
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        private readonly XamlRoot _xamlRoot;
        private Panel? _hostPanel;
        private bool _isClosing;
        private long _isOpenCallbackToken;

        private readonly DaisyButton _closeButton;
        private const double CompactDialogMaxWidth = 360;

        private ProviderErrorDialog(string title, string message, XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;

            var titleStyle = Application.Current?.Resources["TitleTextBlockStyle"] as Style;
            var bodyStyle = Application.Current?.Resources["BodyTextBlockStyle"] as Style;

            var titleText = new TextBlock
            {
                Text = title,
                Style = titleStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            var messageText = new TextBlock
            {
                Text = message,
                Style = bodyStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            _closeButton = new DaisyButton
            {
                Content = FloweryLocalization.GetStringInternal("Common_Close"),
                Variant = DaisyButtonVariant.Primary,
                Size = Enums.DaisySize.Medium,
                MinWidth = 96,
                IsTabStop = true,
                UseSystemFocusVisuals = true
            };
            _closeButton.Click += OnCloseClicked;

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttonPanel.Children.Add(_closeButton);

            var sectionBackground = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var sectionBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            var footerCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = buttonPanel
            };

            var contentStack = new StackPanel
            {
                Spacing = 16,
                MinWidth = 200
            };
            contentStack.Children.Add(titleText);
            contentStack.Children.Add(messageText);
            contentStack.Children.Add(footerCard);

            Content = contentStack;
            ApplySmartSizingWithAutoHeight(xamlRoot);
            ClampDialogWidth(CompactDialogMaxWidth);

            _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
        }

        public static Task ShowAsync(string title, string message, XamlRoot xamlRoot)
        {
            if (xamlRoot == null)
                return Task.CompletedTask;

            var dialog = new ProviderErrorDialog(title, message, xamlRoot);
            return dialog.ShowInternalAsync();
        }

        private Task ShowInternalAsync()
        {
            _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
            if (_hostPanel == null)
            {
                Close();
                return _tcs.Task;
            }

            _hostPanel.Children.Add(this);
            IsOpen = true;
            _closeButton.Focus(FocusState.Programmatic);
            return _tcs.Task;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (!IsOpen && !_isClosing)
            {
                Close();
            }
        }

        private void Close()
        {
            if (_isClosing)
                return;

            _isClosing = true;
            IsOpen = false;

            if (_isOpenCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                _isOpenCallbackToken = 0;
            }

            if (_hostPanel != null)
            {
                _hostPanel.Children.Remove(this);
                _hostPanel = null;
            }

            _tcs.TrySetResult(true);
        }
    }

    internal sealed partial class ProviderConfirmDialog : FloweryDialogBase
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        private readonly XamlRoot _xamlRoot;
        private Panel? _hostPanel;
        private bool _isClosing;
        private long _isOpenCallbackToken;

        private readonly DaisyButton _confirmButton;
        private readonly DaisyButton _cancelButton;
        private const double CompactDialogMaxWidth = 360;

        private ProviderConfirmDialog(string title, string message, string confirmText, XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;

            var titleStyle = Application.Current?.Resources["TitleTextBlockStyle"] as Style;
            var bodyStyle = Application.Current?.Resources["BodyTextBlockStyle"] as Style;

            var titleText = new TextBlock
            {
                Text = title,
                Style = titleStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            var messageText = new TextBlock
            {
                Text = message,
                Style = bodyStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            var buttonPanel = CreateStandardButtonFooter(out _confirmButton, out _cancelButton, confirmText, FloweryLocalization.GetStringInternal("Common_Cancel"));
            _confirmButton.Variant = DaisyButtonVariant.Error;

            var sectionBackground = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var sectionBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            var footerCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = buttonPanel
            };

            var contentStack = new StackPanel
            {
                Spacing = 16,
                MinWidth = 200
            };
            contentStack.Children.Add(titleText);
            contentStack.Children.Add(messageText);
            contentStack.Children.Add(footerCard);

            Content = contentStack;
            ApplySmartSizingWithAutoHeight(xamlRoot);
            ClampDialogWidth(CompactDialogMaxWidth);

            _confirmButton.Click += OnConfirmClicked;
            _cancelButton.Click += OnCancelClicked;
            _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
        }

        public static Task<bool> ShowAsync(string title, string message, string confirmText, XamlRoot xamlRoot)
        {
            if (xamlRoot == null)
                return Task.FromResult(false);

            var dialog = new ProviderConfirmDialog(title, message, confirmText, xamlRoot);
            return dialog.ShowInternalAsync();
        }

        private Task<bool> ShowInternalAsync()
        {
            _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
            if (_hostPanel == null)
            {
                Close(false);
                return _tcs.Task;
            }

            _hostPanel.Children.Add(this);
            IsOpen = true;
            _cancelButton.Focus(FocusState.Programmatic);
            return _tcs.Task;
        }

        private void OnConfirmClicked(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (!IsOpen && !_isClosing)
            {
                Close(false);
            }
        }

        private void Close(bool result)
        {
            if (_isClosing)
                return;

            _isClosing = true;
            IsOpen = false;

            if (_isOpenCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                _isOpenCallbackToken = 0;
            }

            if (_hostPanel != null)
            {
                _hostPanel.Children.Remove(this);
                _hostPanel = null;
            }

            _tcs.TrySetResult(result);
        }
    }

    internal sealed partial class LinkLocalUserDialog : FloweryDialogBase
    {
        private readonly TaskCompletionSource<IFlowUser?> _tcs = new();
        private readonly XamlRoot _xamlRoot;
        private Panel? _hostPanel;
        private bool _isClosing;
        private long _isOpenCallbackToken;

        private readonly DaisySelect _userSelect;
        private readonly DaisyButton _linkButton;
        private readonly DaisyButton _cancelButton;
        private IFlowUser? _selectedUser;
        private readonly IReadOnlyList<LocalUserOption> _localUserOptions;
        private const double CompactDialogMaxWidth = 300;

        private LinkLocalUserDialog(
            string message,
            string linkText,
            IReadOnlyList<IFlowUser> localUsers,
            XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;

            var bodyStyle = Application.Current?.Resources["BodyTextBlockStyle"] as Style;
            var sectionBackground = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var sectionBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            _localUserOptions = localUsers
                .Select(user => new LocalUserOption(user))
                .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _userSelect = new DaisySelect
            {
                ItemsSource = _localUserOptions,
                PlaceholderText = FloweryLocalization.GetStringInternal("Kanban_Users_LinkLocal_Placeholder"),
                Variant = Enums.DaisySelectVariant.Bordered,
                Size = Enums.DaisySize.Small,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _userSelect.SelectionChanged += OnSelectionChanged;

            var selectCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = _userSelect
            };

            var buttonPanel = CreateStandardButtonFooter(out _linkButton, out _cancelButton, linkText, FloweryLocalization.GetStringInternal("Common_Cancel"));
            _linkButton.IsEnabled = false;

            var footerCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = buttonPanel
            };

            var contentStack = new StackPanel
            {
                Spacing = 16,
                MinWidth = 200
            };
            contentStack.Children.Add(new TextBlock
            {
                Text = message,
                Style = bodyStyle,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            contentStack.Children.Add(selectCard);
            contentStack.Children.Add(footerCard);

            Content = contentStack;
            ApplySmartSizingWithAutoHeight(xamlRoot);
            ClampDialogWidth(CompactDialogMaxWidth);

            _linkButton.Click += OnLinkClicked;
            _cancelButton.Click += OnCancelClicked;
            _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
        }

        public static Task<IFlowUser?> ShowAsync(
            string message,
            string linkText,
            IReadOnlyList<IFlowUser> localUsers,
            XamlRoot xamlRoot)
        {
            if (xamlRoot == null || localUsers.Count == 0)
                return Task.FromResult<IFlowUser?>(null);

            var dialog = new LinkLocalUserDialog(message, linkText, localUsers, xamlRoot);
            return dialog.ShowInternalAsync();
        }

        private Task<IFlowUser?> ShowInternalAsync()
        {
            _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
            if (_hostPanel == null)
            {
                Close(null);
                return _tcs.Task;
            }

            _hostPanel.Children.Add(this);
            IsOpen = true;
            _userSelect.Focus(FocusState.Programmatic);
            return _tcs.Task;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = (_userSelect.SelectedItem as LocalUserOption)?.User;
            _linkButton.IsEnabled = _selectedUser != null;
        }

        private void OnLinkClicked(object sender, RoutedEventArgs e)
        {
            CommitAndClose();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (!IsOpen && !_isClosing)
            {
                Close(null);
            }
        }

        private void CommitAndClose()
        {
            Close(_selectedUser);
        }

        private void Close(IFlowUser? result)
        {
            if (_isClosing)
                return;

            _isClosing = true;
            IsOpen = false;

            if (_isOpenCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                _isOpenCallbackToken = 0;
            }

            if (_hostPanel != null)
            {
                _hostPanel.Children.Remove(this);
                _hostPanel = null;
            }

            _tcs.TrySetResult(result);
        }
    }

    public sealed class FlowKanbanUserItem
    {
        public FlowKanbanUserItem(
            IFlowUser user,
            string providerDisplayName,
            string initials,
            string statusLabel,
            DaisyStatus avatarStatus,
            ImageSource? avatarSource,
            bool isCurrentUser)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            ProviderDisplayName = providerDisplayName ?? string.Empty;
            Initials = string.IsNullOrWhiteSpace(initials) ? "??" : initials;
            StatusLabel = statusLabel ?? string.Empty;
            AvatarStatus = avatarStatus;
            AvatarSource = avatarSource;
            IsCurrentUser = isCurrentUser;

            Id = user.Id;
            RawId = user.RawId;
            ProviderKey = user.ProviderKey;
            DisplayName = user.DisplayName;
            Email = user.Email;
            Department = user.Department;
            Title = user.Title;
            Status = user.Status;
            HasEmail = !string.IsNullOrWhiteSpace(Email);
            HasDepartment = !string.IsNullOrWhiteSpace(Department);
            HasTitle = !string.IsNullOrWhiteSpace(Title);
            HasStatusLabel = !string.IsNullOrWhiteSpace(StatusLabel);
        }

        public IFlowUser User { get; }
        public string Id { get; }
        public string RawId { get; }
        public string ProviderKey { get; }
        public string ProviderDisplayName { get; }
        public string DisplayName { get; }
        public string? Email { get; }
        public string? Department { get; }
        public string? Title { get; }
        public FlowUserStatus Status { get; }
        public string StatusLabel { get; }
        public DaisyStatus AvatarStatus { get; }
        public ImageSource? AvatarSource { get; }
        public string Initials { get; }
        public bool IsCurrentUser { get; }
        public bool HasEmail { get; }
        public bool HasDepartment { get; }
        public bool HasTitle { get; }
        public bool HasStatusLabel { get; }
        public bool IsAvatarPlaceholder => AvatarSource == null;

        public bool Matches(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var comparison = StringComparison.OrdinalIgnoreCase;
            return DisplayName.Contains(filter, comparison)
                   || (Email?.Contains(filter, comparison) ?? false)
                   || (Department?.Contains(filter, comparison) ?? false)
                   || (Title?.Contains(filter, comparison) ?? false)
                   || ProviderKey.Contains(filter, comparison)
                   || Id.Contains(filter, comparison);
        }
    }

    internal sealed class LocalUserOption
    {
        public LocalUserOption(IFlowUser user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.RawId : user.DisplayName;
        }

        public IFlowUser User { get; }
        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }
}
