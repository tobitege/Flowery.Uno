using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Enums;
using Flowery.Localization;
using Flowery.Theming;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Toolkit.UI;

namespace Flowery.Uno.Kanban.Controls
{
    public partial class FlowKanban
    {
        #region Metrics (No Swimlanes)

        private BoardMetricsSummary BuildBoardMetricsSummary()
        {
            var summary = new BoardMetricsSummary();
            var validLaneIds = BuildValidLaneIds();

            foreach (var column in Board.Columns)
            {
                var columnTotal = 0;
                var columnActive = 0;

                foreach (var task in column.Tasks)
                {
                    summary.TotalCards++;
                    columnTotal++;

                    if (task.IsArchived)
                    {
                        summary.ArchivedCards++;
                        continue;
                    }

                    summary.ActiveCards++;
                    columnActive++;

                    if (task.IsBlocked)
                        summary.BlockedCards++;

                    if (task.IsOverdue)
                        summary.OverdueCards++;

                    if (IsTaskUnassigned(task, validLaneIds))
                        summary.UnassignedCards++;
                }

                summary.ColumnMetrics.Add(new ColumnMetric(column.Title, columnActive, columnTotal));

                if (column.WipLimit.HasValue && column.WipLimit.Value > 0)
                {
                    summary.WipLimitedColumns++;
                    summary.WipCurrent += columnActive;
                    summary.WipLimit += column.WipLimit.Value;
                    if (columnActive > column.WipLimit.Value)
                        summary.WipExceededColumns++;
                }
            }

            return summary;
        }

        private static string FormatColumnCount(ColumnMetric metric)
        {
            return metric.TotalCount == metric.ActiveCount
                ? metric.TotalCount.ToString()
                : $"{metric.ActiveCount}/{metric.TotalCount}";
        }

        #endregion

        #region Metrics (Swimlanes)

        private IReadOnlyList<LaneMetric> BuildLaneMetricsSummary()
        {
            var validLaneIds = BuildValidLaneIds();
            var metrics = new Dictionary<string, LaneMetric>(StringComparer.Ordinal);

            void EnsureLane(string laneId, string title)
            {
                if (!metrics.ContainsKey(laneId))
                {
                    metrics[laneId] = new LaneMetric(laneId, title);
                }
            }

            foreach (var lane in Board.Lanes)
            {
                if (string.IsNullOrWhiteSpace(lane.Id))
                    continue;

                EnsureLane(lane.Id, lane.Title);
            }

            foreach (var column in Board.Columns)
            {
                foreach (var task in column.Tasks)
                {
                    if (task.IsArchived)
                        continue;

                    var laneId = NormalizeLaneId(task.LaneId);
                    var effectiveId = laneId != null && validLaneIds.Contains(laneId)
                        ? laneId
                        : UnassignedLaneId;

                    if (effectiveId == UnassignedLaneId)
                    {
                        EnsureLane(UnassignedLaneId, FloweryLocalization.GetStringInternal("Kanban_Lanes_Unassigned"));
                    }

                    if (!metrics.TryGetValue(effectiveId, out var laneMetric))
                    {
                        var title = effectiveId == UnassignedLaneId
                            ? FloweryLocalization.GetStringInternal("Kanban_Lanes_Unassigned")
                            : effectiveId;
                        laneMetric = new LaneMetric(effectiveId, title);
                        metrics[effectiveId] = laneMetric;
                    }

                    laneMetric.ActiveCount++;
                    if (task.IsBlocked)
                        laneMetric.BlockedCount++;
                    if (task.IsOverdue)
                        laneMetric.OverdueCount++;
                }
            }

            var ordered = new List<LaneMetric>();
            if (metrics.TryGetValue(UnassignedLaneId, out var unassigned))
                ordered.Add(unassigned);

            foreach (var lane in Board.Lanes)
            {
                if (string.IsNullOrWhiteSpace(lane.Id))
                    continue;

                if (metrics.TryGetValue(lane.Id, out var metric))
                    ordered.Add(metric);
            }

            return ordered;
        }

        #endregion

        private async void ExecuteShowMetrics()
        {
            if (XamlRoot == null)
                return;

            await FlowKanbanMetricsDialog.ShowAsync(this, XamlRoot);
        }

        private HashSet<string> BuildValidLaneIds()
        {
            var validLaneIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var lane in Board.Lanes)
            {
                if (!string.IsNullOrWhiteSpace(lane.Id))
                    validLaneIds.Add(lane.Id);
            }

            return validLaneIds;
        }

        private static bool IsTaskUnassigned(FlowTask task, HashSet<string> validLaneIds)
        {
            if (IsUnassignedLaneId(task.LaneId))
                return true;

            if (string.IsNullOrWhiteSpace(task.LaneId))
                return true;

            return !validLaneIds.Contains(task.LaneId);
        }

        private static Brush? GetMetricAccentBrush(DaisyColor color)
        {
            var key = color switch
            {
                DaisyColor.Primary => "DaisyPrimaryBrush",
                DaisyColor.Secondary => "DaisySecondaryBrush",
                DaisyColor.Accent => "DaisyAccentBrush",
                DaisyColor.Neutral => "DaisyNeutralBrush",
                DaisyColor.Info => "DaisyInfoBrush",
                DaisyColor.Success => "DaisySuccessBrush",
                DaisyColor.Warning => "DaisyWarningBrush",
                DaisyColor.Error => "DaisyErrorBrush",
                _ => null
            };

            return key == null ? null : DaisyResourceLookup.GetBrush(key);
        }

        private sealed class BoardMetricsSummary
        {
            public int TotalCards { get; set; }
            public int ActiveCards { get; set; }
            public int ArchivedCards { get; set; }
            public int BlockedCards { get; set; }
            public int OverdueCards { get; set; }
            public int UnassignedCards { get; set; }
            public int WipCurrent { get; set; }
            public int WipLimit { get; set; }
            public int WipLimitedColumns { get; set; }
            public int WipExceededColumns { get; set; }
            public List<ColumnMetric> ColumnMetrics { get; } = new();
        }

        private sealed class ColumnMetric
        {
            public ColumnMetric(string? title, int activeCount, int totalCount)
            {
                Title = string.IsNullOrWhiteSpace(title)
                    ? FloweryLocalization.GetStringInternal("Kanban_Metrics_UntitledColumn")
                    : title;
                ActiveCount = activeCount;
                TotalCount = totalCount;
            }

            public string Title { get; }
            public int ActiveCount { get; }
            public int TotalCount { get; }
        }

        private sealed class LaneMetric
        {
            public LaneMetric(string id, string title)
            {
                Id = id;
                Title = string.IsNullOrWhiteSpace(title)
                    ? FloweryLocalization.GetStringInternal("Kanban_Metrics_UntitledLane")
                    : title;
            }

            public string Id { get; }
            public string Title { get; }
            public int ActiveCount { get; set; }
            public int BlockedCount { get; set; }
            public int OverdueCount { get; set; }
        }

        private sealed partial class FlowKanbanMetricsDialog : FloweryDialogBase
        {
            private readonly TaskCompletionSource<bool> _tcs = new();
            private readonly XamlRoot _xamlRoot;
            private Panel? _hostPanel;
            private bool _isClosing;
            private long _isOpenCallbackToken;

            private readonly DaisyButton _closeButton;

            private FlowKanbanMetricsDialog(FlowKanban owner, XamlRoot xamlRoot)
            {
                _xamlRoot = xamlRoot;

                Content = CreateDialogLayout(owner, xamlRoot, out _closeButton);
                IsDraggable = true;
                ApplySmartSizingWithAutoHeight(xamlRoot);

                _closeButton.Click += OnCloseClicked;
                _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
            }

            public static Task ShowAsync(FlowKanban owner, XamlRoot xamlRoot)
            {
                if (xamlRoot == null)
                    return Task.CompletedTask;

                var dialog = new FlowKanbanMetricsDialog(owner, xamlRoot);
                return dialog.ShowInternalAsync();
            }

            private Task ShowInternalAsync()
            {
                _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
                if (_hostPanel == null)
                    return Task.CompletedTask;

                _hostPanel.Children.Add(this);
                IsOpen = true;
                DispatcherQueue?.TryEnqueue(() => _closeButton.Focus(FocusState.Programmatic));
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
                if (IsOpen)
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

            private static FrameworkElement CreateDialogLayout(FlowKanban owner, XamlRoot xamlRoot, out DaisyButton closeButton)
            {
                var flowDirection = FloweryLocalization.Instance.IsRtl
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight;

                var title = CreateTextBlock(
                    FloweryLocalization.GetStringInternal("Kanban_BoardStatistics"),
                    "TitleTextBlockStyle");

                var subtitle = CreateTextBlock(
                    owner.Board.Title,
                    "BodyTextBlockStyle");
                subtitle.Opacity = 0.7;

                var header = new AutoLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    Padding = new Thickness(16, 16, 16, 0),
                    FlowDirection = flowDirection
                };
                header.Children.Add(title);
                header.Children.Add(subtitle);

                var summary = owner.BuildBoardMetricsSummary();
                var body = new StackPanel
                {
                    Spacing = 16,
                    Padding = new Thickness(16, 0, 16, 0),
                    FlowDirection = flowDirection
                };

                body.Children.Add(CreateSectionHeader(FloweryLocalization.GetStringInternal("Kanban_Metrics_Overview")));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_TotalCards"),
                    summary.TotalCards.ToString(),
                    DaisyColor.Primary));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_ActiveCards"),
                    summary.ActiveCards.ToString(),
                    DaisyColor.Secondary));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_ArchivedCards"),
                    summary.ArchivedCards.ToString(),
                    DaisyColor.Neutral));

                body.Children.Add(CreateSectionHeader(FloweryLocalization.GetStringInternal("Kanban_Metrics_Health")));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_BlockedCards"),
                    summary.BlockedCards.ToString(),
                    DaisyColor.Error));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_OverdueCards"),
                    summary.OverdueCards.ToString(),
                    DaisyColor.Warning));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_UnassignedCards"),
                    summary.UnassignedCards.ToString(),
                    DaisyColor.Info));

                body.Children.Add(CreateSectionHeader(FloweryLocalization.GetStringInternal("Kanban_Metrics_Wip")));
                var wipUsage = summary.WipLimit > 0 ? $"{summary.WipCurrent}/{summary.WipLimit}" : "-";
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_WipUsage"),
                    wipUsage,
                    DaisyColor.Info));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_WipLimitedColumns"),
                    summary.WipLimitedColumns.ToString(),
                    DaisyColor.Neutral));
                body.Children.Add(CreateMetricRow(
                    FloweryLocalization.GetStringInternal("Kanban_Metrics_WipOverLimit"),
                    summary.WipExceededColumns.ToString(),
                    summary.WipExceededColumns > 0 ? DaisyColor.Error : DaisyColor.Success));

                body.Children.Add(CreateSectionHeader(FloweryLocalization.GetStringInternal("Kanban_Metrics_ByColumn")));
                foreach (var column in summary.ColumnMetrics)
                {
                    body.Children.Add(CreateMetricRow(column.Title, FormatColumnCount(column)));
                }

                if (owner.IsLaneGroupingEnabled)
                {
                    var laneMetrics = owner.BuildLaneMetricsSummary();
                    if (laneMetrics.Count > 0)
                    {
                        body.Children.Add(CreateSectionHeader(FloweryLocalization.GetStringInternal("Kanban_Metrics_ByLane")));
                        foreach (var lane in laneMetrics)
                        {
                            var value = $"{lane.ActiveCount} | {lane.BlockedCount} | {lane.OverdueCount}";
                            body.Children.Add(CreateMetricRow(lane.Title, value));
                        }

                        body.Children.Add(CreateLegendRow(
                            FloweryLocalization.GetStringInternal("Kanban_Metrics_Legend_Active"),
                            FloweryLocalization.GetStringInternal("Kanban_Metrics_Legend_Blocked"),
                            FloweryLocalization.GetStringInternal("Kanban_Metrics_Legend_Overdue")));
                    }
                }

                var footer = new AutoLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = flowDirection == FlowDirection.RightToLeft
                        ? HorizontalAlignment.Left
                        : HorizontalAlignment.Right,
                    Spacing = 12,
                    Padding = new Thickness(16, 0, 24, 16),
                    FlowDirection = flowDirection
                };

                closeButton = new DaisyButton
                {
                    Content = FloweryLocalization.GetStringInternal("Common_Close"),
                    Variant = DaisyButtonVariant.Primary,
                    Size = Enums.DaisySize.Medium,
                    MinWidth = 80
                };

                footer.Children.Add(closeButton);

                var container = FloweryDialogBase.CreateDialogContent(xamlRoot, header, body, footer);
                container.FlowDirection = flowDirection;
                var chromePadding = FloweryDialogBase.ContentDialogChrome;
                var minWidth = FloweryDialogBase.AbsoluteMinWidth - chromePadding;
                container.Width = Math.Max(minWidth, container.Width - chromePadding);

                return container;
            }

            private static TextBlock CreateSectionHeader(string text)
            {
                return new TextBlock
                {
                    Text = text,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 0),
                    TextAlignment = FloweryLocalization.Instance.IsRtl
                        ? Microsoft.UI.Xaml.TextAlignment.Right
                        : Microsoft.UI.Xaml.TextAlignment.Left,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };
            }

            private static FrameworkElement CreateMetricRow(string label, string value, DaisyColor accent = DaisyColor.Default)
            {
                var labelAlignment = FloweryLocalization.Instance.IsRtl
                    ? Microsoft.UI.Xaml.TextAlignment.Right
                    : Microsoft.UI.Xaml.TextAlignment.Left;
                var valueAlignment = FloweryLocalization.Instance.IsRtl
                    ? Microsoft.UI.Xaml.TextAlignment.Left
                    : Microsoft.UI.Xaml.TextAlignment.Right;

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 12
                };

                var labelBlock = new TextBlock
                {
                    Text = label,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = labelAlignment,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };

                var valueBlock = new TextBlock
                {
                    Text = value,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = valueAlignment,
                    Foreground = GetMetricAccentBrush(accent) ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };

                Grid.SetColumn(labelBlock, 0);
                Grid.SetColumn(valueBlock, 1);
                grid.Children.Add(labelBlock);
                grid.Children.Add(valueBlock);
                return grid;
            }

            private static FrameworkElement CreateLegendRow(string activeLabel, string blockedLabel, string overdueLabel)
            {
                var text = $"{activeLabel} | {blockedLabel} | {overdueLabel}";
                return new TextBlock
                {
                    Text = text,
                    Margin = new Thickness(0, 4, 0, 0),
                    Opacity = 0.7,
                    TextAlignment = FloweryLocalization.Instance.IsRtl
                        ? Microsoft.UI.Xaml.TextAlignment.Right
                        : Microsoft.UI.Xaml.TextAlignment.Left,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };
            }

            private static TextBlock CreateTextBlock(string text, string styleKey)
            {
                var block = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = FloweryLocalization.Instance.IsRtl
                        ? Microsoft.UI.Xaml.TextAlignment.Right
                        : Microsoft.UI.Xaml.TextAlignment.Left
                };

                if (DaisyResourceLookup.TryGetResource(Application.Current?.Resources, styleKey, out var style)
                    && style is Style textStyle)
                {
                    block.Style = textStyle;
                }

                return block;
            }
        }
    }
}
