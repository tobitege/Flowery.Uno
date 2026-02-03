# Logging (FloweryLogging)

## Purpose

FloweryLogging provides a single, app-wide logging pipeline that works across all Uno targets. It gives:

- A central API for structured log entries with levels and categories.
- Platform-specific output (Android Logcat, iOS/macOS Debug output, WASM console, desktop Debug).
- Runtime control for enabling/disabling logs and filtering by level.
- A stable surface for UI log viewers via events.

This replaces ad-hoc Debug.WriteLine calls and keeps debug logging configurable per build or runtime.

## Architecture

Core types live in `Flowery.Uno/Services`:

- `FloweryLogging`: static logging entry point and configuration.
- `FloweryLogEntry`: structured log message (timestamp, level, category, message, exception).
- `FloweryLogLevel`: Trace/Debug/Info/Warning/Error/Critical.
- `FloweryLoggingOptions`: runtime flags controlling output.
- `IFloweryLogSink`: interface for adding new sinks.
- `FloweryPlatformLogSink`: default sink that writes to the platform output.

Convenience wrappers:

- `FloweryDiagnostics` (category: "Flowery")
- `GalleryDiagnostics` (category: "Gallery")

## Defaults (Debug/Release)

Defaults are set in `FloweryLogging` and are build-configuration based:

- Debug builds:
  - `Enabled = true`
  - `MinimumLevel = Debug`
- Release builds:
  - `Enabled = false`
  - `MinimumLevel = Warning`

Formatting flags (default):

- `IncludeTimestamp = false`
- `IncludeLevel = true`
- `IncludeCategory = true`

## Platform Output

`FloweryPlatformLogSink` sends logs to:

- Android: Logcat (`Android.Util.Log`)
- iOS/macOS: `System.Diagnostics.Debug.WriteLine`
- WASM: `Console.WriteLine`
- Others: `System.Diagnostics.Debug.WriteLine`

## Usage

### 1) Basic logging

```csharp
using Flowery.Services;

FloweryLogging.Log(FloweryLogLevel.Info, "Theme", "Theme applied");
```

### 2) Log with exception

```csharp
try
{
    // Work
}
catch (Exception ex)
{
    FloweryLogging.Log(FloweryLogLevel.Error, "Carousel", "Failed to load shader", ex);
}
```

### 3) Configure runtime flags

```csharp
FloweryLogging.Configure(options =>
{
    options.Enabled = true;
    options.MinimumLevel = FloweryLogLevel.Info;
    options.IncludeTimestamp = true;
    options.IncludeCategory = true;
});
```

### 4) Add a custom sink

```csharp
public sealed class UiLogSink : IFloweryLogSink
{
    public void Log(FloweryLogEntry entry)
    {
        var text = FloweryLogging.Format(entry);
        // Push to UI, file, or telemetry (keep this fast and safe).
    }
}

FloweryLogging.AddSink(new UiLogSink());
```

### 5) Subscribe to all log entries

```csharp
FloweryLogging.Logged += (_, entry) =>
{
    var text = FloweryLogging.Format(entry);
    // Use for log window, telemetry, etc.
};
```

### 6) Use existing helpers

```csharp
using Flowery.Helpers;
using Flowery.Uno.Gallery;

FloweryDiagnostics.Log("ComboBox created");
GalleryDiagnostics.Log("LayoutExamples failed to load");
```

## Options Reference

`FloweryLoggingOptions`:

- `Enabled` (bool): master switch.
- `MinimumLevel` (FloweryLogLevel): filters log levels.
- `IncludeTimestamp` (bool): include ISO timestamp in formatted output.
- `IncludeLevel` (bool): include `[Level]` in formatted output.
- `IncludeCategory` (bool): include `[Category]` in formatted output.

## Categories

Categories allow filtering and grouping in sinks or UI.

- `FloweryDiagnostics` logs to category: `Flowery`.
- `GalleryDiagnostics` logs to category: `Gallery`.

Use categories to split concerns (e.g. `Theme`, `Carousel`, `Sidebar`).

## Best Practices

- Keep logging fast; do not block the UI thread.
- Avoid throwing exceptions from sinks (they are caught and ignored).
- Use categories to keep logs searchable.
- Avoid logging secrets or user data.
- Prefer `FloweryLogging.Log` over `Debug.WriteLine` to keep outputs consistent.

## Notes

- `FloweryLogging.Format(entry)` uses current options to render log strings.
- `FloweryLogging.Options` returns the live options instance; use `Configure` for updates.
- In release builds, logging is off by default; explicitly enable it for diagnostics.
