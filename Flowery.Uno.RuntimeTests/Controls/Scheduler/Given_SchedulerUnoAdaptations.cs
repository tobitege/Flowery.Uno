using System;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Controls.Scheduler.Contracts;
using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Utilities;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using static Flowery.Uno.RuntimeTests.Controls.Scheduler.SchedulerScope;

namespace Flowery.Uno.RuntimeTests.Controls.Scheduler;

/// <summary>Behavior that the Uno port adds or implements differently: click counting, the measured time gutter,
/// the Planner without weekends, sizing beside host values, external drop data, token overrides and validation
/// through bindings.</summary>
[TestClass]
public class Given_SchedulerUnoAdaptations
{
    [TestMethod]
    public async Task When_PressingTwice_OnlyAQuickPressAtTheSamePlaceIsADoubleClick()
    {
        using var scope = await CreateAsync(SchedulerView.Month);
        var edits = 0;
        var creations = 0;
        scope.Scheduler.AppointmentEditRequested += (_, _) => edits++;
        scope.Scheduler.AppointmentCreateRequested += (_, _) => creations++;
        var quick = DaisyScheduler.DoubleClickMicroseconds / 2;
        var frame = scope.Center(scope.Frame(scope.Appointment));
        await scope.DoubleClickAsync(frame, frame, quick);
        Assert.AreEqual(1, edits);
        await scope.DoubleClickAsync(frame, frame, DaisyScheduler.DoubleClickMicroseconds + 1);
        Assert.AreEqual(1, edits, "Too slow for a double-click");
        await scope.DoubleClickAsync(frame, new Point(frame.X + 6, frame.Y), quick);
        Assert.AreEqual(1, edits, "Moved too far for a double-click");

        var cell = scope.Descendants<Border>().First(border => border.Child is StackPanel { Children.Count: 1 } && border.ActualHeight > 40);
        var empty = scope.Point(cell, cell.ActualWidth / 2, cell.ActualHeight - 4);
        await scope.DoubleClickAsync(empty, empty, quick);
        Assert.AreEqual(1, creations);
    }

    [TestMethod]
    public async Task When_TwelveHourTimesAreLong_TheGutterFitsEveryLabelAndTheCorner()
    {
        using var scope = await CreateAsync(SchedulerView.Week);
        scope.Scheduler.Size = DaisySize.ExtraLarge;
        scope.Scheduler.ClockFormat = SchedulerClockFormat.TwelveHour;
        await scope.LayoutAsync();
        var gutter = scope.Descendants<Canvas>().Single(canvas => !canvas.AllowDrop && canvas.Children.OfType<TextBlock>().Count() == 24);
        foreach (var label in gutter.Children.OfType<TextBlock>())
            Assert.IsTrue(label.DesiredSize.Width < gutter.Width, $"{label.Text}: {label.DesiredSize.Width} >= {gutter.Width}");
        var corner = scope.Descendants<TextBlock>().Single(text => text.Text == "All day");
        var full = new TextBlock { Text = corner.Text, FontSize = corner.FontSize, FontFamily = corner.FontFamily };
        full.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.IsTrue(corner.ActualWidth + 1 >= full.DesiredSize.Width, $"Corner {corner.ActualWidth} < {full.DesiredSize.Width}");
    }

    [TestMethod]
    public async Task When_PlannerHidesWeekends_GesturesShiftTheStoredTimes()
    {
        var friday = Monday.AddDays(4);
        foreach (var (firstDay, appointmentStart, appointmentEnd, edge, minutes, expectedStart, expectedEnd) in new[]
        {
            // Spans a hidden Saturday: a move keeps all 48 hours.
            (DayOfWeek.Monday, friday.AddHours(12), friday.AddDays(2).AddHours(12), "move", 60, friday.AddHours(13), friday.AddDays(2).AddHours(13)),
            // Starts on a hidden Sunday: the drawn start is Monday 00:00, the stored start moves by the same hour.
            (DayOfWeek.Sunday, Monday.AddHours(-2), Monday.AddHours(2), "move", 60, Monday.AddHours(-1), Monday.AddHours(3)),
            (DayOfWeek.Sunday, Monday.AddHours(-2), Monday.AddHours(2), "start", 60, Monday.AddHours(-1), Monday.AddHours(2)),
            // The drawn end stops at Friday midnight; resizing it shifts the stored Sunday end.
            (DayOfWeek.Monday, friday.AddHours(12), friday.AddDays(2).AddHours(12), "end", -60, friday.AddHours(12), friday.AddDays(2).AddHours(11)),
        })
        {
            var label = $"{firstDay} {edge}";
            using var scope = await CreateAsync(SchedulerView.Planner);
            scope.Scheduler.FirstDayOfWeek = firstDay;
            scope.Scheduler.ShowWeekends = false;
            scope.Appointment.Start = appointmentStart;
            scope.Appointment.End = appointmentEnd;
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            if (edge != "move")
                scope.Scheduler.SelectedAppointment = scope.Appointment;
            await scope.LayoutAsync();
            DateTime? start = null, end = null;
            scope.Scheduler.AppointmentMoveRequested += (_, args) => (start, end) = (args.NewStart, args.NewEnd);
            scope.Scheduler.AppointmentResizeRequested += (_, args) => (start, end) = (args.NewStart, args.NewEnd);
            var frame = scope.Frame(scope.Appointment);
            var lane = (Canvas)frame.Parent;
            var ppm = lane.Width / (5 * 1440);
            var x = edge switch { "start" => 2, "end" => frame.ActualWidth - 2, _ => frame.ActualWidth / 2 };
            // Hours fit their start times, so a long bar can be wider than the viewport: show the grabbed point.
            await scope.BringIntoViewAsync(frame, new Rect(x - 1, 0, 2, frame.ActualHeight));
            var origin = scope.Point(frame, x, frame.ActualHeight / 2);
            await scope.DragAsync(origin, new(origin.X + minutes * ppm, origin.Y));
            Assert.AreEqual(expectedStart, start, label);
            Assert.AreEqual(expectedEnd, end, label);
        }
    }

    [TestMethod]
    public async Task When_PlannerHidesWeekends_DaysNeedNotBeContiguous_AndWeekendOnlyResourcesAreIdle()
    {
        using var scope = await CreateAsync(SchedulerView.Planner);
        var weekendOnly = new ResourceModel { Id = 2, Name = "Weekend" };
        var saturday = new AppointmentModel { Id = 2, Start = Monday.AddDays(5).AddHours(9), End = Monday.AddDays(5).AddHours(10), ResourceIds = { 2 } };
        // Wednesday, Thursday, Friday, Monday, Tuesday: the following Monday is the fourth axis day.
        scope.Appointment.Start = Monday.AddDays(7).AddHours(9);
        scope.Appointment.End = Monday.AddDays(7).AddHours(10);
        scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, Name = "Alex" }, weekendOnly };
        scope.Scheduler.Appointments = new[] { scope.Appointment, saturday };
        scope.Scheduler.FirstDayOfWeek = DayOfWeek.Wednesday;
        scope.Scheduler.SelectedDate = new DateTimeOffset(Monday.AddDays(2));
        scope.Scheduler.ShowWeekends = false;
        scope.Scheduler.HideIdleResources = true;
        await scope.LayoutAsync();
        var lanes = scope.Descendants<Canvas>().Where(canvas => canvas.AllowDrop).ToArray();
        Assert.HasCount(1, lanes, "The Saturday-only resource is idle on the shown dates");
        var lane = lanes[0];
        var ppm = lane.Width / (5 * 1440);
        Assert.AreEqual((3 * 1440 + 9 * 60) * ppm, Canvas.GetLeft(scope.Frame(scope.Appointment)), 0.5);
    }

    [TestMethod]
    public async Task When_HostSetsFontSizeOrCornerRadius_SizeChangesKeepThem_AndOneRenderFollows()
    {
        var previous = FlowerySizeManager.CurrentSize;
        using var scope = await CreateAsync(SchedulerView.Month);
        try
        {
            var renders = 0;
            scope.Scheduler.AppointmentRenderer = _ => { renders++; return null; };
            scope.Scheduler.ClearValue(DaisyScheduler.SizeProperty);
            await scope.LayoutAsync();
            renders = 0;
            FlowerySizeManager.ApplySize(DaisySize.Large);
            await scope.LayoutAsync();
            Assert.AreEqual(1, renders, "A size change renders once");
            Assert.AreEqual(18, scope.Scheduler.FontSize);
            scope.Scheduler.FontSize = 16;
            scope.Scheduler.CornerRadius = new CornerRadius(3);
            FlowerySizeManager.ApplySize(DaisySize.ExtraLarge);
            await scope.LayoutAsync();
            Assert.AreEqual(16, scope.Scheduler.FontSize);
            Assert.AreEqual(new CornerRadius(3), scope.Scheduler.CornerRadius);
            scope.Scheduler.ClearValue(Control.FontSizeProperty);
            scope.Scheduler.ClearValue(Control.CornerRadiusProperty);
            await scope.LayoutAsync();
            Assert.AreEqual(20, scope.Scheduler.FontSize);
            Assert.AreEqual(new CornerRadius(12), scope.Scheduler.CornerRadius);
        }
        finally { FlowerySizeManager.ApplySize(previous); }
    }

    [TestMethod]
    public async Task When_DroppingExternalItems_TheHostOperationIsKept_AndADropReleasesTheItem()
    {
        var package = new DataPackage { RequestedOperation = DataPackageOperation.Move };
        ExternalDropHandler.SetData(package, new Given_Scheduler.Droppable());
        Assert.AreEqual(DataPackageOperation.Move | DataPackageOperation.Copy, package.RequestedOperation);

        using var scope = await CreateAsync(SchedulerView.Timeline);
        var dropped = 0;
        scope.Scheduler.ExternalAppointmentDropped += (_, _) => dropped++;
        var lane = scope.Descendants<Canvas>().Single(canvas => canvas.AllowDrop);
        var data = package.GetView();
        Assert.IsTrue(scope.Scheduler.DropOnLane(lane, data, new Point(lane.Width / 2, 4)));
        Assert.AreEqual(1, dropped);
        Assert.IsFalse(ExternalDropHandler.TryGetItem(data, out _));
        Assert.IsFalse(scope.Scheduler.DropOnLane(lane, data, new Point(lane.Width / 2, 4)));
        Assert.AreEqual(1, dropped);
    }

    [TestMethod]
    public async Task When_HostOverridesToolbarAndTooltipTokens_TheSchedulerUsesThem()
    {
        using var scope = await CreateAsync(SchedulerView.Day);
        scope.Scheduler.Resources["DaisySchedulerToolbarMargin"] = new Thickness(20);
        scope.Scheduler.Resources["DaisySchedulerTooltipPadding"] = new Thickness(3);
        scope.Scheduler.Appointments = new[] { scope.Appointment };
        await scope.LayoutAsync();
        Assert.AreEqual(new Thickness(20), scope.Descendants<DaisySchedulerToolbar>().Single().Margin);
        Assert.AreEqual(new Thickness(3), ((ToolTip)ToolTipService.GetToolTip(scope.Frame(scope.Appointment))).Padding);
        var settings = scope.Descendants<Button>().Single(button => Equals(button.Tag, "Settings"));
        new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(settings).Invoke();
        await scope.LayoutAsync();
        var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(scope.Scheduler.XamlRoot);
        try
        {
            var presenter = popups.Select(popup => popup.Child).OfType<FlyoutPresenter>().Single();
            Assert.AreEqual(new Thickness(3), presenter.Padding);
        }
        finally
        {
            foreach (var popup in popups)
                popup.IsOpen = false;
        }
    }

    [TestMethod]
    public async Task When_ABindingDeliversAnInvalidValue_TheBindingSurvives_AndTheLastValidValueIsUsed()
    {
        using var scope = await CreateAsync(SchedulerView.Agenda);
        var source = new DaisyScheduler { VisibleDays = 2 };
        scope.Scheduler.SetBinding(DaisyScheduler.VisibleDaysProperty, new Binding { Source = source, Path = new PropertyPath(nameof(DaisyScheduler.VisibleDays)) });
        await scope.LayoutAsync();
        Assert.AreEqual(2, scope.Scheduler.VisibleDays);
        source.SetValue(DaisyScheduler.VisibleDaysProperty, 0);
        await scope.LayoutAsync();
        Assert.AreEqual(2, scope.Scheduler.VisibleDays);
        source.VisibleDays = 3;
        await scope.LayoutAsync();
        Assert.AreEqual(3, scope.Scheduler.VisibleDays);
        Assert.IsNotNull(scope.Scheduler.GetBindingExpression(DaisyScheduler.VisibleDaysProperty));
    }

    [TestMethod]
    public async Task When_AViewLoads_ItScrollsToTheStartOfTheWorkingDay()
    {
        // Day and Week start at 7:00, Timeline at 7:00 and Planner at the start of the work day, instead of midnight.
        foreach (var view in new[] { SchedulerView.Day, SchedulerView.Week, SchedulerView.Planner, SchedulerView.Timeline })
        {
            using var scope = await CreateAsync(view);
            var body = scope.Descendants<ScrollViewer>().Single();
            var offset = view is SchedulerView.Day or SchedulerView.Week ? body.VerticalOffset : body.HorizontalOffset;
            if (view is SchedulerView.Day or SchedulerView.Week)
                Assert.AreEqual(7 * 60 * 0.8, offset, 1, view.ToString());
            else
                Assert.IsTrue(offset > 0, $"{view} at {offset}");
        }
    }

    [TestMethod]
    public async Task When_AnAppointmentIsSelected_ItsFrameAndContentDoNotMove()
    {
        // Flowery.NET widens the frame border on selection; the overlay ring takes no layout space.
        foreach (var view in new[] { SchedulerView.Month, SchedulerView.Week, SchedulerView.Timeline })
        {
            using var scope = await CreateAsync(view);
            var frame = scope.Frame(scope.Appointment);
            var content = (FrameworkElement)((Panel)frame.Child).Children[0];
            var size = new Size(frame.ActualWidth, frame.ActualHeight);
            var position = content.TransformToVisual(scope.Scheduler).TransformPoint(default);
            await scope.ClickAsync(frame);
            Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment, view.ToString());
            Assert.AreSame(frame, scope.Frame(scope.Appointment), view.ToString());
            Assert.AreEqual(size, new Size(frame.ActualWidth, frame.ActualHeight), view.ToString());
            Assert.AreEqual(position, content.TransformToVisual(scope.Scheduler).TransformPoint(default), view.ToString());
        }
    }

    [TestMethod]
    public async Task When_ATouchSwipes_OnlyASelectedAppointmentIsDragged_AndATapSelects()
    {
        using var scope = await CreateAsync(SchedulerView.Week);
        AppointmentMoveArgs? moved = null;
        var created = 0;
        scope.Scheduler.AppointmentMoveRequested += (_, args) => moved = args;
        scope.Scheduler.AppointmentCreateRequested += (_, _) => created++;
        var center = scope.Center(scope.Frame(scope.Appointment));
        await scope.TouchDragAsync(center, new Point(center.X, center.Y + 60));
        Assert.IsNull(moved, "A swipe over an unselected appointment scrolls instead");
        Assert.IsNull(scope.Scheduler.SelectedAppointment, "A swipe selects nothing");
        var lane = (Canvas)scope.Frame(scope.Appointment).Parent;
        var children = lane.Children.Count;
        // A free slot below the appointment, inside the viewport.
        var frame = scope.Frame(scope.Appointment);
        var empty = scope.Point(lane, lane.ActualWidth / 2, Canvas.GetTop(frame) + frame.ActualHeight + 40);
        await scope.TouchDragAsync(empty, new Point(empty.X, empty.Y - 60));
        Assert.AreEqual(0, created, "A swipe over an empty slot scrolls instead");
        Assert.AreEqual(children, lane.Children.Count, "No creation preview stays behind");
        center = scope.Center(scope.Frame(scope.Appointment));
        await scope.TouchTapAsync(center);
        Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment, "A tap selects");
        Assert.IsNull(moved);
        center = scope.Center(scope.Frame(scope.Appointment));
        await scope.TouchDragAsync(center, new Point(center.X, center.Y + 60));
        Assert.IsNotNull(moved, "The next touch drags the selected appointment");
        Assert.IsTrue(moved.NewStart > scope.Appointment.Start);
    }

    [TestMethod]
    public async Task When_ATouchGrabsASelectedAppointmentNearItsEdge_ItResizes_WhereAMouseMoves()
    {
        foreach (var view in new[] { SchedulerView.Week, SchedulerView.Timeline })
        {
            using var scope = await CreateAsync(view);
            int moves = 0, resizes = 0;
            scope.Scheduler.AppointmentMoveRequested += (_, _) => moves++;
            scope.Scheduler.AppointmentResizeRequested += (_, _) => resizes++;
            await scope.ClickAsync(scope.Frame(scope.Appointment));
            var frame = scope.Frame(scope.Appointment);
            var horizontal = view == SchedulerView.Timeline;
            // 8 px before the end: inside the touch edge, outside the 5 px mouse edge.
            var grab = horizontal
                ? scope.Point(frame, frame.ActualWidth - 8, frame.ActualHeight / 2)
                : scope.Point(frame, frame.ActualWidth / 2, frame.ActualHeight - 8);
            var target = horizontal ? new Point(grab.X + 60, grab.Y) : new Point(grab.X, grab.Y + 60);
            await scope.TouchDragAsync(grab, target);
            Assert.AreEqual((0, 1), (moves, resizes), view + " touch");
            frame = scope.Frame(scope.Appointment);
            grab = horizontal
                ? scope.Point(frame, frame.ActualWidth - 8, frame.ActualHeight / 2)
                : scope.Point(frame, frame.ActualWidth / 2, frame.ActualHeight - 8);
            target = horizontal ? new Point(grab.X + 60, grab.Y) : new Point(grab.X, grab.Y + 60);
            await scope.DragAsync(grab, target);
            Assert.AreEqual((1, 1), (moves, resizes), view + " mouse");
        }
    }

    [TestMethod]
    public async Task When_ATouchSwipesOverAnEmptySlot_ItsPressIsNoDoubleTap()
    {
        // A swipe at an empty slot, then a quick tap there: the swipe's press does not count toward a double tap.
        using var scope = await CreateAsync(SchedulerView.Week);
        var created = 0;
        scope.Scheduler.AppointmentCreateRequested += (_, _) => created++;
        var frame = scope.Frame(scope.Appointment);
        var lane = (Canvas)frame.Parent;
        var empty = scope.Point(lane, lane.ActualWidth / 2, Canvas.GetTop(frame) + frame.ActualHeight + 40);
        await scope.TouchDragAsync(empty, new Point(empty.X, empty.Y - 60));
        await scope.TouchTapAsync(empty, elapsedMicroseconds: DaisyScheduler.DoubleClickMicroseconds / 2);
        Assert.AreEqual(0, created);
    }

    [TestMethod]
    public async Task When_ATouchSwipesOverAnUnselectedAppointment_ItsPressIsNoDoubleTap()
    {
        using var scope = await CreateAsync(SchedulerView.Week);
        var edits = 0;
        scope.Scheduler.AppointmentEditRequested += (_, _) => edits++;
        var quick = DaisyScheduler.DoubleClickMicroseconds / 2;
        var center = scope.Center(scope.Frame(scope.Appointment));
        await scope.TouchDragAsync(center, new Point(center.X, center.Y + 60));
        await scope.TouchTapAsync(center, elapsedMicroseconds: quick);
        Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment);
        Assert.AreEqual(0, edits);
        await scope.TouchTapAsync(center, elapsedMicroseconds: quick);
        Assert.AreEqual(1, edits, "The two taps form a double tap, the swipe's press takes no part");
    }

    [TestMethod]
    public async Task When_TheSystemTakesATouch_ItActsOnNothing_AndItsPressIsNoDoubleTap()
    {
        // A native scroll view that takes a swipe cancels the touch instead of reporting its moves and its release.
        var quick = DaisyScheduler.DoubleClickMicroseconds / 2;
        using (var scope = await CreateAsync(SchedulerView.Month))
        {
            var edits = 0;
            scope.Scheduler.AppointmentEditRequested += (_, _) => edits++;
            var chip = scope.Center(scope.Frame(scope.Appointment));
            scope.TouchDown(chip);
            scope.TouchCancel();
            scope.TouchUp(chip);
            Assert.IsNull(scope.Scheduler.SelectedAppointment, "A canceled tap selects nothing");
            await scope.TouchTapAsync(chip, elapsedMicroseconds: quick);
            await scope.TouchTapAsync(chip, elapsedMicroseconds: quick);
            Assert.AreEqual(1, edits, "Month");
        }
        using (var scope = await CreateAsync(SchedulerView.Week))
        {
            var created = 0;
            scope.Scheduler.AppointmentCreateRequested += (_, _) => created++;
            var frame = scope.Frame(scope.Appointment);
            var lane = (Canvas)frame.Parent;
            var empty = scope.Point(lane, lane.ActualWidth / 2, Canvas.GetTop(frame) + frame.ActualHeight + 40);
            scope.TouchDown(empty);
            scope.TouchCancel();
            await scope.TouchTapAsync(empty, elapsedMicroseconds: quick);
            Assert.AreEqual(0, created, "Week");
        }
    }

    [TestMethod]
    public async Task When_ATouchSwipesOverAnEmptyDay_TheSelectionStays_AndATapSelectsTheDay()
    {
        using var scope = await CreateAsync(SchedulerView.Month);
        var slots = 0;
        scope.Scheduler.EmptySlotSelected += (_, _) => slots++;
        await scope.TouchTapAsync(scope.Center(scope.Frame(scope.Appointment)));
        Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment);
        var frame = scope.Frame(scope.Appointment);
        var empty = scope.Point(frame, frame.ActualWidth / 2, frame.ActualHeight + 20);
        await scope.TouchDragAsync(empty, new Point(empty.X, empty.Y - 60));
        Assert.AreSame(scope.Appointment, scope.Scheduler.SelectedAppointment, "A swipe over a day keeps the selection");
        Assert.AreEqual(0, slots);
        await scope.TouchTapAsync(empty);
        Assert.IsNull(scope.Scheduler.SelectedAppointment, "A tap on a day selects the day");
        Assert.AreEqual(1, slots);
    }
}
