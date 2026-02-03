using Microsoft.UI.Dispatching;

namespace Flowery.Uno.Gallery.Examples
{
    public abstract partial class ScrollableExamplePage : UserControl, IScrollableExample
    {
        private Dictionary<string, FrameworkElement>? _sectionTargetsById;
        private string? _pendingScrollSectionId;

        protected ScrollableExamplePage()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected abstract ScrollViewer? ScrollViewer { get; }

        public void ScrollToSection(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return;

            var scrollViewer = ScrollViewer;
            if (scrollViewer == null)
                return;

            if (!IsLoaded)
            {
                _pendingScrollSectionId = sectionName;
                return;
            }

            var target = GetSectionTarget(sectionName);
            if (target == null)
            {
                var normalized = NormalizeSectionId(sectionName);
                if (!string.Equals(normalized, sectionName, StringComparison.OrdinalIgnoreCase))
                    target = GetSectionTarget(normalized);
            }
            if (target == null)
                return;

            if (!TryScrollToTarget(target))
                target.StartBringIntoView();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureHitTestable();
            WarmSectionIndex();

            if (_pendingScrollSectionId == null)
                return;

            var pending = _pendingScrollSectionId;
            _pendingScrollSectionId = null;
            ScrollToSection(pending);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Ensures ScrollViewer and its content have transparent backgrounds for hit-testing.
        /// In WinUI/Uno, elements without a background are not hit-testable for mouse wheel events.
        /// </summary>
        private void EnsureHitTestable()
        {
            var sv = ScrollViewer;
            if (sv == null)
                return;

            var transparentBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

            // Ensure ScrollViewer is hit-testable
            sv.Background ??= transparentBrush;

            // Ensure content (typically StackPanel) is hit-testable
            if (sv.Content is Panel panel && panel.Background == null)
                panel.Background = transparentBrush;
        }

        private void WarmSectionIndex()
        {
            if (_sectionTargetsById != null)
                return;

            var dispatcher = DispatcherQueue;
            if (dispatcher == null)
            {
                _sectionTargetsById = BuildSectionIndex();
                return;
            }

            if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (_sectionTargetsById == null && IsLoaded)
                {
                    _sectionTargetsById = BuildSectionIndex();
                }
            }))
            {
                _sectionTargetsById = BuildSectionIndex();
            }
        }


        private FrameworkElement? GetSectionTarget(string sectionId)
        {
            _sectionTargetsById ??= BuildSectionIndex();
            return _sectionTargetsById.TryGetValue(sectionId, out var target) ? target : null;
        }

        private Dictionary<string, FrameworkElement> BuildSectionIndex()
        {
            var map = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in EnumerateVisualTree(this))
            {
                if (element is FrameworkElement fe)
                {
                    string? id = null;
                    FrameworkElement? target = null;

                    if (fe is SectionHeader header && !string.IsNullOrWhiteSpace(header.SectionId))
                    {
                        id = header.SectionId.Trim();
                        target = header.Parent as FrameworkElement ?? header;
                    }
                    else
                    {
                        var propsId = SectionProps.GetId(fe);
                        if (!string.IsNullOrWhiteSpace(propsId))
                        {
                            id = propsId.Trim();
                            target = fe;
                        }
                    }

                    if (id != null && target != null)
                    {
                        map[id] = target;

                        var normalized = NormalizeSectionId(id);
                        if (!string.Equals(normalized, id, StringComparison.OrdinalIgnoreCase))
                            map[normalized] = target;
                    }
                }
            }

            return map;
        }

        private static bool TryScrollToTarget(FrameworkElement target)
        {
            try
            {
                // Use StartBringIntoView with VerticalAlignmentRatio = 0.0 to position target at top
                target.StartBringIntoView(new BringIntoViewOptions
                {
                    AnimationDesired = false,
                    VerticalAlignmentRatio = 0.0
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeSectionId(string sectionId)
        {
            return sectionId
                .Trim()
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);
        }

        private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            yield return root;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                foreach (var descendant in EnumerateVisualTree(child))
                    yield return descendant;
            }
        }
    }
}
