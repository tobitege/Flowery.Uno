using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Controls.Scheduler.Contracts;
using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Rendering;
using Flowery.Localization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Flowery.Uno.RuntimeTests.Controls.Scheduler.SchedulerScope;

namespace Flowery.Uno.RuntimeTests.Controls.Scheduler;

/// <summary>Counterparts of Flowery.NET's SchedulerFeatureTests.</summary>
[TestClass]
public class Given_SchedulerFeatures
{
    [TestMethod]
    public async Task When_FormatOverridesChange_AppointmentAndTooltipRefresh_WithoutChangingUiLanguage()
    {
        foreach (var (format, clock, timePattern, cultureName, date) in new[]
        {
            (SchedulerDateTimeFormat.US, SchedulerClockFormat.Locale, "t", "en-US", "10/12/2026"),
            (SchedulerDateTimeFormat.UK, SchedulerClockFormat.Locale, "t", "en-GB", "12/10/2026"),
            (SchedulerDateTimeFormat.European, SchedulerClockFormat.Locale, "t", "de-DE", "12.10.2026"),
            (SchedulerDateTimeFormat.US, SchedulerClockFormat.TwentyFourHour, "HH:mm", "en-US", "10/12/2026"),
            (SchedulerDateTimeFormat.European, SchedulerClockFormat.TwelveHour, "h:mm tt", "de-DE", "12.10.2026")
        })
        {
            // Locale formatting follows the runtime's culture data, including ICU/NLS spacing.
            var time = Monday.AddHours(9).ToString(timePattern, CultureInfo.GetCultureInfo(cultureName));
            using var scope = await CreateAsync(SchedulerView.Day);
            var previousCulture = FloweryLocalization.CurrentCultureName;
            scope.Scheduler.DateTimeFormat = format;
            scope.Scheduler.ClockFormat = clock;
            await scope.LayoutAsync();
            var frame = scope.Frame(scope.Appointment);
            var context = (AppointmentContext)frame.DataContext;
            Assert.AreEqual(time, context.StartTime, cultureName);
            Assert.AreEqual(time, scope.Scheduler.FormatTime(Monday.AddHours(9)), cultureName);
            Assert.AreEqual(date, scope.Scheduler.FormatDate(Monday), cultureName);
            var tooltip = (StackPanel)((ToolTip)ToolTipService.GetToolTip(frame)).Content;
            Assert.IsTrue(tooltip.Children.OfType<TextBlock>().Any(label => label.Text?.Contains(date, StringComparison.Ordinal) == true), cultureName);
            Assert.AreEqual(previousCulture, FloweryLocalization.CurrentCultureName);
            Assert.AreEqual(Monday.AddHours(9), scope.Appointment.Start);
        }
    }

    [TestMethod]
    public async Task When_SingleLineChipsRender_TheyShowTheWholeStartTimeBesideTheSubject()
    {
        foreach (var (clock, expected) in new[] { (SchedulerClockFormat.TwelveHour, "9:00 AM"), (SchedulerClockFormat.TwentyFourHour, "09:00") })
        {
            using var scope = await CreateAsync(SchedulerView.Month);
            scope.Scheduler.DateTimeFormat = SchedulerDateTimeFormat.US;
            scope.Scheduler.ClockFormat = clock;
            await scope.LayoutAsync();
            var texts = Descendants<TextBlock>(scope.Frame(scope.Appointment)).Select(static text => text.Text).ToArray();
            CollectionAssert.Contains(texts, expected, clock + ": " + string.Join(" | ", texts));
            CollectionAssert.Contains(texts, "Install", clock.ToString());
        }
    }

    [TestMethod]
    public async Task When_WeekendsAreHidden_MonthAndWeekShowOnlyWorkdays()
    {
        using var scope = await CreateAsync(SchedulerView.Month);
        var saturday = new AppointmentModel { Id = 2, Subject = "Saturday", Start = Monday.AddDays(5).AddHours(9), End = Monday.AddDays(5).AddHours(10) };
        scope.Scheduler.Appointments = new[] { scope.Appointment, saturday };
        scope.Scheduler.ShowWeekends = false;
        await scope.LayoutAsync();
        var grid = scope.Descendants<Grid>().Single(control => control.RowDefinitions.Count == 7);
        Assert.HasCount(5, grid.ColumnDefinitions);
        Assert.HasCount(1, scope.Frames(scope.Appointment));
        Assert.IsEmpty(scope.Frames(saturday));
        // A week that starts on Sunday still shows Monday through Friday.
        scope.Scheduler.FirstDayOfWeek = DayOfWeek.Sunday;
        scope.Scheduler.CurrentView = SchedulerView.Week;
        await scope.LayoutAsync();
        Assert.AreEqual(5, scope.Descendants<Canvas>().Count(canvas => canvas.AllowDrop));
        Assert.IsEmpty(scope.Frames(saturday));
        scope.Scheduler.ShowWeekends = true;
        await scope.LayoutAsync();
        Assert.AreEqual(7, scope.Descendants<Canvas>().Count(canvas => canvas.AllowDrop));
        Assert.HasCount(1, scope.Frames(saturday));
    }

    [TestMethod]
    public async Task When_WeekendsAreHidden_ThePlannerAxisSkipsThem_AndGesturesReportRealTimes()
    {
        using var scope = await CreateAsync(SchedulerView.Planner);
        var friday = new AppointmentModel { Id = 2, Subject = "Friday", Start = Monday.AddDays(4).AddHours(9), End = Monday.AddDays(4).AddHours(10), ResourceIds = { 1 } };
        var saturday = new AppointmentModel { Id = 3, Subject = "Saturday", Start = Monday.AddDays(5).AddHours(9), End = Monday.AddDays(5).AddHours(10), ResourceIds = { 1 } };
        var overnight = new AppointmentModel { Id = 4, Subject = "Overnight", Start = Monday.AddDays(1).AddHours(12), End = Monday.AddDays(2).AddHours(12), ResourceIds = { 1 } };
        scope.Scheduler.Appointments = new[] { scope.Appointment, friday, saturday, overnight };
        scope.Scheduler.ShowWeekends = false;
        scope.Scheduler.SingleClickCreates = true;
        await scope.LayoutAsync();
        var lane = scope.Descendants<Canvas>().Single(canvas => canvas.AllowDrop);
        var ppm = lane.Width / (5 * 1440);
        Assert.IsEmpty(scope.Frames(saturday));
        Assert.AreEqual((4 * 1440 + 9 * 60) * ppm, Canvas.GetLeft(scope.Frame(friday)), 0.5);
        Assert.AreEqual(24 * 60 * ppm - 2, scope.Frame(overnight).Width, 0.5);

        AppointmentMoveArgs? move = null;
        scope.Scheduler.AppointmentMoveRequested += (_, args) => move = args;
        var frame = scope.Frame(friday);
        await scope.BringIntoViewAsync(frame);
        var origin = scope.Center(frame);
        await scope.DragAsync(origin, new(origin.X + 60 * ppm, origin.Y));
        Assert.IsNotNull(move);
        Assert.AreEqual(Monday.AddDays(4).AddHours(10), move.NewStart);
        Assert.AreEqual(Monday.AddDays(4).AddHours(11), move.NewEnd);

        AppointmentCreateArgs? create = null;
        scope.Scheduler.AppointmentCreateRequested += (_, args) => create = args;
        var slotX = (4 * 1440 + 14 * 60 + 1) * ppm;
        await scope.BringIntoViewAsync(lane, new Windows.Foundation.Rect(slotX - 4, 0, 8, lane.Height));
        var slot = scope.Point(lane, slotX, lane.ActualHeight - 2);
        scope.MouseDown(slot);
        scope.MouseUp(slot);
        Assert.IsNotNull(create);
        Assert.AreEqual(Monday.AddDays(4).AddHours(14), create.Start);
    }

    [TestMethod]
    public void When_EditorSlotsAreRequested_BothAlignedEndpointsAreIncluded_IndependentOfGridSnapping()
    {
        var scheduler = new DaisyScheduler
        {
            TimeSlotStart = TimeSpan.FromHours(7), TimeSlotEnd = TimeSpan.FromHours(17),
            TimeSlotInterval = TimeSpan.FromMinutes(30), TimeIncrement = new TimeIncrementConfig { MinorTick = TimeSpan.FromMinutes(15) }
        };
        var slots = scheduler.GetTimeSlots();
        Assert.HasCount(21, slots);
        Assert.AreEqual(TimeSpan.FromHours(7), slots[0]);
        Assert.AreEqual(TimeSpan.FromHours(17), slots[^1]);
        Assert.AreEqual(TimeSpan.FromHours(7.5), slots[1]);
        scheduler.TimeSlotEnd = TimeSpan.FromHours(17).Add(TimeSpan.FromMinutes(10));
        Assert.AreEqual(TimeSpan.FromHours(17), scheduler.GetTimeSlots()[^1]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => scheduler.TimeSlotInterval = TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.FromMinutes(30), scheduler.TimeSlotInterval);
        // XAML and bindings bypass the setter: the property keeps its last valid value.
        scheduler.SetValue(DaisyScheduler.TimeSlotIntervalProperty, TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.FromMinutes(30), scheduler.TimeSlotInterval);
        scheduler.TimeSlotEnd = TimeSpan.FromHours(6);
        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.GetTimeSlots());
    }

    [TestMethod]
    public async Task When_DailySpansRender_OccurrencesShow_AndSelectionReturnsTheOriginalSeries()
    {
        foreach (var (view, count) in new[]
        {
            (SchedulerView.Day, 1), (SchedulerView.Week, 3), (SchedulerView.Month, 3),
            (SchedulerView.Agenda, 3), (SchedulerView.Planner, 3), (SchedulerView.Timeline, 1)
        })
        {
            using var scope = await CreateAsync(view);
            scope.Appointment.SpanMode = AppointmentSpanMode.Daily;
            scope.Appointment.End = Monday.AddDays(2).AddHours(10);
            if (view == SchedulerView.Agenda)
                scope.Scheduler.VisibleDays = 3;
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            await scope.LayoutAsync();
            await scope.SettleScrollAsync();
            var frames = scope.Frames(scope.Appointment).ToArray();
            Assert.HasCount(count, frames, view.ToString());
            foreach (var frame in frames)
            {
                var context = (AppointmentContext)frame.DataContext;
                Assert.AreEqual(TimeSpan.FromHours(1), context.End - context.Start, view.ToString());
            }
            await scope.ClickAsync(frames[0]);
            Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment, view.ToString());
            Assert.AreEqual(Monday.AddDays(2).AddHours(10), scope.Appointment.End);
        }
    }

    [TestMethod]
    public async Task When_DraggingLaterDailyOccurrence_TheWholeSeriesMovesByTheSameDelta()
    {
        using var scope = await CreateAsync(SchedulerView.Day);
        scope.Appointment.SpanMode = AppointmentSpanMode.Daily;
        scope.Appointment.End = Monday.AddDays(2).AddHours(10);
        scope.Scheduler.SelectedDate = new DateTimeOffset(Monday.AddDays(2));
        scope.Scheduler.Appointments = new[] { scope.Appointment };
        await scope.LayoutAsync();
        AppointmentMoveArgs? request = null;
        scope.Scheduler.AppointmentMoveRequested += (_, args) => request = args;
        var origin = scope.Center(scope.Frame(scope.Appointment));
        await scope.DragAsync(origin, new(origin.X, origin.Y + 48));
        Assert.IsNotNull(request);
        Assert.AreSame(scope.Appointment, request.Appointment);
        Assert.AreEqual(Monday.AddHours(10), request.NewStart);
        Assert.AreEqual(Monday.AddDays(2).AddHours(11), request.NewEnd);
        Assert.AreEqual(Monday.AddHours(9), scope.Appointment.Start);
    }

    [TestMethod]
    public async Task When_PlannerRenders_EachAppointmentKeepsOneRow_AndDailyBarsStayOnTheirSeriesRow()
    {
        using var scope = await CreateAsync(SchedulerView.Planner);
        scope.Appointment.SpanMode = AppointmentSpanMode.Daily;
        scope.Appointment.End = Monday.AddDays(2).AddHours(10);
        var later = new AppointmentModel { Id = 2, Subject = "Later", Start = Monday.AddHours(14), End = Monday.AddHours(15), ResourceIds = { 1 } };
        var continuous = new AppointmentModel { Id = 3, Subject = "Continuous", Start = Monday.AddHours(7), End = Monday.AddDays(2).AddHours(17), ResourceIds = { 1 } };
        var allDay = new AppointmentModel { Id = 4, Subject = "All day", Start = Monday, End = Monday.AddDays(2), IsAllDay = true, ResourceIds = { 1 } };
        scope.Scheduler.Appointments = new[] { scope.Appointment, later, continuous, allDay };
        await scope.LayoutAsync();
        var dailyRows = scope.Frames(scope.Appointment).Select(Canvas.GetTop).Distinct().ToArray();
        Assert.HasCount(1, dailyRows);
        var rows = new[] { dailyRows[0], Canvas.GetTop(scope.Frame(later)), Canvas.GetTop(scope.Frame(continuous)), Canvas.GetTop(scope.Frame(allDay)) };
        Assert.AreEqual(4, rows.Distinct().Count());
        Assert.IsTrue(scope.Frame(continuous).Width > scope.Frame(later).Width);
    }

    [TestMethod]
    public async Task When_MonthRenders_EveryAppointmentKeepsItsOwnVisibleRow()
    {
        using var scope = await CreateAsync(SchedulerView.Month);
        var appointments = Enumerable.Range(1, 6).Select(index => new AppointmentModel { Id = index, Subject = "Event " + index,
            Start = Monday.AddHours(9), End = Monday.AddHours(10) }).ToArray();
        scope.Scheduler.Appointments = appointments;
        await scope.LayoutAsync();
        foreach (var appointment in appointments)
            Assert.HasCount(1, scope.Frames(appointment));
        var positions = appointments.Select(appointment => scope.Point(scope.Frame(appointment), 0, 0).Y).ToArray();
        Assert.AreEqual(6, positions.Distinct().Count());
    }

    [TestMethod]
    public async Task When_DayTimelineGroups_KindAndArbitraryCriteriaCombine_AndGroupsCollapseIndependently()
    {
        using var scope = await CreateAsync(SchedulerView.Timeline);
        var person = new ResourceModel { Id = 1, Name = "Alex", Kind = ResourceKind.Person, GroupValues = { ["Department"] = "Development" } };
        var room = new ResourceModel { Id = 2, Name = "Room", Kind = ResourceKind.Room, GroupValues = { ["Department"] = "Development" } };
        var all = new ResourceGroupModel { Id = "all", Name = "All", Kind = ResourceKind.Person };
        var department = new ResourceGroupModel { Id = "dev", Name = "Development", Kind = ResourceKind.Person, GroupType = "Department", GroupValue = "Development" };
        scope.Scheduler.SchedulerResources = new[] { person, room };
        scope.Scheduler.ResourceGroups = new[] { all, department };
        await scope.LayoutAsync();
        Assert.IsTrue(department.Matches(person));
        Assert.IsFalse(department.Matches(room));
        Assert.AreEqual(2, scope.Frames(scope.Appointment).Count());
        var button = scope.Descendants<DaisyButton>().Single(control => AutomationProperties.GetName(control) == "All");
        // The group heading is a button: raise its click as the pointer would.
        new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(button).Invoke();
        await scope.LayoutAsync();
        CollectionAssert.AreEqual(new object[] { "all" }, scope.Scheduler.CollapsedResourceGroupIds!.ToArray());
        Assert.HasCount(1, scope.Frames(scope.Appointment));
        Assert.AreEqual("All", all.Name);
        scope.Scheduler.ResourceKindFilter = ResourceKind.Room;
        await scope.LayoutAsync();
        Assert.IsEmpty(scope.Frames(scope.Appointment));
        scope.Scheduler.ResourceKindFilter = ResourceKind.Person;
        scope.Scheduler.CollapsedResourceGroupIds = Array.Empty<object>();
        await scope.LayoutAsync();
        Assert.AreEqual(2, scope.Frames(scope.Appointment).Count());
        var frame = scope.Frames(scope.Appointment).First();
        // Hours fit their start times, so the appointment can lie beyond the viewport.
        await scope.BringIntoViewAsync(frame);
        await scope.ClickAsync(frame);
        Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment);
        scope.Scheduler.Navigate(1);
        await scope.LayoutAsync();
        Assert.AreEqual(Monday.AddDays(1), scope.Scheduler.SelectedDate.Date);
        Assert.IsEmpty(scope.Frames(scope.Appointment));
    }

    [TestMethod]
    public async Task When_TimelineRenders_ResourceCellsAndAppointmentRowsHaveMatchingGeometry()
    {
        foreach (var size in new[] { DaisySize.ExtraSmall, DaisySize.Small, DaisySize.Medium, DaisySize.Large, DaisySize.ExtraLarge })
        {
            using var scope = await CreateAsync(SchedulerView.Timeline);
            scope.Scheduler.Size = size;
            var resources = Enumerable.Range(0, 3).Select(index => new ResourceModel { Id = index, Name = "P" + index, Kind = ResourceKind.Person }).ToArray();
            var appointments = Enumerable.Range(0, 6).Select(index => new AppointmentModel { Id = index, Subject = "A" + index,
                Start = Monday.AddHours(9 + index), End = Monday.AddHours(10 + index), ResourceIds = { index % 3 } }).ToArray();
            scope.Scheduler.SchedulerResources = resources;
            scope.Scheduler.ResourceGroups = new[] { new ResourceGroupModel { Id = "all", Name = "All", Kind = ResourceKind.Person } };
            scope.Scheduler.Appointments = appointments;
            await scope.LayoutAsync();
            var differences = new List<string>();
            foreach (var appointment in appointments)
            {
                var resource = resources[(int)appointment.ResourceIds[0]];
                var label = scope.Descendants<TextBlock>().Single(text => text.Text == resource.Name + " · " + appointment.Subject);
                var row = (Border)label.Parent;
                var cell = Ancestors(row).OfType<Border>().First();
                var frame = scope.Frame(appointment);
                var canvas = (Canvas)frame.Parent;
                var cellTop = cell.TransformToVisual(scope.Scheduler).TransformPoint(default).Y;
                var canvasTop = canvas.TransformToVisual(scope.Scheduler).TransformPoint(default).Y;
                var rowCenter = row.TransformToVisual(scope.Scheduler).TransformPoint(new(0, row.ActualHeight / 2)).Y;
                var frameCenter = frame.TransformToVisual(scope.Scheduler).TransformPoint(new(0, frame.ActualHeight / 2)).Y;
                if (Math.Abs(cellTop - canvasTop) > 0.5 || Math.Abs(cell.ActualHeight - canvas.ActualHeight) > 0.5 || Math.Abs(rowCenter - frameCenter) > 0.5)
                    differences.Add($"{appointment.Subject}: cell top/height {cellTop}/{cell.ActualHeight}, canvas {canvasTop}/{canvas.ActualHeight}, row centers {rowCenter}/{frameCenter}");
            }
            Assert.IsEmpty(differences, size + ": " + string.Join("; ", differences));
        }
    }

    [TestMethod]
    public async Task When_HostOverridesDesignTokens_SchedulerLabelsUseThem()
    {
        using var scope = await CreateAsync(SchedulerView.Timeline);
        scope.Scheduler.Size = DaisySize.Small;
        await scope.LayoutAsync();
        var originalWidth = ((Canvas)scope.Frame(scope.Appointment).Parent).Width;
        scope.Scheduler.Resources["DaisySizeSmallSecondaryFontSize"] = 10.5d;
        scope.Scheduler.Resources["DaisySpacingSmall"] = 12d;
        scope.Scheduler.Resources["DaisySchedulerTimelineMinimumHourWidth"] = 200d;
        scope.Appointment.Tiles.Add(new AppointmentDecoration { Text = "Token badge" });
        scope.Scheduler.Appointments = new[] { scope.Appointment };
        await scope.LayoutAsync();
        var label = scope.Descendants<TextBlock>().Single(text => text.Text == "Alex · Install");
        Assert.AreEqual(10.5, label.FontSize);
        var badge = Descendants<TextBlock>(scope.Frame(scope.Appointment)).Single(text => text.Text == "Token badge");
        Assert.AreEqual(10.5, badge.FontSize);
        Assert.AreEqual(40, ((Border)label.Parent).Height);
        Assert.IsTrue(((Canvas)scope.Frame(scope.Appointment).Parent).Width > originalWidth);
    }
}
