<!-- Supplementary documentation for FloweryAlarmHelpers -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

FloweryAlarmHelpers provides a complete alarm scheduling system with support for one-time and repeating alarms, snooze/dismiss functionality, and automatic triggering. The system is built around the `AlarmScheduler` class which manages a collection of `Alarm` objects.

## Core Classes

### Alarm

Represents a scheduled alarm with time, label, repeat pattern, and snooze settings.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `int` | auto | Unique identifier (auto-assigned) |
| `Time` | `TimeSpan` | - | Time of day for the alarm |
| `Label` | `string` | `""` | Display name/description |
| `Repeat` | `AlarmRepeat` | `None` | Repeat pattern (flags enum) |
| `IsEnabled` | `bool` | `true` | Whether alarm is active |
| `SnoozeDuration` | `TimeSpan` | `5 min` | How long snooze lasts |
| `MaxSnoozeCount` | `int` | `3` | Max snoozes allowed (0 = unlimited) |
| `State` | `AlarmState` | `Scheduled` | Current state (read-only) |
| `SnoozeCount` | `int` | `0` | Times snoozed since last ring |
| `NextRingTime` | `DateTimeOffset?` | - | Next scheduled trigger time |
| `CreatedAt` | `DateTimeOffset` | now | When alarm was created |

### AlarmRepeat (Flags Enum)

| Value | Description |
| --- | --- |
| `None` | One-time alarm |
| `Sunday` through `Saturday` | Individual days |
| `Daily` | Every day |
| `Weekdays` | Monday through Friday |
| `Weekends` | Saturday and Sunday |

```csharp
// Repeat on specific days
alarm.Repeat = AlarmRepeat.Monday | AlarmRepeat.Wednesday | AlarmRepeat.Friday;

// Use convenience values
alarm.Repeat = AlarmRepeat.Weekdays;  // Mon-Fri
alarm.Repeat = AlarmRepeat.Daily;     // Every day
```

### AlarmState

| State | Description |
| --- | --- |
| `Scheduled` | Waiting for trigger time |
| `Ringing` | Currently active, needs user action |
| `Snoozed` | Temporarily silenced |
| `Disabled` | Turned off |

### AlarmScheduler

Manages a collection of alarms with automatic scheduling.

| Method | Description |
| --- | --- |
| `AddAlarm(Alarm)` | Adds an existing alarm |
| `AddAlarm(time, label, repeat)` | Creates and adds a new alarm |
| `RemoveAlarm(Alarm)` | Removes an alarm |
| `RemoveAlarm(id)` | Removes by ID |
| `FindAlarm(id)` | Gets alarm by ID |
| `Snooze(alarm)` | Snoozes a ringing alarm |
| `Dismiss(alarm)` | Dismisses a ringing/snoozed alarm |
| `SetEnabled(alarm, bool)` | Enables/disables an alarm |
| `Dispose()` | Stops the scheduler |

| Event | Description |
| --- | --- |
| `AlarmTriggered` | Alarm started ringing |
| `AlarmSnoozed` | Alarm was snoozed |
| `AlarmDismissed` | Alarm was dismissed |

| Property | Type | Description |
| --- | --- | --- |
| `Alarms` | `ReadOnlyObservableCollection<Alarm>` | All scheduled alarms |

## Quick Examples

```csharp
// Create a scheduler
var scheduler = new AlarmScheduler();

// Add a simple one-time alarm (7:30 AM)
var wakeUp = scheduler.AddAlarm(
    TimeSpan.FromHours(7.5),  // 7:30 AM
    "Wake up!");

// Add a repeating weekday alarm (8:00 AM Mon-Fri)
var workAlarm = scheduler.AddAlarm(
    new TimeSpan(8, 0, 0),
    "Leave for work",
    AlarmRepeat.Weekdays);

// Add a custom alarm with snooze settings
var customAlarm = new Alarm(new TimeSpan(6, 0, 0), "Early morning")
{
    Repeat = AlarmRepeat.Daily,
    SnoozeDuration = TimeSpan.FromMinutes(10),
    MaxSnoozeCount = 5
};
scheduler.AddAlarm(customAlarm);

// Handle alarm events
scheduler.AlarmTriggered += (s, e) =>
{
    ShowNotification($"Alarm: {e.Alarm.Label}");
    PlayAlarmSound();
};

scheduler.AlarmSnoozed += (s, e) =>
{
    ShowToast($"Snoozed for {e.Alarm.SnoozeDuration.TotalMinutes} minutes");
};

scheduler.AlarmDismissed += (s, e) =>
{
    StopAlarmSound();
};

// User actions on ringing alarm
scheduler.Snooze(alarm);   // Postpone
scheduler.Dismiss(alarm);  // Stop completely

// Enable/disable alarm
scheduler.SetEnabled(alarm, false);  // Turn off
scheduler.SetEnabled(alarm, true);   // Turn back on

// Remove alarm
scheduler.RemoveAlarm(alarm);
// or by ID
scheduler.RemoveAlarm(alarm.Id);

// Check next ring time
var nextRing = alarm.CalculateNextRingTime();
var display = alarm.GetNextRingTimeDisplay();  // "In 2 hours 30 minutes"

// Clean up when done
scheduler.Dispose();
```

## XAML Integration

While AlarmScheduler is a code-behind component, you can bind to its `Alarms` collection:

```xml
<ListView ItemsSource="{x:Bind ViewModel.Scheduler.Alarms}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="helpers:Alarm">
            <Grid Padding="12">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                
                <StackPanel>
                    <TextBlock Text="{x:Bind Label}" FontWeight="SemiBold" />
                    <TextBlock Text="{x:Bind Time}" Opacity="0.7" />
                </StackPanel>
                
                <ToggleSwitch Grid.Column="1" 
                              IsOn="{x:Bind IsEnabled, Mode=TwoWay}" />
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

## Tips & Best Practices

- **Dispose the scheduler** when your page/window closes to stop the background timer
- **Handle AlarmTriggered** immediately - the alarm stays in `Ringing` state until you call `Snooze()` or `Dismiss()`
- **One-time alarms auto-disable** after being dismissed
- **Use MaxSnoozeCount** to prevent infinite snoozing (default is 3)
- **Bind to Alarms collection** for UI - it's an `ObservableCollection` so updates automatically
- **Check NextRingTime** to display countdown in your UI
- **Use GetNextRingTimeDisplay()** for user-friendly time remaining text
