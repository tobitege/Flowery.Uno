using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Helpers;
using Flowery.Localization;
using Flowery.Theming;
using Flowery.Uno.Gallery.Examples;
using Flowery.Uno.Gallery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Storage;

namespace Flowery.Uno.Gallery
{
    public sealed partial class MainView : UserControl
    {
        private const double SidebarWidth = 250;
        private const double MinContentWidth = 400;
        private const double MaxSidebarWidthPercent = 0.35;
        private const double HeaderCollapseScrollThreshold = 20;
        // Central diagnostics log can grow unbounded during log storms; keep disabled for now.
        // Runtime-readonly flag to avoid CS0162 warnings.
        private static readonly bool EnableCentralDiagnosticsLog = false;

        private readonly Dictionary<string, Func<FrameworkElement>> _categoryFactories;
        private readonly Dictionary<string, SidebarCategory> _categoriesByName;
        private FrameworkElement? _activeCategoryContent;
        private HomePage? _homePage;
        private readonly StringBuilder _diagnosticsLog = new();
        private bool _isHeaderCollapsed;
        private bool _isLandscape;
        private readonly bool _isMobilePlatform;
        private ScrollViewer? _currentScrollViewer;
        private readonly RectangleGeometry _headerClip = new();
        private bool _suppressScrollEvents;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _scrollSuppressTimer;

        public MainView()
        {
            // Ensure Gallery localization is initialized before the sidebar resolves strings.
            _ = GalleryLocalization.Instance;
            FloweryLocalization.RefreshBindings();

            InitializeComponent();
            if (EnableCentralDiagnosticsLog)
            {
                GalleryDiagnostics.MessageLogged += OnDiagnosticsMessageLogged;
                FloweryDiagnostics.MessageLogged += OnDiagnosticsMessageLogged;
            }
            DaisyBaseContentControl.GlobalNeumorphicIntensity = 0.25;

            if (HeaderBorder != null)
            {
                HeaderBorder.Clip = _headerClip;
                HeaderBorder.SizeChanged += OnHeaderSizeChanged;
                UpdateHeaderClip();
            }

            _isMobilePlatform = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

            // Show platform hint in header
            if (MitLicenseText != null)
            {
                var platform = GetPlatformName();
                MitLicenseText.Text = $" - 2025 - MIT licensed ({platform})";
            }

            UpdateFlowDirection();
            FloweryLocalization.CultureChanged += OnCultureChanged;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;

            _categoryFactories = new Dictionary<string, Func<FrameworkElement>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Sidebar_Home"] = CreateHomePage,
                ["Sidebar_Actions"] = GetOrCreateActionsExamples,
                ["Sidebar_Cards"] = () => new CardsExamples(),
                ["Sidebar_Carousel"] = () => new CarouselExamples(),
#if __WASM__ //&& (HAS_UNO_SKIA || __UNO_SKIA__ || __SKIA__ || HAS_UNO_SKIA_WEBASSEMBLY_BROWSER || __UNO_SKIA_WEBASSEMBLY_BROWSER__)
                ["Sidebar_CarouselGL"] = () => new CarouselGlExamples(),
#if FLOWERY_GL_TRANSITIONS
                ["Sidebar_CarouselGLTransitions"] = () => new CarouselGlTransitionsExamples(),
#endif
#endif
                ["Sidebar_Patterns"] = () => new PatternsExamples(),
                ["Sidebar_DataDisplay"] = () => new DataDisplayExamples(),
                ["Sidebar_DateDisplay"] = () => new DateDisplayExamples(),
                ["Sidebar_DataInput"] = () => new DataInputExamples(),
                ["Sidebar_Divider"] = () => new DividerExamples(),
                ["Sidebar_Feedback"] = () => new FeedbackExamples(),
                ["Sidebar_Layout"] = () => new LayoutExamples(),
                ["Sidebar_Navigation"] = () => new NavigationExamples(),
                ["Sidebar_Theming"] = () => new ThemingExamples(),
                ["Sidebar_LightweightStyling"] = () => new StylingExamples(),
                ["Sidebar_ProductThemes"] = () => new ProductThemesExamples(),
                ["Sidebar_Effects"] = () => new EffectsExamples(),
                ["Sidebar_Scaling"] = () => new ScalingExamples(),
                ["Sidebar_CustomControls"] = () => new CustomControls(),
                ["Sidebar_FlowKanban"] = () => new FlowKanbanExample(),
                ["Sidebar_ColorPicker"] = () => new ColorPickerExamples(),
                ["Sidebar_Showcase"] = () => new ShowcaseExamples(),
            };

            var categories = GallerySidebarData.CreateCategories();
            _categoriesByName = categories.ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);

            if (ComponentSidebar != null)
            {
                ComponentSidebar.Categories = categories;
                ComponentSidebar.AvailableLanguages = GallerySidebarData.CreateLanguages();
            }

            FlowerySizeManager.SizeChanged += OnGlobalSizeChanged;

            if (ComponentSidebar != null)
            {
                // Sidebar state restoration now triggers IteSelected event
                // which this view handles via ComponentSidebar_ItemSelected.
                // We just need to check if no default was selected.
                var (lastItemId, _) = ComponentSidebar.GetLastViewedItem();
                if (lastItemId != null)
                    return;
            }

            NavigateToCategory("Sidebar_Home");
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            SuppressScrollEvents();
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateFlowDirection();
                if (_activeCategoryContent != null && CategoryTitle != null && CategoryTitle.Tag is string key)
                {
                    CategoryTitle.Text = FloweryLocalization.GetString(key);
                }
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            ApplyTheme();

            UpdateResponsiveLayout(ActualWidth);

            if (_isMobilePlatform)
            {
                _isLandscape = ActualWidth > ActualHeight;

                if (!_isLandscape)
                    SetHeaderCollapsed(false);

                if (_activeCategoryContent != null && _currentScrollViewer == null)
                    AttachScrollHandler(_activeCategoryContent);
            }

            ApplyGlobalSizeToControls(this, FlowerySizeManager.CurrentSize);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            if (EnableCentralDiagnosticsLog)
            {
                GalleryDiagnostics.MessageLogged -= OnDiagnosticsMessageLogged;
                FloweryDiagnostics.MessageLogged -= OnDiagnosticsMessageLogged;
            }
        }

        private void OnThemeChanged(object? sender, string themeName) => ApplyTheme();

        private void ApplyTheme()
        {
            if (RootGrid != null)
                RootGrid.Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");

            if (CategoryTitleBar != null)
            {
                CategoryTitleBar.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                CategoryTitleBar.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            }

            if (CategoryTitle != null)
            {
                CategoryTitle.Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            }

            // Update chevron fills for category title
            var primaryBrush = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            if (primaryBrush != null && CategoryChevrons?.Child is StackPanel chevronPanel)
            {
                foreach (var child in chevronPanel.Children)
                {
                    if (child is Microsoft.UI.Xaml.Shapes.Path path)
                    {
                        path.Fill = primaryBrush;
                    }
                }
            }
        }

        private void OnDiagnosticsMessageLogged(object? sender, string message)
        {
            if (EnableCentralDiagnosticsLog)
                AppendDiagnosticsMessage(message);
        }

        private void OnHeaderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            UpdateHeaderClip();
        }

        private void UpdateHeaderClip()
        {
            if (HeaderBorder == null)
                return;

            var width = HeaderBorder.ActualWidth;
            var height = HeaderBorder.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            _headerClip.Rect = new Rect(0, 0, width, height);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResponsiveLayout(e.NewSize.Width);

            if (!_isMobilePlatform)
                return;

            var wasLandscape = _isLandscape;
            _isLandscape = e.NewSize.Width > e.NewSize.Height;

            if (wasLandscape && !_isLandscape && _isHeaderCollapsed)
            {
                SetHeaderCollapsed(false);
            }
            else if (!_isLandscape && _isHeaderCollapsed)
            {
                SetHeaderCollapsed(false);
            }
        }

        private void UpdateResponsiveLayout(double width)
        {
            if (MainSplitView == null || HamburgerButton == null)
                return;

            var contentWidthIfInline = width - SidebarWidth;
            var sidebarPercent = width > 0 ? SidebarWidth / width : 0;

            var shouldCollapse = contentWidthIfInline < MinContentWidth || sidebarPercent > MaxSidebarWidthPercent;

            MainSplitView.DisplayMode = shouldCollapse ? SplitViewDisplayMode.Overlay : SplitViewDisplayMode.Inline;
            HamburgerButton.Visibility = shouldCollapse ? Visibility.Visible : Visibility.Collapsed;
            MainSplitView.IsPaneOpen = !shouldCollapse;
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainSplitView != null)
            {
                MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
            }
        }

        private HomePage CreateHomePage()
        {
            var homePage = new HomePage();
            _homePage = homePage;
            homePage.BrowseComponentsRequested += OnBrowseComponentsRequested;
            homePage.OpenGitHubRequested += OnOpenGitHubRequested;
            homePage.OpenUnoRepoRequested += OnOpenUnoRepoRequested;
            if (_diagnosticsLog.Length > 0)
            {
                homePage.SetDiagnosticsText(_diagnosticsLog.ToString());
            }
            return homePage;
        }

        private ActionsExamples GetOrCreateActionsExamples()
        {
            var actionsExamples = new ActionsExamples();
            actionsExamples.OpenModalRequested += OnOpenModalRequested;
            actionsExamples.OpenModalWithRadiiRequested += OnOpenModalWithRadiiRequested;
            return actionsExamples;
        }

        private void OnBrowseComponentsRequested(object? sender, EventArgs e)
        {
            NavigateToCategory("Sidebar_Actions", "button");
        }

        private void OnOpenGitHubRequested(object? sender, EventArgs e)
        {
            OpenGitHubLink();
        }

        private void OnOpenUnoRepoRequested(object? sender, EventArgs e)
        {
            OpenUnoRepoLink();
        }

        private void OnOpenModalRequested(object? sender, EventArgs e)
        {
            if (DemoModal == null)
                return;

            DemoModal.TopLeftRadius = 16;
            DemoModal.TopRightRadius = 16;
            DemoModal.BottomLeftRadius = 16;
            DemoModal.BottomRightRadius = 16;
            SetModalTitle("Hello!");
            DemoModal.IsOpen = true;
        }

        private void OnOpenModalWithRadiiRequested(object? sender, ModalRadiiEventArgs e)
        {
            if (DemoModal == null)
                return;

            DemoModal.TopLeftRadius = e.TopLeft;
            DemoModal.TopRightRadius = e.TopRight;
            DemoModal.BottomLeftRadius = e.BottomLeft;
            DemoModal.BottomRightRadius = e.BottomRight;
            SetModalTitle(e.Title);
            DemoModal.IsOpen = true;
        }

        private void SetModalTitle(string title)
        {
            if (ModalTitle != null)
                ModalTitle.Text = title;
        }

        private void NavigateToCategory(string tabHeader, string? sectionId = null)
        {
            if (MainContentHost == null)
                return;

            if (CategoryTitle != null)
            {
                CategoryTitle.Text = FloweryLocalization.GetString(tabHeader);
                CategoryTitle.Tag = tabHeader;
            }

            if (CategoryTitleBar != null)
                CategoryTitleBar.Visibility = tabHeader == "Sidebar_Home" ? Visibility.Collapsed : Visibility.Visible;

            var newContent = GetOrCreateCategoryContent(tabHeader);
            var contentChanged = !ReferenceEquals(_activeCategoryContent, newContent);

            if (contentChanged)
            {
                if (_activeCategoryContent != null)
                {
                    MainContentHost.Children.Remove(_activeCategoryContent);
                }

                if (!MainContentHost.Children.Contains(newContent))
                    MainContentHost.Children.Add(newContent);

                newContent.Visibility = Visibility.Visible;
                _activeCategoryContent = newContent;
            }

            if (sectionId != null && newContent is IScrollableExample scrollable)
            {
                if (contentChanged)
                {
                    DispatcherQueue.TryEnqueue(() => scrollable.ScrollToSection(sectionId));
                }
                else
                {
                    scrollable.ScrollToSection(sectionId);
                }
            }

            if (_isMobilePlatform && contentChanged)
            {
                DispatcherQueue.TryEnqueue(() => AttachScrollHandler(newContent));
            }

            if (contentChanged)
            {
                // Hook into Loaded event to ensure visual tree is fully realized
                // before applying global size (DispatcherQueue timing is unreliable)
                void OnContentLoaded(object s, RoutedEventArgs args)
                {
                    newContent.Loaded -= OnContentLoaded;
                    ApplyGlobalSizeToControls(newContent, FlowerySizeManager.CurrentSize);
                }

                if (newContent.IsLoaded)
                {
                    // Already loaded (cached view), apply immediately
                    ApplyGlobalSizeToControls(newContent, FlowerySizeManager.CurrentSize);
                }
                else
                {
                    newContent.Loaded += OnContentLoaded;
                }
            }
        }

        private FrameworkElement GetOrCreateCategoryContent(string tabHeader)
        {
            FrameworkElement newContent;
            if (_categoryFactories.TryGetValue(tabHeader, out var factory))
            {
                try
                {
                    newContent = factory();
                }
                catch (Exception ex)
                {
                    ReportCategoryFailure(tabHeader, ex);
                    if (_categoriesByName.TryGetValue(tabHeader, out var category))
                    {
                        newContent = new CategoryPlaceholderView(category);
                    }
                    else
                    {
                        newContent = new CategoryPlaceholderView(new SidebarCategory { Name = tabHeader });
                    }
                }
            }
            else if (_categoriesByName.TryGetValue(tabHeader, out var category))
            {
                newContent = new CategoryPlaceholderView(category);
            }
            else
            {
                newContent = new CategoryPlaceholderView(new SidebarCategory { Name = tabHeader });
            }

            return newContent;
        }

        private void ReportCategoryFailure(string tabHeader, Exception ex)
        {
            var message = FormatCategoryFailureMessage(tabHeader, ex);
            AppendDiagnosticsMessage(message);
            _ = LogCategoryFailureAsync(message);
        }

        private void AppendDiagnosticsMessage(string message)
        {
            if (!EnableCentralDiagnosticsLog)
                return;
            if (string.IsNullOrWhiteSpace(message))
                return;

            _diagnosticsLog.AppendLine(message);
            _homePage?.AppendDiagnostics(message);
        }

        private static string FormatCategoryFailureMessage(string tabHeader, Exception ex)
        {
            return $"{DateTimeOffset.Now:O} [Gallery] Category '{tabHeader}' failed: {ex}";
        }

        private static async Task LogCategoryFailureAsync(string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(message);
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync("flowery-gallery.log", CreationCollisionOption.OpenIfExists);
                await FileIO.AppendTextAsync(file, message + Environment.NewLine);
            }
            catch
            {
            }
        }

        private void AttachScrollHandler(FrameworkElement content)
        {
            if (_currentScrollViewer != null)
            {
                _currentScrollViewer.ViewChanged -= OnContentViewChanged;
                _currentScrollViewer = null;
            }

            var scrollViewer = FindFirstScrollViewer(content);
            if (scrollViewer != null)
            {
                _currentScrollViewer = scrollViewer;
                _currentScrollViewer.ViewChanged += OnContentViewChanged;
            }
        }

        private void OnContentViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_suppressScrollEvents)
                return;

            if (!_isLandscape)
                return;

            if (sender is not ScrollViewer scrollViewer)
                return;

            var shouldCollapse = scrollViewer.VerticalOffset > HeaderCollapseScrollThreshold;
            if (shouldCollapse != _isHeaderCollapsed)
            {
                SetHeaderCollapsed(shouldCollapse);
            }
        }

        private void SuppressScrollEvents()
        {
            _suppressScrollEvents = true;

            var queue = DispatcherQueue;
            if (queue == null)
                return;

            if (_scrollSuppressTimer == null)
            {
                _scrollSuppressTimer = queue.CreateTimer();
                _scrollSuppressTimer.Interval = TimeSpan.FromMilliseconds(1);
                _scrollSuppressTimer.Tick += OnScrollSuppressTimerTick;
            }

            _scrollSuppressTimer.Stop();
            _scrollSuppressTimer.Start();
        }

        private void OnScrollSuppressTimerTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            _suppressScrollEvents = false;
        }

        private void SetHeaderCollapsed(bool collapsed)
        {
            _isHeaderCollapsed = collapsed;

            if (HeaderPanel == null || SubtitleRow == null || HeaderTitle == null)
                return;

            if (collapsed)
            {
                SubtitleRow.Visibility = Visibility.Collapsed;
                HeaderTitle.FontSize = 16;
                if (HeaderContentGrid != null)
                    HeaderContentGrid.Margin = new Thickness(12, 6, 16, 4);
                if (HeaderOrbRight != null)
                    HeaderOrbRight.Visibility = Visibility.Collapsed;
                if (HeaderOrbLeft != null)
                    HeaderOrbLeft.Visibility = Visibility.Collapsed;
                if (HeaderAccentBar != null)
                    HeaderAccentBar.Height = 2;
                if (CategoryTitleBar != null)
                    CategoryTitleBar.Padding = new Thickness(16, 2, 16, 6);
                if (CategoryTitle != null)
                    CategoryTitle.FontSize = 16;
                if (CategoryChevrons != null)
                    CategoryChevrons.Visibility = Visibility.Collapsed;
            }
            else
            {
                SubtitleRow.Visibility = Visibility.Visible;
                HeaderTitle.FontSize = 28;
                if (HeaderContentGrid != null)
                    HeaderContentGrid.Margin = new Thickness(12, 16, 16, 8);
                if (HeaderOrbRight != null)
                    HeaderOrbRight.Visibility = Visibility.Visible;
                if (HeaderOrbLeft != null)
                    HeaderOrbLeft.Visibility = Visibility.Visible;
                if (HeaderAccentBar != null)
                    HeaderAccentBar.Height = 3;
                if (CategoryTitleBar != null)
                    CategoryTitleBar.Padding = new Thickness(24, 4, 24, 20);
                if (CategoryTitle != null)
                    CategoryTitle.FontSize = 22;
                if (CategoryChevrons != null)
                    CategoryChevrons.Visibility = Visibility.Visible;
            }
        }

        private void UpdateFlowDirection()
        {
            FlowDirection = FloweryLocalization.Instance.IsRtl
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        public void ComponentSidebar_ItemSelected(object sender, SidebarItemSelectedEventArgs e)
        {
            NavigateToCategory(e.Item.TabHeader, e.Item.Id);
            if (MainSplitView != null && MainSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                MainSplitView.IsPaneOpen = false;
            }
        }

        public void CloseModalBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            if (DemoModal != null)
                DemoModal.IsOpen = false;
        }

        public void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            OpenGitHubLink();
        }

        public void CaptureAllScreenshots_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
        }

        private static void OpenGitHubLink()
        {
            // Process.Start is not supported on iOS/tvOS
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
                return;

            const string url = "https://www.github.com/tobitege";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private static void OpenUnoRepoLink()
        {
            // Process.Start is not supported on iOS/tvOS
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
                return;

            const string url = "https://github.com/unoplatform/uno";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private static string GetPlatformName()
        {
#if __WASM__ || HAS_UNO_WASM
            return "Browser";
#elif __ANDROID__
            return "Android";
#elif __IOS__
            return "iOS";
#elif DESKTOP || __DESKTOP__ || __UNO_SKIA__ || HAS_UNO_SKIA || __SKIA__
            // Skia Desktop (includes Windows Skia, Linux, macOS)
            if (OperatingSystem.IsMacOS())
                return "macOS";
            if (OperatingSystem.IsLinux())
                return "Linux";
            return "Desktop";
#elif WINDOWS
            // Native WinUI on Windows
            return "Windows";
#else
            return "Unknown";
#endif
        }

        private void OnGlobalSizeChanged(object? sender, DaisySize newSize)
        {
            SuppressScrollEvents();
            ApplyGlobalSizeToControls(this, newSize);
            ApplySizeToGalleryElements(newSize);
        }

        private void ApplySizeToGalleryElements(DaisySize size)
        {
            if (CategoryTitle != null)
            {
                CategoryTitle.FontSize = size switch
                {
                    DaisySize.ExtraSmall => 16,
                    DaisySize.Small => 18,
                    DaisySize.Medium => 22,
                    DaisySize.Large => 26,
                    DaisySize.ExtraLarge => 30,
                    _ => 22
                };
            }

            if (CategoryTitleBar != null)
            {
                var padding = size switch
                {
                    DaisySize.ExtraSmall => new Thickness(16, 2, 16, 8),
                    DaisySize.Small => new Thickness(20, 3, 20, 14),
                    DaisySize.Medium => new Thickness(24, 4, 24, 20),
                    DaisySize.Large => new Thickness(28, 6, 28, 24),
                    DaisySize.ExtraLarge => new Thickness(32, 8, 32, 28),
                    _ => new Thickness(24, 4, 24, 20)
                };
                CategoryTitleBar.Padding = padding;
            }

            if (CategoryChevrons != null)
            {
                var chevronSize = size switch
                {
                    DaisySize.ExtraSmall => (Width: 16.0, Height: 10.0),
                    DaisySize.Small => (Width: 20.0, Height: 12.0),
                    DaisySize.Medium => (Width: 24.0, Height: 14.0),
                    DaisySize.Large => (Width: 28.0, Height: 16.0),
                    DaisySize.ExtraLarge => (Width: 32.0, Height: 18.0),
                    _ => (Width: 24.0, Height: 14.0)
                };
                CategoryChevrons.Width = chevronSize.Width;
                CategoryChevrons.Height = chevronSize.Height;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers",
            Justification = "DaisyUI controls that support Size property are always preserved. Method handles missing properties gracefully.")]
        private static void ApplyGlobalSizeToControls(DependencyObject root, DaisySize size)
        {
            foreach (var element in EnumerateVisualTree(root))
            {
                if (element is Control control)
                {
                    if (ShouldIgnoreGlobalSize(control))
                        continue;

                    var sizeProperty = control.GetType().GetProperty("Size", BindingFlags.Public | BindingFlags.Instance);
                    if (sizeProperty != null && sizeProperty.PropertyType == typeof(DaisySize) && sizeProperty.CanWrite)
                    {
                        try
                        {
                            sizeProperty.SetValue(control, size);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private static bool ShouldIgnoreGlobalSize(DependencyObject control)
        {
            DependencyObject? current = control;
            while (current != null)
            {
                if (FlowerySizeManager.GetIgnoreGlobalSize(current))
                    return true;

                // Fallback: Check if it's a FrameworkElement's Parent property if VisualTreeHelper fails
                var next = VisualTreeHelper.GetParent(current);
                if (next == null && current is FrameworkElement fe)
                {
                    next = fe.Parent;
                }
                current = next;
            }

            return false;
        }

        /// <summary>
        /// Iteratively enumerates the visual tree to avoid StackOverflow.
        /// </summary>
        private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var element = queue.Dequeue();
                yield return element;

                int count = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    if (child != null)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        private static ScrollViewer? FindFirstScrollViewer(DependencyObject root)
        {
            foreach (var element in EnumerateVisualTree(root))
            {
                if (element is ScrollViewer viewer)
                    return viewer;
            }

            return null;
        }
    }
}
