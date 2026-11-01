using Flowery.Controls.Scheduler.Models;
using Flowery.Helpers;
using Flowery.Services;
using Microsoft.UI.Input;

namespace Flowery.Controls;

public partial class DaisyScheduler
{
    internal const double MinimumColumnZoom = 1;
    internal const double MaximumColumnZoom = 4;
    // A double tap on a column heading switches between the fitted columns and this zoom.
    private const double WideColumnZoom = 2;
    // One notch of the mouse wheel with Ctrl.
    private const double WheelZoomStep = 1.25;
    private const string ColumnZoomState = "columnZoom=";

    private readonly FloweryPinchTracker _pinch = new();
    // The zoom the shown columns have, to keep the content under the focus where it was when the zoom changes.
    private double _appliedZoom = MinimumColumnZoom;
    // The x in the body that a zoom keeps where it is: under the fingers or the pointer; null keeps the middle.
    private double? _zoomFocus;
    private double? _pinchStartZoom;
    private bool _loadingState;
    private bool _savePending;

    // Agenda and Calendar have no columns or hours to widen.
    private bool ZoomsColumns => CurrentView is not (SchedulerView.Agenda or SchedulerView.Calendar);

    private void ZoomByUser(double zoom, double focus)
    {
        _zoomFocus = focus;
        try
        {
            ColumnZoom = Math.Clamp(zoom, MinimumColumnZoom, MaximumColumnZoom);
        }
        finally
        {
            _zoomFocus = null;
        }
    }

    // Resizes the columns of the shown view for the new zoom and keeps the content under the focus where it was. Each
    // column and hour scales with the zoom, so the content does too.
    private void ZoomColumns()
    {
        var ratio = ColumnZoom / _appliedZoom;
        if (_bodyScroll is not { } scroll)
        {
            _appliedZoom = ColumnZoom;
            return;
        }
        var focus = _zoomFocus ?? scroll.ViewportWidth / 2;
        var anchor = scroll.HorizontalOffset + focus;
        UpdateViewGeometry();
        RescaleHours();
        _appliedZoom = ColumnZoom;
        scroll.UpdateLayout();
        scroll.ChangeView(Math.Max(0, anchor * ratio - focus), null, null, true);
    }

    /// <summary>The window position of the pinch on the scheduler body, as x in the body's viewport.</summary>
    private double BodyX(Point windowPosition) =>
        _bodyScroll is { } scroll ? scroll.TransformToVisual(null).Inverse.TransformPoint(windowPosition).X : 0;

    private void OnPinchStarted()
    {
        // What the first finger began is neither a tap nor a drag.
        ForgetPress();
        CancelGesture();
        _pinchStartZoom = ZoomsColumns ? ColumnZoom : null;
    }

    private void OnPinchChanged(double scale, Point middle)
    {
        if (_pinchStartZoom is { } start && _bodyScroll is not null)
            ZoomByUser(start * scale, BodyX(middle));
    }

    private void OnPinchEnded() => _pinchStartZoom = null;

    // Tests call the tracker as the pointer events do.
    internal FloweryPinchTracker Pinch => _pinch;

    // On the content, below the scroll viewer: a handled wheel does not scroll it.
    private void AttachZoomWheel(UIElement content) => content.PointerWheelChanged += OnZoomWheel;

    private void OnZoomWheel(object? sender, PointerRoutedEventArgs e)
    {
        if (_bodyScroll is { } scroll && ZoomByWheel(e.KeyModifiers, e.GetCurrentPoint(scroll).Properties.MouseWheelDelta,
                e.GetCurrentPoint(scroll).Position.X))
            e.Handled = true;
    }

    /// <summary>Ctrl (Command) and the wheel zoom by <see cref="WheelZoomStep"/> per notch at the pointer's x in the
    /// body; returns whether the wheel zoomed.</summary>
    internal bool ZoomByWheel(Windows.System.VirtualKeyModifiers modifiers, int delta, double x)
    {
        if (!HasControl(modifiers) || delta == 0 || !ZoomsColumns || _bodyScroll is null)
            return false;
        ZoomByUser(ColumnZoom * Math.Pow(WheelZoomStep, delta / 120d), x);
        return true;
    }

    // A double click on a column heading, or on touch and pen two taps in a row, switches between the fitted columns
    // and wide ones. A touch is decided on release like any other (see TouchTap): a swipe that starts on a heading
    // only scrolls.
    private void AttachColumnHeading(FrameworkElement heading)
    {
        heading.Tag = ColumnHeading.Instance;
        heading.PointerPressed += OnColumnHeadingPressed;
    }

    /// <summary>The tag of a column heading, which RoutePointerPressed recognizes.</summary>
    private sealed class ColumnHeading
    {
        public static readonly ColumnHeading Instance = new();
    }

    private void OnColumnHeadingPressed(object? sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement heading && PressColumnHeading(heading, new PointerInput(e)))
            e.Handled = true;
    }

    private bool PressColumnHeading(FrameworkElement heading, PointerInput input)
    {
        if (!input.IsLeftButton || !ZoomsColumns)
            return false;
        var clickCount = ClickCount(ColumnHeading.Instance, input);
        if (input.IsTouch)
        {
            CancelGesture();
            _press = new TouchTap
            {
                PointerId = input.PointerId, IsTouch = true, WindowPosition = input.WindowPosition, ClickCount = clickCount,
                Heading = true
            };
        }
        else if (clickCount == 2)
            ToggleWideColumns(input.WindowPosition);
        return true;
    }

    private void ToggleWideColumns(Point windowPosition) =>
        ZoomByUser(ColumnZoom > MinimumColumnZoom ? MinimumColumnZoom : WideColumnZoom, BodyX(windowPosition));

    // The right edge of a column heading resizes the column with the mouse, as in a table; every column follows, since
    // the zoom is the same for all. The left edge of the dragged column stays.
    private sealed record EdgeDrag(uint PointerId, Pointer? Pointer, UIElement Grip, double PressX, double Width,
        double Zoom, double Left);

    private EdgeDrag? _edgeDrag;

    private double ColumnEdgeWidth => SpacingXS + LineThickness * 2;

    /// <summary>Puts a grip with the resize cursor on the right edge of a heading cell, over its content. The
    /// scheduler's own move and release handlers follow the drag.</summary>
    private void AddColumnEdge(Border heading)
    {
        var grip = new ColumnEdgeGrip(heading) { Width = ColumnEdgeWidth, HorizontalAlignment = HorizontalAlignment.Right };
        grip.PointerPressed += OnColumnEdgePressed;
        grip.PointerCaptureLost += (_, _) => _edgeDrag = null;
        var content = heading.Child;
        heading.Child = null;
        var cell = new Grid();
        if (content is not null)
            cell.Children.Add(content);
        cell.Children.Add(grip);
        heading.Child = cell;
    }

    private void OnColumnEdgePressed(object? sender, PointerRoutedEventArgs e)
    {
        if (sender is ColumnEdgeGrip grip && PressColumnEdge(grip, new PointerInput(e)))
            e.Handled = true;
    }

    private bool PressColumnEdge(ColumnEdgeGrip grip, PointerInput input)
    {
        // A finger needs the whole heading; only the mouse resizes at the edge.
        if (input.IsTouch || !input.IsLeftButton || !ZoomsColumns || _bodyScroll is not { } scroll)
            return false;
        CancelGesture();
        _edgeDrag = new EdgeDrag(input.PointerId, input.Pointer, grip, input.WindowPosition.X, grip.Heading.ActualWidth,
            ColumnZoom, grip.Heading.TransformToVisual(scroll).TransformPoint(default).X);
        if (input.Pointer is { } pointer)
            grip.CapturePointer(pointer);
        return true;
    }

    private bool MoveColumnEdge(PointerInput input)
    {
        if (_edgeDrag is not { } drag || drag.PointerId != input.PointerId || drag.Width <= 0)
            return false;
        var width = Math.Max(1, drag.Width + input.WindowPosition.X - drag.PressX);
        ZoomByUser(drag.Zoom * width / drag.Width, drag.Left);
        return true;
    }

    private bool ReleaseColumnEdge(uint pointerId)
    {
        if (_edgeDrag is not { } drag || drag.PointerId != pointerId)
            return false;
        _edgeDrag = null;
        if (drag.Pointer is { } pointer)
            drag.Grip.ReleasePointerCapture(pointer);
        return true;
    }

    /// <summary>The right edge of a column heading, which shows the resize cursor.</summary>
    private sealed partial class ColumnEdgeGrip : Grid
    {
        public ColumnEdgeGrip(FrameworkElement heading)
        {
            Heading = heading;
            // Transparent, not null: a null background is not hit.
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }

        public FrameworkElement Heading { get; }
    }

    private string? StorageKey => string.IsNullOrWhiteSpace(StateKey) ? null : StateStorageKeyHelper.BuildScopedKey("scheduler", StateKey);

    // The settings are a convenience: a storage that cannot be read or written keeps the current ones.
    private void LoadState()
    {
        if (!_attached || StorageKey is not { } key)
            return;
        IReadOnlyList<string> lines;
        try
        {
            lines = StateStorageProvider.Instance.LoadLines(key);
        }
        catch (Exception)
        {
            return;
        }
        foreach (var line in lines)
        {
            if (!line.StartsWith(ColumnZoomState, StringComparison.Ordinal) ||
                !double.TryParse(line.AsSpan(ColumnZoomState.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom) ||
                !double.IsFinite(zoom))
                continue;
            _loadingState = true;
            try
            {
                ColumnZoom = zoom;
            }
            finally
            {
                _loadingState = false;
            }
        }
    }

    private void SaveState()
    {
        if (_loadingState || _savePending || !_attached || StorageKey is null)
            return;
        // A pinch changes the zoom many times; one write after it is enough.
        _savePending = true;
        if (DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, WriteState) != true)
            WriteState();
    }

    private void WriteState()
    {
        _savePending = false;
        if (StorageKey is not { } key)
            return;
        try
        {
            StateStorageProvider.Instance.SaveLines(key, new[] { ColumnZoomState + ColumnZoom.ToString("R", CultureInfo.InvariantCulture) });
        }
        catch (Exception)
        {
            // Kept for this session only.
        }
    }
}
