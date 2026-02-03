# Flowery.Uno Build Scripts

PowerShell scripts for building and running Flowery.Uno projects. All scripts follow a consistent output format with clear terminal, head identification, and build summaries.

## Projects

| Project                        | Description                    |
| ------------------------------ | ------------------------------ |
| `Flowery.Uno`                  | Core UI component library      |
| `Flowery.Uno.Gallery`          | Shared Gallery UI library      |
| `Flowery.Uno.Gallery.Windows`  | WinUI Gallery app (Windows)    |
| `Flowery.Uno.Gallery.Desktop`  | Skia desktop Gallery head      |
| `Flowery.Uno.Gallery.Browser`  | WebAssembly Gallery head       |
| `Flowery.Uno.Gallery.Android`  | Android Gallery head           |

---

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- PowerShell 7+ (`pwsh`)
- Visual Studio 2022 with MSBuild (for `build_desktop.ps1` and `build_windows.ps1`)

---

## Local CI with gh act (optional)

Run Linux GitHub Actions jobs locally using the `nektos/gh-act` extension (requires Docker Desktop).

**Install:**

```powershell
gh extension install nektos/gh-act
```

**Usage (CI build-library):**

```powershell
gh act -W .github/workflows/ci.yml -j build-library
```

**Usage (Release build-library without publish/tag):**

```powershell
@'
{"inputs":{"version":"0.0.0","skip_nuget":"true","skip_tag":"true"}}
'@ | Set-Content -Encoding utf8 .\.github\act-release.json
gh act workflow_dispatch -W .github/workflows/release.yml -j build-library -e .\.github\act-release.json
```

**Notes:**

- The repo includes `.actrc`, which provides a default `ubuntu-latest` runner image and forces `linux/amd64`, so you do not need to pass `-P` manually.
- `act` cannot run `windows-latest` jobs; use GitHub Actions or a Windows runner for the Gallery Windows build.

---

## Architecture

### Shared Core Module

All MSBuild-based build scripts use `build_core.ps1`, a shared module providing:

- **`Initialize-BuildEnvironment`** - Sets up logging, clears terminal, locates MSBuild
- **`Invoke-ProjectBuild`** - Builds a project, captures output, counts warnings
- **`Write-BuildSummary`** - Outputs formatted summary with per-project timing and warnings
- **`Start-GalleryApp`** - Launches the Gallery app after build

### Output Format

All scripts follow a consistent output pattern:

```bash
Building Flowery.Uno {HeadName} projects ({Configuration})...

Building Flowery.Uno...
Building Flowery.Uno.Gallery.Windows...

═══════════════════════════════════════════════════════════
 BUILD SUMMARY
═══════════════════════════════════════════════════════════

  [OK] Flowery.Uno                               00:13.05 (12 warnings)
  [OK] Flowery.Uno.Gallery.Windows               00:04.65

  Total time: 00:17.73
  Projects built: 2
  Total warnings: 12
  Log: D:\github\Flowery.Uno\logs\desktop_build.log

All builds completed successfully.

Starting Gallery app...
Started: Flowery.Uno.Gallery.Windows.exe
```

---

## Build Scripts

### build_desktop.ps1 / build_desktop.sh

Builds the library and Desktop (Skia) Gallery head using MSBuild. Runs the Gallery app by default.

```powershell
pwsh ./scripts/build_desktop.ps1              # Build and run
pwsh ./scripts/build_desktop.ps1 -NoRun       # Build only
pwsh ./scripts/build_desktop.ps1 -Rebuild     # Clean rebuild
pwsh ./scripts/build_desktop.ps1 -VerboseOutput     # Show full build output
pwsh ./scripts/build_desktop.ps1 -Configuration Release
```

**Git Bash:** `./scripts/build_desktop.sh`

| Parameter        | Type   | Default | Description                                |
| ---------------- | ------ | ------- | ------------------------------------------ |
| `-Configuration` | string | `Debug` | Build configuration (`Debug` or `Release`) |
| `-NoRun`         | switch | false   | Skip running the app after build           |
| `-Rebuild`       | switch | false   | Perform a clean rebuild                    |
| `-VerboseOutput` | switch | false   | Print build output to console              |

**Log file:** `logs/desktop_build.log`

---

### build_windows.ps1 / build_windows.sh

Builds only the Windows (WinUI) head using MSBuild.

```powershell
pwsh ./scripts/build_windows.ps1
pwsh ./scripts/build_windows.ps1 -NoRun -Rebuild
pwsh ./scripts/build_windows.ps1 -Configuration Release
```

**Git Bash:** `./scripts/build_windows.sh`

| Parameter        | Type   | Default | Description                                |
| ---------------- | ------ | ------- | ------------------------------------------ |
| `-Configuration` | string | `Debug` | Build configuration (`Debug` or `Release`) |
| `-NoRun`         | switch | false   | Skip running the app after build           |
| `-Rebuild`       | switch | false   | Perform a clean rebuild                    |
| `-VerboseOutput` | switch | false   | Print build output to console              |

**Log file:** `logs/windows_build.log`

---

### publish_windows.ps1

Publishes the Windows (WinUI) Gallery head using dotnet publish.

```powershell
pwsh ./scripts/publish_windows.ps1
pwsh ./scripts/publish_windows.ps1 -Configuration Release
pwsh ./scripts/publish_windows.ps1 -SelfContained:$false -OutputDir ./publish-win
```

| Parameter        | Type   | Default    | Description                                        |
| --------------- | ------ | ---------- | -------------------------------------------------- |
| `-Configuration` | string | `Release`  | Publish configuration (`Debug` or `Release`)       |
| `-RuntimeIdentifier` | string | `win-x64` | Runtime identifier                                 |
| `-SelfContained` | bool   | `true`     | Publish self-contained output                       |
| `-OutputDir`     | string | `publish`  | Output directory                                    |
| `-VerboseOutput` | switch | false      | Print output to console (still writes log)          |

**Log file:** `logs/windows_publish.log`

---

### build_android.ps1 / build_android.sh

Builds the Android head using dotnet build.

```powershell
pwsh ./scripts/build_android.ps1
pwsh ./scripts/build_android.ps1 -Rebuild
pwsh ./scripts/build_android.ps1 -AndroidSdkDirectory "C:\Users\YOU\AppData\Local\Android\Sdk"
```

**Git Bash:** `./scripts/build_android.sh`

| Parameter              | Type   | Default | Description                                |
| ---------------------- | ------ | ------- | ------------------------------------------ |
| `-Configuration`       | string | `Debug` | Build configuration (`Debug` or `Release`) |
| `-AndroidSdkDirectory` | string | auto    | Android SDK path (or set ANDROID_SDK_ROOT) |
| `-NoRun`               | switch | false   | Skip showing run instructions              |
| `-Rebuild`             | switch | false   | Perform a clean rebuild                    |
| `-VerboseOutput`       | switch | false   | Print build output to console              |

**Log file:** `logs/android_build.log`

---

### build_browser.ps1 / build_browser.sh

Builds the library and Browser (WebAssembly) Gallery head. Runs the Gallery app via local server by default.

```powershell
pwsh ./scripts/build_browser.ps1              # Build and run
pwsh ./scripts/build_browser.ps1 -NoRun       # Build only
pwsh ./scripts/build_browser.ps1 -Rebuild     # Clean rebuild
pwsh ./scripts/build_browser.ps1 -VerboseOutput     # Show full build output
pwsh ./scripts/build_browser.ps1 -Configuration Release
```

**Git Bash:** `./scripts/build_browser.sh`

| Parameter        | Type   | Default | Description                                |
| ---------------- | ------ | ------- | ------------------------------------------ |
| `-Configuration` | string | `Debug` | Build configuration (`Debug` or `Release`) |
| `-NoRun`         | switch | false   | Skip running the app after build           |
| `-Rebuild`       | switch | false   | Perform a clean rebuild                    |
| `-VerboseOutput` | switch | false   | Print build output to console              |

**Log file:** `logs/browser_build.log`

---

### build_all.ps1 / build_all.sh

Builds all heads (Windows, Desktop, Browser, Android) plus the shared libraries.

```powershell
pwsh ./scripts/build_all.ps1
pwsh ./scripts/build_all.ps1 -Rebuild
pwsh ./scripts/build_all.ps1 -RestoreWorkloads
pwsh ./scripts/build_all.ps1 -Configuration Release
```

**Git Bash:** `./scripts/build_all.sh`

| Parameter              | Type   | Default | Description                                |
| ---------------------- | ------ | ------- | ------------------------------------------ |
| `-Configuration`       | string | `Debug` | Build configuration (`Debug` or `Release`) |
| `-AndroidSdkDirectory` | string | auto    | Android SDK path (or set ANDROID_SDK_ROOT) |
| `-RestoreWorkloads`    | switch | false   | Restore .NET workloads before building     |
| `-NoRun`               | switch | false   | (reserved for future use)                  |
| `-Rebuild`             | switch | false   | Perform a clean rebuild                    |
| `-VerboseOutput`       | switch | false   | Print build output to console              |

**Log file:** `logs/all_build.log`

---

## Run Scripts

All run scripts clear the terminal and display which head they're running. Desktop/Windows run in a console window and wait by default so exceptions stay visible.

### run-desktop.ps1

Builds and launches the Skia desktop head. Runs via `cmd /k` and waits by default; use `-NoWait` to detach.

```powershell
pwsh ./scripts/run-desktop.ps1
pwsh ./scripts/run-desktop.ps1 -Configuration Release -Rebuild
pwsh ./scripts/run-desktop.ps1 -NoWait
```

---

### run-windows.ps1

Builds and launches the WinUI Windows head. Runs via `cmd /k` and waits by default; use `-NoWait` to detach.

```powershell
pwsh ./scripts/run-windows.ps1
pwsh ./scripts/run-windows.ps1 -Configuration Release -Rebuild
pwsh ./scripts/run-windows.ps1 -NoWait
```

---

### run-browser.ps1

Runs the WASM head and optionally opens the browser.

```powershell
pwsh ./scripts/run-browser.ps1
pwsh ./scripts/run-browser.ps1 -NoBrowser -Rebuild
pwsh ./scripts/run-browser.ps1 -Port 8080
```

---

### run-android.ps1

Builds and deploys the Android head to an emulator/device.

```powershell
pwsh ./scripts/run-android.ps1
pwsh ./scripts/run-android.ps1 -DeviceName "emulator-5554" -Rebuild
```

---

## build_nuget.ps1

Builds the NuGet package for `Flowery.Uno`.

```powershell
pwsh ./scripts/build_nuget.ps1
pwsh ./scripts/build_nuget.ps1 -OutputDir ./packages
```

| Parameter        | Type   | Default   | Description                        |
| ---------------- | ------ | --------- | ---------------------------------- |
| `-Configuration` | string | `Release` | Build configuration                |
| `-OutputDir`     | string | `./nupkg` | Output directory for `.nupkg` file |

---

## Troubleshooting

### Build fails with missing SDK

Ensure you have .NET 9.0 SDK installed:

```powershell
dotnet --version
# Should show 9.0.x
```

### MSBuild not found

The `build_desktop.ps1` and `build_windows.ps1` scripts require Visual Studio 2022 with MSBuild. Ensure VS is installed and includes the ".NET desktop development" workload.

### Executable not found after build

Check that the build succeeded and look in the `bin/` directory at the repo root.

### View build errors

When builds fail, errors and warnings are automatically extracted from the log and displayed in the console. Full logs are saved to the `logs/` folder with head-specific filenames.
