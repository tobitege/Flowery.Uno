using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Pagination item that represents a single page button in the pagination control.
    /// </summary>
    public partial class DaisyPaginationItem : Button
    {
        public DaisyPaginationItem()
        {
            DefaultStyleKey = typeof(Button);
        }

        #region PageNumber
        public static readonly DependencyProperty PageNumberProperty =
            DependencyProperty.Register(
                nameof(PageNumber),
                typeof(int?),
                typeof(DaisyPaginationItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the page number this item represents. Null for non-numeric items like prev/next.
        /// </summary>
        public int? PageNumber
        {
            get => (int?)GetValue(PageNumberProperty);
            set => SetValue(PageNumberProperty, value);
        }
        #endregion

        #region IsEllipsis
        public static readonly DependencyProperty IsEllipsisProperty =
            DependencyProperty.Register(
                nameof(IsEllipsis),
                typeof(bool),
                typeof(DaisyPaginationItem),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether this item is an ellipsis placeholder.
        /// </summary>
        public bool IsEllipsis
        {
            get => (bool)GetValue(IsEllipsisProperty);
            set => SetValue(IsEllipsisProperty, value);
        }
        #endregion
    }

    /// <summary>
    /// A pagination control styled after DaisyUI's Pagination component.
    /// Uses DaisyJoin internally for joined button styling.
    /// Supports smart truncation with ellipsis for large page counts.
    /// </summary>
    public partial class DaisyPagination : DaisyBaseContentControl
    {
        private DaisyJoin? _join;
        private readonly List<DaisyPaginationItem> _allButtons = [];
        private bool _isLoaded;

        public DaisyPagination()
        {
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            _isLoaded = true;

            if (_join != null)
            {
                ApplyAll();
                return;
            }

            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            _isLoaded = false;
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

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Left:
                case VirtualKey.Up:
                    CurrentPage = Math.Max(1, CurrentPage - 1);
                    break;
                case VirtualKey.Right:
                case VirtualKey.Down:
                    CurrentPage = Math.Min(TotalPages, CurrentPage + 1);
                    break;
                case VirtualKey.Home:
                    CurrentPage = 1;
                    break;
                case VirtualKey.End:
                    CurrentPage = TotalPages;
                    break;
                case VirtualKey.PageUp:
                    CurrentPage = Math.Max(1, CurrentPage - JumpStep);
                    break;
                case VirtualKey.PageDown:
                    CurrentPage = Math.Min(TotalPages, CurrentPage + JumpStep);
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        #region CurrentPage
        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(
                nameof(CurrentPage),
                typeof(int),
                typeof(DaisyPagination),
                new PropertyMetadata(1, OnCurrentPageChanged));

        /// <summary>
        /// Gets or sets the currently selected page number.
        /// </summary>
        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, Math.Max(1, Math.Min(value, TotalPages)));
        }

        private static void OnCurrentPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPagination pagination)
            {
                // Clamp to valid range
                var newValue = (int)e.NewValue;
                var clamped = Math.Max(1, Math.Min(newValue, pagination.TotalPages));
                if (clamped != newValue)
                {
                    pagination.CurrentPage = clamped;
                    return;
                }

                if (pagination._isLoaded)
                    pagination.RebuildPages();

                pagination.PageChanged?.Invoke(pagination, pagination.CurrentPage);
                pagination.UpdateAutomationProperties();
            }
        }
        #endregion

        #region TotalPages
        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register(
                nameof(TotalPages),
                typeof(int),
                typeof(DaisyPagination),
                new PropertyMetadata(1, OnTotalPagesChanged));

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            set => SetValue(TotalPagesProperty, Math.Max(1, value));
        }

        private static void OnTotalPagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPagination pagination)
            {
                // Ensure CurrentPage is within valid range
                if (pagination.CurrentPage > pagination.TotalPages)
                    pagination.CurrentPage = pagination.TotalPages;

                if (pagination._isLoaded)
                    pagination.RebuildPages();

                pagination.UpdateAutomationProperties();
            }
        }
        #endregion

        #region MaxVisiblePages
        public static readonly DependencyProperty MaxVisiblePagesProperty =
            DependencyProperty.Register(
                nameof(MaxVisiblePages),
                typeof(int),
                typeof(DaisyPagination),
                new PropertyMetadata(7, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the maximum number of page buttons to display (excluding navigation buttons).
        /// When TotalPages exceeds this, ellipsis will be shown.
        /// </summary>
        public int MaxVisiblePages
        {
            get => (int)GetValue(MaxVisiblePagesProperty);
            set => SetValue(MaxVisiblePagesProperty, Math.Max(5, value)); // Minimum 5: first, ellipsis, current, ellipsis, last
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyPagination),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of pagination buttons.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyPagination),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        #endregion

        #region ShowPrevNext
        public static readonly DependencyProperty ShowPrevNextProperty =
            DependencyProperty.Register(
                nameof(ShowPrevNext),
                typeof(bool),
                typeof(DaisyPagination),
                new PropertyMetadata(true, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether to show Previous/Next buttons (single chevron).
        /// </summary>
        public bool ShowPrevNext
        {
            get => (bool)GetValue(ShowPrevNextProperty);
            set => SetValue(ShowPrevNextProperty, value);
        }
        #endregion

        #region ShowFirstLast
        public static readonly DependencyProperty ShowFirstLastProperty =
            DependencyProperty.Register(
                nameof(ShowFirstLast),
                typeof(bool),
                typeof(DaisyPagination),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether to show First/Last page buttons.
        /// </summary>
        public bool ShowFirstLast
        {
            get => (bool)GetValue(ShowFirstLastProperty);
            set => SetValue(ShowFirstLastProperty, value);
        }
        #endregion

        #region ShowJumpButtons
        public static readonly DependencyProperty ShowJumpButtonsProperty =
            DependencyProperty.Register(
                nameof(ShowJumpButtons),
                typeof(bool),
                typeof(DaisyPagination),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether to show double-chevron jump buttons (jump multiple pages).
        /// </summary>
        public bool ShowJumpButtons
        {
            get => (bool)GetValue(ShowJumpButtonsProperty);
            set => SetValue(ShowJumpButtonsProperty, value);
        }
        #endregion

        #region Accessibility
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyPagination),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPagination pagination)
            {
                pagination.UpdateAutomationProperties();
            }
        }
        #endregion

        #region JumpStep
        public static readonly DependencyProperty JumpStepProperty =
            DependencyProperty.Register(
                nameof(JumpStep),
                typeof(int),
                typeof(DaisyPagination),
                new PropertyMetadata(10));

        /// <summary>
        /// Gets or sets how many pages to jump when using jump buttons.
        /// </summary>
        public int JumpStep
        {
            get => (int)GetValue(JumpStepProperty);
            set => SetValue(JumpStepProperty, Math.Max(2, value));
        }
        #endregion

        #region EllipsisText
        public static readonly DependencyProperty EllipsisTextProperty =
            DependencyProperty.Register(
                nameof(EllipsisText),
                typeof(string),
                typeof(DaisyPagination),
                new PropertyMetadata("…", OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the text displayed for ellipsis.
        /// </summary>
        public string EllipsisText
        {
            get => (string)GetValue(EllipsisTextProperty);
            set => SetValue(EllipsisTextProperty, value);
        }
        #endregion

        /// <summary>
        /// Event raised when the current page changes.
        /// </summary>
        public event EventHandler<int>? PageChanged;

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPagination pagination && pagination._join != null)
            {
                pagination._join.Orientation = pagination.Orientation;
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPagination pagination && pagination._isLoaded)
            {
                pagination.RebuildPages();
            }
        }

        private void BuildVisualTree()
        {
            RebuildPages();
        }

        private void RebuildPages()
        {
            _allButtons.Clear();

            // Create a fresh DaisyJoin each time we rebuild
            _join = new DaisyJoin
            {
                Orientation = Orientation
            };

            // Build the panel with all buttons
            // Order by magnitude: First/Last (outermost) → Jump (middle) → Prev/Next (innermost)
            // Layout: |← « ‹ [pages] › » →|
            var panel = new StackPanel { Orientation = Orientation };

            // First page button (|←) - outermost left
            if (ShowFirstLast)
            {
                var firstButton = CreateIconButton("DaisyIconPageFirst");
                ConfigureButton(firstButton, "First page");
                firstButton.Click += (s, e) => CurrentPage = 1;
                _allButtons.Add(firstButton);
                panel.Children.Add(firstButton);
            }

            // Jump backward button («) - middle left
            if (ShowJumpButtons)
            {
                var jumpBackButton = CreateIconButton("DaisyIconChevronDoubleLeft");
                ConfigureButton(jumpBackButton, "Jump back");
                jumpBackButton.Click += (s, e) => CurrentPage = CalculatePreviousJumpTarget();
                _allButtons.Add(jumpBackButton);
                panel.Children.Add(jumpBackButton);
            }

            // Previous button (‹) - innermost left, adjacent to pages
            if (ShowPrevNext)
            {
                var prevButton = CreateIconButton("DaisyIconChevronLeft");
                ConfigureButton(prevButton, "Previous page");
                prevButton.Click += (s, e) =>
                {
                    if (CurrentPage > 1)
                        CurrentPage--;
                };
                _allButtons.Add(prevButton);
                panel.Children.Add(prevButton);
            }

            // Page number buttons with smart truncation
            AddPageButtons(panel);

            // Next button (›) - innermost right, adjacent to pages
            if (ShowPrevNext)
            {
                var nextButton = CreateIconButton("DaisyIconChevronRight");
                ConfigureButton(nextButton, "Next page");
                nextButton.Click += (s, e) =>
                {
                    if (CurrentPage < TotalPages)
                        CurrentPage++;
                };
                _allButtons.Add(nextButton);
                panel.Children.Add(nextButton);
            }

            // Jump forward button (») - middle right
            if (ShowJumpButtons)
            {
                var jumpForwardButton = CreateIconButton("DaisyIconChevronDoubleRight");
                ConfigureButton(jumpForwardButton, "Jump forward");
                jumpForwardButton.Click += (s, e) => CurrentPage = CalculateNextJumpTarget();
                _allButtons.Add(jumpForwardButton);
                panel.Children.Add(jumpForwardButton);
            }

            // Last page button (→|) - outermost right
            if (ShowFirstLast)
            {
                var lastButton = CreateIconButton("DaisyIconPageLast");
                ConfigureButton(lastButton, "Last page");
                lastButton.Click += (s, e) => CurrentPage = TotalPages;
                _allButtons.Add(lastButton);
                panel.Children.Add(lastButton);
            }

            // Set up the join
            _join.Content = panel;
            Content = _join;

            // Apply sizing to all buttons
            ApplySizing();

            // Set up active index - needs to be done after DaisyJoin loads
            _join.Loaded += (s, e) =>
            {
                UpdateActiveIndex();
            };
        }

        /// <summary>
        /// Calculates the previous jump target - the previous multiple of JumpStep.
        /// E.g., JumpStep=10: from 53 → 50, from 50 → 40, from 47 → 40
        /// </summary>
        private int CalculatePreviousJumpTarget()
        {
            // If already on a multiple, go to the previous multiple
            // Otherwise go to the floor multiple
            int currentMultiple = (CurrentPage / JumpStep) * JumpStep;
            
            if (currentMultiple == CurrentPage)
            {
                // Already on a multiple, go to previous one
                return Math.Max(1, currentMultiple - JumpStep);
            }
            else
            {
                // Go to the floor (previous) multiple
                return Math.Max(1, currentMultiple);
            }
        }

        /// <summary>
        /// Calculates the next jump target - the next multiple of JumpStep.
        /// E.g., JumpStep=10: from 53 → 60, from 50 → 60, from 47 → 50
        /// </summary>
        private int CalculateNextJumpTarget()
        {
            // Go to the ceiling (next) multiple
            int nextMultiple = ((CurrentPage / JumpStep) + 1) * JumpStep;
            return Math.Min(TotalPages, nextMultiple);
        }


        /// <summary>
        /// Adds page buttons with smart truncation/ellipsis for large page counts.
        /// </summary>
        private void AddPageButtons(StackPanel panel)
        {
            var pagesToShow = CalculateVisiblePages();

            int? lastPage = null;
            foreach (var pageNum in pagesToShow)
            {
                // Check if we need an ellipsis
                if (lastPage.HasValue && pageNum - lastPage.Value > 1)
                {
                    var ellipsisButton = CreateEllipsisButton();
                    _allButtons.Add(ellipsisButton);
                    panel.Children.Add(ellipsisButton);
                }

                var pageButton = CreatePageButton(pageNum.ToString(), pageNum);
                ConfigureButton(pageButton, $"Page {pageNum}");
                int capturedPageNum = pageNum; // Capture for closure
                pageButton.Click += (s, e) => CurrentPage = capturedPageNum;
                _allButtons.Add(pageButton);
                panel.Children.Add(pageButton);

                lastPage = pageNum;
            }
        }

        /// <summary>
        /// Calculates which page numbers should be visible based on current page and max visible pages.
        /// Returns a sorted list of page numbers to display.
        /// </summary>
        private List<int> CalculateVisiblePages()
        {
            var result = new List<int>();

            // If total pages fits within max visible, show all
            if (TotalPages <= MaxVisiblePages)
            {
                for (int i = 1; i <= TotalPages; i++)
                    result.Add(i);
                return result;
            }

            // Always show first page
            result.Add(1);

            // Calculate the range around current page
            // Reserve 2 slots for first and last page, remaining slots for middle section
            int middleSlots = MaxVisiblePages - 2;
            int halfMiddle = middleSlots / 2;

            int rangeStart = CurrentPage - halfMiddle;
            int rangeEnd = CurrentPage + halfMiddle;

            // Adjust if we're near the start
            if (rangeStart <= 2)
            {
                rangeStart = 2;
                rangeEnd = rangeStart + middleSlots - 1;
            }

            // Adjust if we're near the end
            if (rangeEnd >= TotalPages - 1)
            {
                rangeEnd = TotalPages - 1;
                rangeStart = rangeEnd - middleSlots + 1;
            }

            // Clamp to valid range
            rangeStart = Math.Max(2, rangeStart);
            rangeEnd = Math.Min(TotalPages - 1, rangeEnd);

            // Add middle pages
            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                if (i > 1 && i < TotalPages)
                    result.Add(i);
            }

            // Always show last page (if different from first)
            if (TotalPages > 1)
                result.Add(TotalPages);

            // Sort and remove duplicates
            result.Sort();
            return result;
        }

        private static DaisyPaginationItem CreatePageButton(string content, int? pageNumber) => new()
        {
            Content = content,
            PageNumber = pageNumber,
            IsEllipsis = false,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        private DaisyPaginationItem CreateEllipsisButton()
        {
            var button = new DaisyPaginationItem
            {
                Content = EllipsisText,
                PageNumber = null,
                IsEllipsis = true,
                IsEnabled = false, // Ellipsis is not clickable
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            ConfigureButton(button, "More pages");
            return button;
        }

        private static DaisyPaginationItem CreateIconButton(string iconKey)
        {
            var button = new DaisyPaginationItem
            {
                PageNumber = null,
                IsEllipsis = false,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            button.Content = CreateNavIcon(button, iconKey);
            return button;
        }

        private static Viewbox CreateNavIcon(DaisyPaginationItem source, string iconKey)
        {
            var pathData = FloweryPathHelpers.GetIconPathData(iconKey);
            var path = new Microsoft.UI.Xaml.Shapes.Path
            {
                Stretch = Stretch.Uniform
            };

            if (!string.IsNullOrEmpty(pathData))
            {
                FloweryPathHelpers.TrySetPathData(path, () => FloweryPathHelpers.ParseGeometry(pathData));
            }

            path.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.FillProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath(nameof(Foreground))
            });

            return new Viewbox
            {
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = path
            };
        }

        private void UpdateActiveIndex()
        {
            if (_join == null)
                return;

            // Find the index of the active page in the joined items
            int activeIndex = -1;

            // Count navigation buttons at the start (unused but documented for future use)
            // int offset = (ShowFirstLast ? 1 : 0) + (ShowJumpButtons ? 1 : 0) + (ShowPrevNext ? 1 : 0);

            // Find the button with the current page number
            for (int i = 0; i < _allButtons.Count; i++)
            {
                if (_allButtons[i].PageNumber == CurrentPage)
                {
                    activeIndex = i;
                    break;
                }
            }

            _join.ActiveIndex = activeIndex;
        }

        private void ApplyAll()
        {
            ApplySizing();
            UpdateActiveIndex();
            UpdateAutomationProperties();
        }

        private void ApplySizing()
        {
            if (_join == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            var effectiveSize = FlowerySizeManager.ShouldIgnoreGlobalSize(this)
                ? Size
                : FlowerySizeManager.CurrentSize;

            // Get sizing from centralized defaults
            double baseHeight = DaisyResourceLookup.GetDefaultHeight(effectiveSize);
            // Make buttons slightly taller for square appearance
            double height = baseHeight + 4;
            double width = height; // Square buttons
            double fontSize = DaisyResourceLookup.GetDefaultFontSize(effectiveSize);
            var base300Brush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            foreach (var button in _allButtons)
            {
                button.MinWidth = width;
                button.Height = height;
                button.FontSize = fontSize;
                button.Padding = new Thickness(4); // Symmetric padding
                button.BorderThickness = new Thickness(1);
                button.BorderBrush = base300Brush;

                // Style ellipsis differently
                if (button.IsEllipsis)
                {
                    button.Opacity = 0.6;
                }
            }
        }

        private void ConfigureButton(DaisyPaginationItem button, string? automationName)
        {
            button.IsTabStop = false;
            if (!string.IsNullOrWhiteSpace(automationName))
            {
                AutomationProperties.SetName(button, automationName);
            }

            button.Click += (_, _) => DaisyAccessibility.FocusOnPointer(this);
        }

        private void UpdateAutomationProperties()
        {
            var name = !string.IsNullOrWhiteSpace(AccessibleText)
                ? AccessibleText
                : $"Page {CurrentPage} of {TotalPages}";

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }
    }
}
