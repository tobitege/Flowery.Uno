using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls.Scheduler.Contracts;
using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Utilities;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using static Flowery.Uno.RuntimeTests.Controls.Scheduler.SchedulerScope;

namespace Flowery.Uno.RuntimeTests.Controls.Scheduler;

/// <summary>Counterparts of Flowery.NET's SchedulerGestureRegressionTests.</summary>
[TestClass]
public class Given_SchedulerGestures
{
    [TestMethod]
    public async Task When_GroupMoving_SelectedVisibleResourceLanesAreUsed_AfterHostRefresh()
    {
        foreach (var allDay in new[] { false, true })
        {
            using var scope = await CreateAsync(SchedulerView.Day);
            scope.Scheduler.ResourceKindFilter = ResourceKind.Room;
            scope.Scheduler.SchedulerResources = new[]
            {
                new ResourceModel { Id = "alex", Kind = ResourceKind.Person },
                new ResourceModel { Id = "bailey", Kind = ResourceKind.Person },
                new ResourceModel { Id = "a", Kind = ResourceKind.Room },
                new ResourceModel { Id = "equipment", Kind = ResourceKind.Equipment },
                new ResourceModel { Id = "b", Kind = ResourceKind.Room },
                new ResourceModel { Id = "c", Kind = ResourceKind.Room }
            };
            var first = new AppointmentModel { Id = 1, Start = Monday.AddHours(9), End = Monday.AddHours(10), ResourceIds = { "alex", "a", "b" } };
            var second = new AppointmentModel { Id = 2, Start = Monday.AddHours(11), End = Monday.AddHours(12), ResourceIds = { "alex", "a", "b" } };
            if (allDay)
            {
                second.IsAllDay = true;
                second.Start = Monday;
                second.End = Monday.AddDays(1);
            }
            scope.Scheduler.Appointments = new[] { first, second };
            await scope.LayoutAsync();
            await SelectFrameAsync(scope, scope.Frames(first).First());
            await SelectFrameAsync(scope, scope.Frames(second).Last(), multiple: true);
            // Replacement instances must retain which of their two room columns was selected.
            first = first.Clone();
            second = second.Clone();
            scope.Scheduler.Appointments = new[] { first, second };
            await scope.LayoutAsync();
            Assert.HasCount(2, scope.Scheduler.SelectedAppointments, allDay.ToString());
            AppointmentsMoveArgs? request = null;
            scope.Scheduler.AppointmentsMoveRequested += (_, args) => request = args;
            var source = scope.Frames(first).First();
            var target = scope.Frames(first).Last();
            var origin = scope.Point(source, source.ActualWidth / 2, source.ActualHeight / 3);
            var destination = scope.Point(target, target.ActualWidth / 2, target.ActualHeight / 3);
            await scope.DragAsync(origin, destination);
            Assert.IsNotNull(request, allDay.ToString());
            var firstItem = first;
            var secondItem = second;
            var movedFirst = request.Items.Single(item => ReferenceEquals(item.Appointment, firstItem));
            var movedSecond = request.Items.Single(item => ReferenceEquals(item.Appointment, secondItem));
            Assert.AreEqual("a", movedFirst.FromResourceId);
            Assert.AreEqual("b", movedFirst.ToResourceId);
            Assert.AreEqual("b", movedSecond.FromResourceId);
            Assert.AreEqual("c", movedSecond.ToResourceId);
            Assert.AreEqual(first.Start, movedFirst.NewStart);
            Assert.AreEqual(second.Start, movedSecond.NewStart);
            CollectionAssert.AreEqual(new object[] { "alex", "a", "b" }, first.ResourceIds.ToArray());
            CollectionAssert.AreEqual(new object[] { "alex", "a", "b" }, second.ResourceIds.ToArray());
        }
    }

    [TestMethod]
    [Ignore("Fails intermittently on Windows when the whole suite runs, not alone; the cause is not analyzed yet.")]
    public async Task When_DailyMoveCrossesMidnight_ItIsRejected_AndAValidMoveIsStillAccepted()
    {
        using var scope = await CreateAsync(SchedulerView.Day);
        scope.Appointment.Start = Monday.AddHours(22);
        scope.Appointment.End = Monday.AddDays(4).AddHours(23);
        scope.Appointment.SpanMode = AppointmentSpanMode.Daily;
        scope.Scheduler.Appointments = new[] { scope.Appointment };
        await scope.LayoutAsync();
        var frame = await BringIntoViewAsync(scope, scope.Frame(scope.Appointment));
        AppointmentMoveArgs? request = null;
        scope.Scheduler.AppointmentMoveRequested += (_, args) => request = args;
        var origin = scope.Center(frame);
        await scope.DragAsync(origin, new(origin.X, origin.Y + 48));
        Assert.IsNull(request);
        Assert.AreEqual(5, AppointmentOccurrences.InRange(scope.Appointment, Monday, Monday.AddDays(7)).Count());
        origin = scope.Point(frame, frame.ActualWidth / 2, frame.ActualHeight / 4);
        await scope.DragAsync(origin, new(origin.X, origin.Y - 48));
        Assert.IsNotNull(request);
        Assert.AreEqual(Monday.AddHours(21), request.NewStart);
        Assert.AreEqual(Monday.AddDays(4).AddHours(22), request.NewEnd);
    }

    [TestMethod]
    public async Task When_GroupEditHasAnInvalidDailyWindow_NothingIsPublished()
    {
        foreach (var resize in new[] { false, true })
        {
            using var scope = await CreateAsync(SchedulerView.Day);
            scope.Appointment.End = Monday.AddHours(12);
            var daily = new AppointmentModel
            {
                Id = 2, SpanMode = AppointmentSpanMode.Daily,
                Start = Monday.AddHours(resize ? 9.5 : 22),
                End = Monday.AddDays(4).AddHours(resize ? 10 : 23)
            };
            scope.Scheduler.Appointments = new[] { scope.Appointment, daily };
            await scope.LayoutAsync();
            await SelectFrameAsync(scope, scope.Frame(daily));
            await SelectFrameAsync(scope, scope.Frame(scope.Appointment), multiple: true);
            var requests = 0;
            scope.Scheduler.AppointmentsMoveRequested += (_, _) => requests++;
            scope.Scheduler.AppointmentsResizeRequested += (_, _) => requests++;
            var frame = await BringIntoViewAsync(scope, scope.Frame(scope.Appointment));
            var origin = scope.Point(frame, frame.ActualWidth / 2, resize ? frame.ActualHeight - 2 : frame.ActualHeight / 3);
            await scope.DragAsync(origin, new(origin.X, origin.Y + (resize ? -24 : 48)));
            Assert.AreEqual(0, requests, resize.ToString());
            Assert.AreEqual(5, AppointmentOccurrences.InRange(daily, Monday, Monday.AddDays(7)).Count());
            Assert.AreEqual(Monday.AddHours(9), scope.Appointment.Start);
            Assert.AreEqual(Monday.AddHours(12), scope.Appointment.End);
        }
    }

    [TestMethod]
    public async Task When_SpringGapIsClickedOrDroppedOn_ItIsRejected_WithoutBreakingLaterInput()
    {
        foreach (var view in new[] { SchedulerView.Planner, SchedulerView.Timeline })
        {
            using var scope = await CreateAsync(view);
            var date = new DateTime(2026, 3, 29);
            scope.Scheduler.SelectedDate = new DateTimeOffset(date);
            scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, TimeZoneId = "Europe/Berlin" } };
            scope.Scheduler.Appointments = Array.Empty<AppointmentModel>();
            scope.Scheduler.SingleClickCreates = true;
            await scope.LayoutAsync();
            var lane = scope.Descendants<Canvas>().Single(canvas => canvas.AllowDrop);
            var selections = 0;
            var creations = new List<AppointmentCreateArgs>();
            var drops = new List<ExternalDropArgs>();
            scope.Scheduler.EmptySlotSelected += (_, _) => selections++;
            scope.Scheduler.AppointmentCreateRequested += (_, args) => creations.Add(args);
            scope.Scheduler.ExternalAppointmentDropped += (_, args) => drops.Add(args);
            var gap = await HorizontalSlotPointAsync(scope, lane, date.AddHours(2.5));
            scope.MouseDown(gap);
            scope.MouseUp(gap);
            Assert.AreEqual(0, selections, view.ToString());
            Assert.IsEmpty(creations, view.ToString());
            var data = ExternalDropHandler.CreateDataPackage(new Given_Scheduler.Droppable()).GetView();
            var gapInLane = LanePoint(scope, lane, gap);
            Assert.AreEqual(DataPackageOperation.None, scope.Scheduler.DragOverLane(lane, data, gapInLane), view.ToString());
            Assert.IsFalse(scope.Scheduler.DropOnLane(lane, data, gapInLane), view.ToString());
            Assert.IsEmpty(drops, view.ToString());
            var valid = await HorizontalSlotPointAsync(scope, lane, date.AddHours(1.5));
            scope.MouseDown(valid);
            scope.MouseUp(valid);
            Assert.AreEqual(1, selections, view.ToString());
            Assert.AreEqual(date.AddMinutes(30), creations.Single().Start, view.ToString());
            var validInLane = LanePoint(scope, lane, valid);
            Assert.AreEqual(DataPackageOperation.Copy, scope.Scheduler.DragOverLane(lane, data, validInLane), view.ToString());
            Assert.IsTrue(scope.Scheduler.DropOnLane(lane, data, validInLane), view.ToString());
            Assert.AreEqual(date.AddMinutes(30), drops.Single().Start, view.ToString());
        }
    }

    [TestMethod]
    public async Task When_SpringGapMoveOrResize_NoChangeIsPublished()
    {
        foreach (var resize in new[] { false, true })
        {
            using var scope = await CreateAsync(SchedulerView.Timeline);
            var date = new DateTime(2026, 3, 29);
            scope.Scheduler.SelectedDate = new DateTimeOffset(date);
            scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, TimeZoneId = "Europe/Berlin" } };
            scope.Appointment.Start = date;
            scope.Appointment.End = date.AddMinutes(30);
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            if (resize)
                scope.Scheduler.SelectedAppointment = scope.Appointment;
            await scope.LayoutAsync();
            var requests = 0;
            scope.Scheduler.AppointmentMoveRequested += (_, _) => requests++;
            scope.Scheduler.AppointmentResizeRequested += (_, _) => requests++;
            var frame = await BringIntoViewAsync(scope, scope.Frame(scope.Appointment));
            var lane = (Canvas)frame.Parent;
            var origin = scope.Point(frame, resize ? frame.ActualWidth - 2 : frame.ActualWidth / 2, frame.ActualHeight / 2);
            await scope.DragAsync(origin, new(origin.X + lane.Width / 1440 * (resize ? 60 : 90), origin.Y));
            Assert.AreEqual(0, requests, resize.ToString());
            Assert.AreEqual(date, scope.Appointment.Start);
            Assert.AreEqual(date.AddMinutes(30), scope.Appointment.End);
        }
    }

    [TestMethod]
    public async Task When_HidingIdleResources_TheDisplayedTimeBasisIsUsed()
    {
        foreach (var view in new[] { SchedulerView.Day, SchedulerView.Week, SchedulerView.Planner, SchedulerView.Timeline })
        {
            using var scope = await CreateAsync(view);
            scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, TimeZoneId = "America/New_York" } };
            scope.Appointment.Start = Monday.AddHours(1);
            scope.Appointment.End = Monday.AddHours(2);
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            await scope.LayoutAsync();
            var horizontal = view is SchedulerView.Planner or SchedulerView.Timeline;
            Assert.AreEqual(horizontal ? 0 : 1, scope.Frames(scope.Appointment).Count(), view.ToString());
            scope.Scheduler.HideIdleResources = true;
            await scope.LayoutAsync();
            Assert.AreEqual(horizontal ? 0 : 1, scope.Frames(scope.Appointment).Count(), view.ToString());
            // Public occupancy queries retain their resource-local contract in every view.
            Assert.IsEmpty(scope.Scheduler.GetOccupiedResources(2026, 42));
            scope.Appointment.IsCancelled = true;
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            await scope.LayoutAsync();
            Assert.IsEmpty(scope.Frames(scope.Appointment), view.ToString());
        }
    }

    private static async Task SelectFrameAsync(SchedulerScope scope, Border frame, bool multiple = false)
    {
        frame = await BringIntoViewAsync(scope, frame);
        await scope.ClickAsync(frame, modifiers: multiple ? VirtualKeyModifiers.Control : VirtualKeyModifiers.None);
    }

    private static async Task<Border> BringIntoViewAsync(SchedulerScope scope, Border frame)
    {
        await scope.BringIntoViewAsync(frame);
        return frame;
    }

    private static Point LanePoint(SchedulerScope scope, Canvas lane, Point windowPoint) =>
        scope.Scheduler.XamlRoot!.Content!.TransformToVisual(lane).TransformPoint(windowPoint);

    private static async Task<Point> HorizontalSlotPointAsync(SchedulerScope scope, Canvas lane, DateTime time)
    {
        var origin = scope.Scheduler.SelectedDate.Date;
        var days = scope.Scheduler.CurrentView == SchedulerView.Planner ? 7 : 1;
        if (days == 7)
            origin = origin.AddDays(-((7 + (int)origin.DayOfWeek - (int)scope.Scheduler.FirstDayOfWeek) % 7));
        var x = ((time - origin).TotalMinutes + 1) * lane.Width / (days * 1440);
        await scope.BringIntoViewAsync(lane, new Rect(x - 4, 0, 8, lane.Height));
        return scope.Point(lane, x, lane.Height / 2);
    }
}
