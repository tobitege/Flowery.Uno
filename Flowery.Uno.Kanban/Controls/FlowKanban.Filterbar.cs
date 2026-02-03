using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Flowery.Controls;
using Flowery.Localization;
using Flowery.Services;
using Flowery.Uno.Kanban.Controls.Users;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// Partial class containing filterbar-related properties and methods for FlowKanban.
    /// </summary>
    public partial class FlowKanban
    {
        private const string FilterPresetsStorageKey = "kanban.filter.presets.global";

        private ObservableCollection<FlowKanbanFilterPreset> _filterPresets = new();
        private DaisyCalendarDatePicker? _filterDateFromPicker;
        private DaisyCalendarDatePicker? _filterDateToPicker;
        private bool _isSyncingFilterDates;
        private bool _isSyncingSearchText;
        private bool _isSyncingAssigneeFilter;
        private int _assigneeFilterRefreshVersion;

        #region Filter DPs

        public static readonly DependencyProperty FilterCriteriaProperty =
            DependencyProperty.Register(
                nameof(FilterCriteria),
                typeof(FlowKanbanFilterCriteria),
                typeof(FlowKanban),
                new PropertyMetadata(null, OnFilterCriteriaChanged));

        /// <summary>
        /// Current filter criteria applied to the board.
        /// </summary>
        public FlowKanbanFilterCriteria? FilterCriteria
        {
            get => (FlowKanbanFilterCriteria?)GetValue(FilterCriteriaProperty);
            set => SetValue(FilterCriteriaProperty, value);
        }

        public static readonly DependencyProperty IsFilterActiveProperty =
            DependencyProperty.Register(
                nameof(IsFilterActive),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        /// <summary>
        /// True when any filter criterion is active.
        /// </summary>
        public bool IsFilterActive
        {
            get => (bool)GetValue(IsFilterActiveProperty);
            private set => SetValue(IsFilterActiveProperty, value);
        }

        public static readonly DependencyProperty ActiveFilterCountProperty =
            DependencyProperty.Register(
                nameof(ActiveFilterCount),
                typeof(int),
                typeof(FlowKanban),
                new PropertyMetadata(0));

        /// <summary>
        /// Number of active filter criteria.
        /// </summary>
        public int ActiveFilterCount
        {
            get => (int)GetValue(ActiveFilterCountProperty);
            private set => SetValue(ActiveFilterCountProperty, value);
        }

        public static readonly DependencyProperty IsFilterExpandedProperty =
            DependencyProperty.Register(
                nameof(IsFilterExpanded),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether the filter panel is expanded.
        /// </summary>
        public bool IsFilterExpanded
        {
            get => (bool)GetValue(IsFilterExpandedProperty);
            set => SetValue(IsFilterExpandedProperty, value);
        }

        public static readonly DependencyProperty FilterShowOverdueProperty =
            DependencyProperty.Register(
                nameof(FilterShowOverdue),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterShowOverdueChanged));

        /// <summary>
        /// Whether to filter to only overdue tasks.
        /// </summary>
        public bool FilterShowOverdue
        {
            get => (bool)GetValue(FilterShowOverdueProperty);
            set => SetValue(FilterShowOverdueProperty, value);
        }

        private static void OnFilterShowOverdueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            var criteria = kanban.EnsureFilterCriteria();
            criteria.ShowOnlyOverdue = (bool)e.NewValue;
            criteria.NotifyChanged();
        }

        public static readonly DependencyProperty FilterShowBlockedProperty =
            DependencyProperty.Register(
                nameof(FilterShowBlocked),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterShowBlockedChanged));

        /// <summary>
        /// Whether to filter to only blocked tasks.
        /// </summary>
        public bool FilterShowBlocked
        {
            get => (bool)GetValue(FilterShowBlockedProperty);
            set => SetValue(FilterShowBlockedProperty, value);
        }

        private static void OnFilterShowBlockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            var criteria = kanban.EnsureFilterCriteria();
            criteria.ShowOnlyBlocked = (bool)e.NewValue;
            criteria.NotifyChanged();
        }

        #region Priority Filter Bridge DPs

        public static readonly DependencyProperty FilterPriorityLowProperty =
            DependencyProperty.Register(
                nameof(FilterPriorityLow),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterPriorityLowChanged));

        /// <summary>
        /// Whether Low priority filter is active.
        /// </summary>
        public bool FilterPriorityLow
        {
            get => (bool)GetValue(FilterPriorityLowProperty);
            set => SetValue(FilterPriorityLowProperty, value);
        }

        private static void OnFilterPriorityLowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdatePriorityFilter(FlowTaskPriority.Low, (bool)e.NewValue);
        }

        public static readonly DependencyProperty FilterPriorityNormalProperty =
            DependencyProperty.Register(
                nameof(FilterPriorityNormal),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterPriorityNormalChanged));

        /// <summary>
        /// Whether Normal priority filter is active.
        /// </summary>
        public bool FilterPriorityNormal
        {
            get => (bool)GetValue(FilterPriorityNormalProperty);
            set => SetValue(FilterPriorityNormalProperty, value);
        }

        private static void OnFilterPriorityNormalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdatePriorityFilter(FlowTaskPriority.Normal, (bool)e.NewValue);
        }

        public static readonly DependencyProperty FilterPriorityHighProperty =
            DependencyProperty.Register(
                nameof(FilterPriorityHigh),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterPriorityHighChanged));

        /// <summary>
        /// Whether High priority filter is active.
        /// </summary>
        public bool FilterPriorityHigh
        {
            get => (bool)GetValue(FilterPriorityHighProperty);
            set => SetValue(FilterPriorityHighProperty, value);
        }

        private static void OnFilterPriorityHighChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdatePriorityFilter(FlowTaskPriority.High, (bool)e.NewValue);
        }

        public static readonly DependencyProperty FilterPriorityUrgentProperty =
            DependencyProperty.Register(
                nameof(FilterPriorityUrgent),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterPriorityUrgentChanged));

        /// <summary>
        /// Whether Urgent priority filter is active.
        /// </summary>
        public bool FilterPriorityUrgent
        {
            get => (bool)GetValue(FilterPriorityUrgentProperty);
            set => SetValue(FilterPriorityUrgentProperty, value);
        }

        private static void OnFilterPriorityUrgentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdatePriorityFilter(FlowTaskPriority.Urgent, (bool)e.NewValue);
        }

        private void UpdatePriorityFilter(FlowTaskPriority priority, bool isSelected)
        {
            var criteria = EnsureFilterCriteria();
            if (criteria.Priorities == null)
                criteria.Priorities = new List<FlowTaskPriority>();

            if (isSelected && !criteria.Priorities.Contains(priority))
                criteria.Priorities.Add(priority);
            else if (!isSelected && criteria.Priorities.Contains(priority))
                criteria.Priorities.Remove(priority);

            criteria.NotifyChanged();
        }

        #endregion

        #region Date Range Filter Bridge DPs

        public static readonly DependencyProperty FilterDateFromProperty =
            DependencyProperty.Register(
                nameof(FilterDateFrom),
                typeof(DateTimeOffset?),
                typeof(FlowKanban),
                new PropertyMetadata(null, OnFilterDateFromChanged));

        /// <summary>
        /// Start date for due date range filter.
        /// </summary>
        public DateTimeOffset? FilterDateFrom
        {
            get => (DateTimeOffset?)GetValue(FilterDateFromProperty);
            set => SetValue(FilterDateFromProperty, value);
        }

        private static void OnFilterDateFromChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdateDateRangeFilter();
            kanban.SyncFilterDatePickers();
        }

        public static readonly DependencyProperty FilterDateToProperty =
            DependencyProperty.Register(
                nameof(FilterDateTo),
                typeof(DateTimeOffset?),
                typeof(FlowKanban),
                new PropertyMetadata(null, OnFilterDateToChanged));

        /// <summary>
        /// End date for due date range filter.
        /// </summary>
        public DateTimeOffset? FilterDateTo
        {
            get => (DateTimeOffset?)GetValue(FilterDateToProperty);
            set => SetValue(FilterDateToProperty, value);
        }

        private static void OnFilterDateToChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdateDateRangeFilter();
            kanban.SyncFilterDatePickers();
        }

        private void UpdateDateRangeFilter()
        {
            var criteria = EnsureFilterCriteria();
            var from = FilterDateFrom?.DateTime;
            var to = FilterDateTo?.DateTime;

            if (from.HasValue || to.HasValue)
            {
                criteria.PlannedEndRange = new FlowKanbanDateRange
                {
                    From = from,
                    To = to?.Date.AddDays(1).AddTicks(-1) // End of day
                };
            }
            else
            {
                criteria.PlannedEndRange = null;
            }

            criteria.NotifyChanged();
        }

        #endregion

        #region Quick Date Filter Bridge DPs

        public static readonly DependencyProperty FilterDueTodayProperty =
            DependencyProperty.Register(
                nameof(FilterDueToday),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterDueTodayChanged));

        /// <summary>
        /// Whether the "Today" quick date filter is active.
        /// </summary>
        public bool FilterDueToday
        {
            get => (bool)GetValue(FilterDueTodayProperty);
            set => SetValue(FilterDueTodayProperty, value);
        }

        private static void OnFilterDueTodayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdateQuickDateFilters();
        }

        public static readonly DependencyProperty FilterDueThisWeekProperty =
            DependencyProperty.Register(
                nameof(FilterDueThisWeek),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false, OnFilterDueThisWeekChanged));

        /// <summary>
        /// Whether the "This Week" quick date filter is active.
        /// </summary>
        public bool FilterDueThisWeek
        {
            get => (bool)GetValue(FilterDueThisWeekProperty);
            set => SetValue(FilterDueThisWeekProperty, value);
        }

        private static void OnFilterDueThisWeekChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdateQuickDateFilters();
        }

        private void UpdateQuickDateFilters()
        {
            var today = DateTime.Today;
            DateTime? from = null;
            DateTime? to = null;

            if (FilterDueToday)
            {
                from = today;
                to = today;
            }

            if (FilterDueThisWeek)
            {
                var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;
                if (daysUntilSunday == 0) daysUntilSunday = 7;
                var weekEnd = today.AddDays(daysUntilSunday - 1);

                // Combine with Today if both are checked
                if (from.HasValue)
                {
                    // Keep from as today, extend to to week end
                    to = weekEnd;
                }
                else
                {
                    from = today;
                    to = weekEnd;
                }
            }

            // Update the bridge DPs (which will trigger UpdateDateRangeFilter)
            if (from.HasValue)
            {
                FilterDateFrom = new DateTimeOffset(from.Value);
                FilterDateTo = new DateTimeOffset(to!.Value);
            }
            else
            {
                // Clear date filter when both unchecked
                FilterDateFrom = null;
                FilterDateTo = null;
            }
        }

        #endregion

        #region Assignee Filter DPs

        public static readonly DependencyProperty AssigneeFilterOptionsProperty =
            DependencyProperty.Register(
                nameof(AssigneeFilterOptions),
                typeof(ObservableCollection<FlowTaskAssigneeOption>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Available assignees for filtering.
        /// </summary>
        public ObservableCollection<FlowTaskAssigneeOption> AssigneeFilterOptions
        {
            get
            {
                if (GetValue(AssigneeFilterOptionsProperty) is not ObservableCollection<FlowTaskAssigneeOption> options)
                {
                    options = new ObservableCollection<FlowTaskAssigneeOption>();
                    SetValue(AssigneeFilterOptionsProperty, options);
                }
                return options;
            }
            private set => SetValue(AssigneeFilterOptionsProperty, value);
        }

        public static readonly DependencyProperty SelectedAssigneeFilterProperty =
            DependencyProperty.Register(
                nameof(SelectedAssigneeFilter),
                typeof(FlowTaskAssigneeOption),
                typeof(FlowKanban),
                new PropertyMetadata(null, OnSelectedAssigneeFilterChanged));

        /// <summary>
        /// Selected assignee filter option.
        /// </summary>
        public FlowTaskAssigneeOption? SelectedAssigneeFilter
        {
            get => (FlowTaskAssigneeOption?)GetValue(SelectedAssigneeFilterProperty);
            set => SetValue(SelectedAssigneeFilterProperty, value);
        }

        public static readonly DependencyProperty HasAssigneeOptionsProperty =
            DependencyProperty.Register(
                nameof(HasAssigneeOptions),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        /// <summary>
        /// True when assignee options are available.
        /// </summary>
        public bool HasAssigneeOptions
        {
            get => (bool)GetValue(HasAssigneeOptionsProperty);
            private set => SetValue(HasAssigneeOptionsProperty, value);
        }

        private static void OnSelectedAssigneeFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kanban = (FlowKanban)d;
            kanban.UpdateAssigneeFilterFromSelection();
        }

        #endregion

        public static readonly DependencyProperty IsFilterDirtyProperty =
            DependencyProperty.Register(
                nameof(IsFilterDirty),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        /// <summary>
        /// True when filter criteria have been modified since last save/load.
        /// </summary>
        public bool IsFilterDirty
        {
            get => (bool)GetValue(IsFilterDirtyProperty);
            private set => SetValue(IsFilterDirtyProperty, value);
        }

        public static readonly DependencyProperty ActivePresetProperty =
            DependencyProperty.Register(
                nameof(ActivePreset),
                typeof(FlowKanbanFilterPreset),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Currently loaded filter preset.
        /// </summary>
        public FlowKanbanFilterPreset? ActivePreset
        {
            get => (FlowKanbanFilterPreset?)GetValue(ActivePresetProperty);
            private set => SetValue(ActivePresetProperty, value);
        }

        public static readonly DependencyProperty FilterPresetsProperty =
            DependencyProperty.Register(
                nameof(FilterPresets),
                typeof(ObservableCollection<FlowKanbanFilterPreset>),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        /// <summary>
        /// Available filter presets.
        /// </summary>
        public ObservableCollection<FlowKanbanFilterPreset> FilterPresets
        {
            get
            {
                if (GetValue(FilterPresetsProperty) is not ObservableCollection<FlowKanbanFilterPreset> presets)
                {
                    presets = new ObservableCollection<FlowKanbanFilterPreset>();
                    SetValue(FilterPresetsProperty, presets);
                }
                return presets;
            }
            private set => SetValue(FilterPresetsProperty, value);
        }

        public static readonly DependencyProperty HasFilterPresetsProperty =
            DependencyProperty.Register(
                nameof(HasFilterPresets),
                typeof(bool),
                typeof(FlowKanban),
                new PropertyMetadata(false));

        /// <summary>
        /// True when saved filter presets exist.
        /// </summary>
        public bool HasFilterPresets
        {
            get => (bool)GetValue(HasFilterPresetsProperty);
            private set => SetValue(HasFilterPresetsProperty, value);
        }

        #endregion

        #region Filter Commands

        public static readonly DependencyProperty ClearFilterCommandProperty =
            DependencyProperty.Register(
                nameof(ClearFilterCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ClearFilterCommand
        {
            get => (ICommand)GetValue(ClearFilterCommandProperty);
            set => SetValue(ClearFilterCommandProperty, value);
        }

        public static readonly DependencyProperty SaveFilterPresetCommandProperty =
            DependencyProperty.Register(
                nameof(SaveFilterPresetCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand SaveFilterPresetCommand
        {
            get => (ICommand)GetValue(SaveFilterPresetCommandProperty);
            set => SetValue(SaveFilterPresetCommandProperty, value);
        }

        public static readonly DependencyProperty LoadFilterPresetCommandProperty =
            DependencyProperty.Register(
                nameof(LoadFilterPresetCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand LoadFilterPresetCommand
        {
            get => (ICommand)GetValue(LoadFilterPresetCommandProperty);
            set => SetValue(LoadFilterPresetCommandProperty, value);
        }

        public static readonly DependencyProperty DeleteFilterPresetCommandProperty =
            DependencyProperty.Register(
                nameof(DeleteFilterPresetCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand DeleteFilterPresetCommand
        {
            get => (ICommand)GetValue(DeleteFilterPresetCommandProperty);
            set => SetValue(DeleteFilterPresetCommandProperty, value);
        }

        public static readonly DependencyProperty ToggleFilterExpandedCommandProperty =
            DependencyProperty.Register(
                nameof(ToggleFilterExpandedCommand),
                typeof(ICommand),
                typeof(FlowKanban),
                new PropertyMetadata(null));

        public ICommand ToggleFilterExpandedCommand
        {
            get => (ICommand)GetValue(ToggleFilterExpandedCommandProperty);
            set => SetValue(ToggleFilterExpandedCommandProperty, value);
        }

        #endregion

        #region Filter Callbacks

        private static void OnFilterCriteriaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowKanban kanban)
            {
                if (e.OldValue is FlowKanbanFilterCriteria oldCriteria)
                {
                    oldCriteria.PropertyChanged -= kanban.OnFilterCriteriaPropertyChanged;
                }

                if (e.NewValue is FlowKanbanFilterCriteria newCriteria)
                {
                    newCriteria.PropertyChanged += kanban.OnFilterCriteriaPropertyChanged;
                }

                kanban.SyncSearchTextFromCriteria();
                kanban.SyncSelectedAssigneeFromCriteria();
                kanban.UpdateFilterState();
                kanban.ApplyFilter();
            }
        }

        private void OnFilterCriteriaPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsFilterDirty = true;
            if (string.Equals(e.PropertyName, nameof(FlowKanbanFilterCriteria.TextQuery), StringComparison.Ordinal))
            {
                SyncSearchTextFromCriteria();
            }
            SyncSelectedAssigneeFromCriteria();
            UpdateFilterState();
            ApplyFilter();
        }

        private void UpdateFilterState()
        {
            var criteria = FilterCriteria;
            IsFilterActive = criteria?.HasAnyFilter == true;
            ActiveFilterCount = CountActiveFilters(criteria);
        }

        private int CountActiveFilters(FlowKanbanFilterCriteria? criteria)
        {
            if (criteria == null)
                return 0;

            int count = 0;
            if (!string.IsNullOrWhiteSpace(criteria.TextQuery)) count++;
            if (criteria.Priorities?.Count > 0) count++;
            if (criteria.ShowOnlyOverdue == true) count++;
            if (criteria.ShowOnlyBlocked == true) count++;
            if (criteria.PlannedStartRange?.HasValue == true) count++;
            if (criteria.PlannedEndRange?.HasValue == true) count++;
            if (criteria.IncludedColumnIds?.Count > 0) count++;
            if (criteria.AssigneeIds?.Count > 0) count++;
            return count;
        }

        #endregion

        #region Filter Date Picker Sync

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Manual sync avoids DateTimeOffset binding conversion issues in Uno.
            HookFilterDatePickers();
        }

        private void HookFilterDatePickers()
        {
            DetachFilterDatePickers();

            _filterDateFromPicker = GetTemplateChild("PART_FilterDateFrom") as DaisyCalendarDatePicker;
            _filterDateToPicker = GetTemplateChild("PART_FilterDateTo") as DaisyCalendarDatePicker;

            if (_filterDateFromPicker != null)
            {
                _filterDateFromPicker.DateChanged += OnFilterDateFromPickerChanged;
            }

            if (_filterDateToPicker != null)
            {
                _filterDateToPicker.DateChanged += OnFilterDateToPickerChanged;
            }

            SyncFilterDatePickers();
        }

        private void DetachFilterDatePickers()
        {
            if (_filterDateFromPicker != null)
            {
                _filterDateFromPicker.DateChanged -= OnFilterDateFromPickerChanged;
            }

            if (_filterDateToPicker != null)
            {
                _filterDateToPicker.DateChanged -= OnFilterDateToPickerChanged;
            }
        }

        private void OnFilterDateFromPickerChanged(DaisyCalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isSyncingFilterDates)
                return;

            _isSyncingFilterDates = true;
            try
            {
                FilterDateFrom = sender.Date;
            }
            finally
            {
                _isSyncingFilterDates = false;
            }
        }

        private void OnFilterDateToPickerChanged(DaisyCalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_isSyncingFilterDates)
                return;

            _isSyncingFilterDates = true;
            try
            {
                FilterDateTo = sender.Date;
            }
            finally
            {
                _isSyncingFilterDates = false;
            }
        }

        private void SyncFilterDatePickers()
        {
            if (_isSyncingFilterDates)
                return;

            _isSyncingFilterDates = true;
            try
            {
                if (_filterDateFromPicker != null && !Equals(_filterDateFromPicker.Date, FilterDateFrom))
                {
                    _filterDateFromPicker.Date = FilterDateFrom;
                }

                if (_filterDateToPicker != null && !Equals(_filterDateToPicker.Date, FilterDateTo))
                {
                    _filterDateToPicker.Date = FilterDateTo;
                }
            }
            finally
            {
                _isSyncingFilterDates = false;
            }
        }

        #endregion

        #region Filter Methods

        /// <summary>
        /// Initializes filter commands and loads saved presets.
        /// </summary>
        private void InitializeFilterbar()
        {
            ClearFilterCommand = new RelayCommand(ExecuteClearFilter);
            SaveFilterPresetCommand = new RelayCommand(ExecuteSaveFilterPreset);
            LoadFilterPresetCommand = new RelayCommand<FlowKanbanFilterPreset>(ExecuteLoadFilterPreset);
            DeleteFilterPresetCommand = new RelayCommand<FlowKanbanFilterPreset>(ExecuteDeleteFilterPreset);
            ToggleFilterExpandedCommand = new RelayCommand(ExecuteToggleFilterExpanded);

            LoadFilterPresets();
        }

        private FlowKanbanFilterCriteria EnsureFilterCriteria()
        {
            if (FilterCriteria == null)
                FilterCriteria = new FlowKanbanFilterCriteria();
            return FilterCriteria;
        }

        private void SyncSearchTextFromCriteria()
        {
            if (_isSyncingSearchText)
                return;

            _isSyncingSearchText = true;
            try
            {
                var criteriaText = FilterCriteria?.TextQuery ?? string.Empty;
                if (!string.Equals(SearchText, criteriaText, StringComparison.Ordinal))
                {
                    SearchText = criteriaText;
                }
            }
            finally
            {
                _isSyncingSearchText = false;
            }
        }

        private void SyncCriteriaTextFromSearch()
        {
            if (_isSyncingSearchText)
                return;

            if (FilterCriteria == null && string.IsNullOrWhiteSpace(SearchText))
                return;

            _isSyncingSearchText = true;
            try
            {
                var criteria = EnsureFilterCriteria();
                var normalized = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
                if (!string.Equals(criteria.TextQuery, normalized, StringComparison.Ordinal))
                {
                    criteria.TextQuery = normalized;
                }
            }
            finally
            {
                _isSyncingSearchText = false;
            }
        }

        private void UpdateAssigneeFilterFromSelection()
        {
            if (_isSyncingAssigneeFilter)
                return;

            var selected = SelectedAssigneeFilter;
            var criteria = FilterCriteria;
            if (criteria == null)
            {
                if (selected == null)
                    return;

                criteria = EnsureFilterCriteria();
            }

            _isSyncingAssigneeFilter = true;
            try
            {
                if (selected == null || string.IsNullOrWhiteSpace(selected.Id))
                {
                    criteria.AssigneeIds = null;
                }
                else
                {
                    criteria.AssigneeIds = new List<string> { selected.Id };
                }
                criteria.NotifyChanged();
            }
            finally
            {
                _isSyncingAssigneeFilter = false;
            }
        }

        private void SyncSelectedAssigneeFromCriteria()
        {
            if (_isSyncingAssigneeFilter)
                return;

            var assigneeId = FilterCriteria?.AssigneeIds?.FirstOrDefault();
            FlowTaskAssigneeOption? match = null;
            if (!string.IsNullOrWhiteSpace(assigneeId))
            {
                match = AssigneeFilterOptions.FirstOrDefault(option =>
                    string.Equals(option.Id, assigneeId, StringComparison.Ordinal));
            }

            _isSyncingAssigneeFilter = true;
            try
            {
                if (!ReferenceEquals(SelectedAssigneeFilter, match))
                {
                    SelectedAssigneeFilter = match;
                }
            }
            finally
            {
                _isSyncingAssigneeFilter = false;
            }
        }

        private void UpdateAssigneeFilterOptionsFromUsers(IEnumerable<IFlowUser> users)
        {
            var options = BuildAssigneeFilterOptions(users);
            UpdateAssigneeFilterOptions(options);
        }

        private void UpdateAssigneeFilterOptions(IReadOnlyList<FlowTaskAssigneeOption> options)
        {
            var items = AssigneeFilterOptions;
            items.Clear();

            foreach (var option in options)
            {
                items.Add(option);
            }

            HasAssigneeOptions = items.Count > 0;
            SyncSelectedAssigneeFromCriteria();
        }

        private void ClearAssigneeFilterOptions()
        {
            AssigneeFilterOptions.Clear();
            HasAssigneeOptions = false;
            SyncSelectedAssigneeFromCriteria();
        }

        private static List<FlowTaskAssigneeOption> BuildAssigneeFilterOptions(IEnumerable<IFlowUser> users)
        {
            var options = new List<FlowTaskAssigneeOption>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var user in users ?? Array.Empty<IFlowUser>())
            {
                if (user == null)
                    continue;

                var id = user.Id?.Trim();
                if (string.IsNullOrWhiteSpace(id) || !seenIds.Add(id))
                    continue;

                var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? id : user.DisplayName.Trim();
                options.Add(new FlowTaskAssigneeOption(id, displayName));
            }

            options.Sort((left, right) =>
                string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return options;
        }

        private void ExecuteClearFilter()
        {
            // Reset all bridge DPs first (prevents re-triggering filter updates)
            FilterPriorityLow = false;
            FilterPriorityNormal = false;
            FilterPriorityHigh = false;
            FilterPriorityUrgent = false;
            FilterShowOverdue = false;
            FilterShowBlocked = false;
            FilterDueToday = false;
            FilterDueThisWeek = false;
            FilterDateFrom = null;
            FilterDateTo = null;
            SelectedAssigneeFilter = null;

            // Clear the criteria (NotifyChanged is called internally by Clear())
            FilterCriteria?.Clear();
            ActivePreset = null;
            IsFilterDirty = false;

            // Update filter state to refresh badge
            UpdateFilterState();
            ApplyFilter();
        }

        private async void ExecuteSaveFilterPreset()
        {
            if (FilterCriteria == null || XamlRoot == null)
                return;

            var presetName = await ShowInputDialogAsync(
                FloweryLocalization.GetStringInternal("Kanban_Filter_SavePreset", "Save Filter Preset"),
                ActivePreset?.Name ?? string.Empty,
                FloweryLocalization.GetStringInternal("Kanban_Filter_PresetName", "Preset Name"));

            if (string.IsNullOrWhiteSpace(presetName))
                return;

            var sanitizedName = FlowKanbanFilterPreset.SanitizeName(presetName);
            if (string.IsNullOrEmpty(sanitizedName))
                return;

            FlowKanbanFilterPreset preset;
            if (ActivePreset != null)
            {
                preset = ActivePreset;
                preset.Name = sanitizedName;
                preset.Criteria = FilterCriteria.Clone();
                preset.LastUsedAt = DateTime.Now;
            }
            else
            {
                preset = new FlowKanbanFilterPreset
                {
                    Name = sanitizedName,
                    Criteria = FilterCriteria.Clone()
                };
                FilterPresets.Add(preset);
            }

            ActivePreset = preset;
            IsFilterDirty = false;
            SaveFilterPresets();
            UpdateHasFilterPresets();
        }

        private void ExecuteLoadFilterPreset(FlowKanbanFilterPreset? preset)
        {
            if (preset == null)
                return;

            FilterCriteria = preset.Criteria.Clone();
            ActivePreset = preset;
            preset.LastUsedAt = DateTime.Now;
            IsFilterDirty = false;
            SaveFilterPresets();
        }

        private void ExecuteDeleteFilterPreset(FlowKanbanFilterPreset? preset)
        {
            if (preset == null)
                return;

            FilterPresets.Remove(preset);
            if (ActivePreset?.Id == preset.Id)
            {
                ActivePreset = null;
            }
            SaveFilterPresets();
            UpdateHasFilterPresets();
        }

        private void ExecuteToggleFilterExpanded()
        {
            IsFilterExpanded = !IsFilterExpanded;
        }

        /// <summary>
        /// Applies filter criteria to all visible tasks.
        /// </summary>
        private void ApplyFilter()
        {
            ApplySearchFilter();
        }

        #endregion

        #region Preset Persistence

        private void LoadFilterPresets()
        {
            try
            {
                var lines = StateStorageProvider.Instance.LoadLines(FilterPresetsStorageKey);
                if (lines.Count == 0)
                    return;

                var json = string.Join(Environment.NewLine, lines);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var collection = JsonSerializer.Deserialize<FlowKanbanFilterPresetCollection>(
                    json,
                    FlowKanbanJsonContext.Default.FlowKanbanFilterPresetCollection);
                if (collection?.Presets != null)
                {
                    FilterPresets.Clear();
                    foreach (var preset in collection.Presets)
                    {
                        FilterPresets.Add(preset);
                    }
                }
                UpdateHasFilterPresets();
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        private void SaveFilterPresets()
        {
            try
            {
                var collection = new FlowKanbanFilterPresetCollection
                {
                    Presets = new List<FlowKanbanFilterPreset>(FilterPresets)
                };
                var json = JsonSerializer.Serialize(
                    collection,
                    FlowKanbanJsonContext.Default.FlowKanbanFilterPresetCollection);
                var lines = json.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                StateStorageProvider.Instance.SaveLines(FilterPresetsStorageKey, lines);
            }
            catch
            {
                // Ignore serialization errors
            }
        }

        private void UpdateHasFilterPresets()
        {
            HasFilterPresets = FilterPresets.Count > 0;
        }

        #endregion

        #region Enhanced Task Matching

        /// <summary>
        /// Checks if a task matches the current filter criteria.
        /// </summary>
        private bool IsTaskMatchWithCriteria(FlowTask task, FlowKanbanFilterCriteria? criteria)
        {
            if (criteria == null || !criteria.HasAnyFilter)
                return true;

            // Text search
            if (!string.IsNullOrWhiteSpace(criteria.TextQuery))
            {
                var query = criteria.TextQuery.Trim();
                var comparison = StringComparison.OrdinalIgnoreCase;
                var textMatch = false;

                if (int.TryParse(query, NumberStyles.Integer, CultureInfo.CurrentUICulture, out var workItemNumber)
                    && workItemNumber > 0
                    && task.WorkItemNumber == workItemNumber)
                {
                    textMatch = true;
                }

                if (criteria.MatchTitle && !string.IsNullOrWhiteSpace(task.Title) &&
                    task.Title.Contains(query, comparison))
                    textMatch = true;

                if (!textMatch && criteria.MatchDescription && !string.IsNullOrWhiteSpace(task.Description) &&
                    task.Description.Contains(query, comparison))
                    textMatch = true;

                if (!textMatch && criteria.MatchTags && !string.IsNullOrWhiteSpace(task.Tags) &&
                    task.Tags.Contains(query, comparison))
                    textMatch = true;

                if (!textMatch && criteria.MatchAssignee && !string.IsNullOrWhiteSpace(task.Assignee) &&
                    task.Assignee.Contains(query, comparison))
                    textMatch = true;

                if (!textMatch)
                    return false;
            }

            // Priority filter
            if (criteria.Priorities?.Count > 0 && !criteria.Priorities.Contains(task.Priority))
                return false;

            // Overdue filter
            if (criteria.ShowOnlyOverdue == true && !task.IsOverdue)
                return false;

            // Blocked filter
            if (criteria.ShowOnlyBlocked == true && !task.IsBlocked)
                return false;

            // Planned start date range
            if (criteria.PlannedStartRange?.HasValue == true)
            {
                if (!task.PlannedStartDate.HasValue ||
                    !criteria.PlannedStartRange.IsInRange(task.PlannedStartDate))
                    return false;
            }

            // Planned end date range
            if (criteria.PlannedEndRange?.HasValue == true)
            {
                if (!task.PlannedEndDate.HasValue ||
                    !criteria.PlannedEndRange.IsInRange(task.PlannedEndDate))
                    return false;
            }

            // Assignee filter
            if (criteria.AssigneeIds?.Count > 0)
            {
                var matchesAssignee = !string.IsNullOrWhiteSpace(task.AssigneeId) &&
                                      criteria.AssigneeIds.Contains(task.AssigneeId);

                if (!matchesAssignee)
                {
                    var assigneeName = task.Assignee?.Trim();
                    if (!string.IsNullOrWhiteSpace(assigneeName))
                    {
                        foreach (var assigneeId in criteria.AssigneeIds)
                        {
                            var option = AssigneeFilterOptions.FirstOrDefault(candidate =>
                                string.Equals(candidate.Id, assigneeId, StringComparison.Ordinal));
                            if (option != null &&
                                string.Equals(option.DisplayName, assigneeName, StringComparison.OrdinalIgnoreCase))
                            {
                                matchesAssignee = true;
                                break;
                            }
                        }
                    }
                }

                if (!matchesAssignee)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a column should be included based on filter criteria.
        /// </summary>
        private bool IsColumnIncluded(FlowKanbanColumnData column, FlowKanbanFilterCriteria? criteria)
        {
            if (criteria == null)
                return true;

            // Check archive exclusion
            if (criteria.ExcludeArchive && column.IsArchiveColumn)
                return false;

            // Check explicit column inclusion
            if (criteria.IncludedColumnIds?.Count > 0)
            {
                return criteria.IncludedColumnIds.Contains(column.Id);
            }

            return true;
        }

        #endregion
    }
}
