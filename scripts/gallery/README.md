# Flowery.Uno Gallery — build and distribution scripts (demo)

Builds the installation packages of the **Flowery.Uno.Gallery** demo app for every supported environment.
Source: the repository that contains this folder (`scripts/gallery/`). Everything runs from macOS.

```bash
./0.build-all.sh          # everything, in sequence (run from scripts/gallery/)
```

| Script | Output (in `output/installer/`) | Notes |
|---|---|---|
| `build-android.sh` | `FloweryUnoGallery.apk` | Release, arm64 + x64, debug key signing by default |
| `build-dmg-macos.sh` | `FloweryUnoGallery.dmg` | arm64 + x64 in a single `.app`, Developer ID signed, not notarized |
| `build-windows.sh` | `FloweryUnoGallery_win-x64.zip`, `FloweryUnoGallery_win-arm64.zip` | portable, self-contained: run `Flowery.Uno.Gallery.exe` |
| `build-deb-linux.sh` | `flowery-uno-gallery_<ver>_amd64.deb`, `..._arm64.deb` | installs to `/opt/flowery-uno-gallery`, command `flowery-uno-gallery` |

Windows, macOS and Linux use the `net10.0-desktop` TFM (Skia), published with `-r <rid> --self-contained`.
Android uses `net10.0-android36.0`.
The version is read from `Directory.Build.props` in the repo (`Version`).

## Layout

```
output/                 ignored by git
  installer/            final packages
  publish/x64, arm64    Windows publish output (intermediate, zipped afterwards)
packaging/
  render-icon.sh        app_icon.svg -> PNG (rsvg-convert, or qlmanage + sips on macOS)
  linux/                build-deb.sh, control.in, .desktop
  macos/                Info.plist.in, Entitlements.plist
```

## Prerequisites (one time)

```bash
brew install create-dmg dpkg
```

- macOS: a `Developer ID Application: Stefano Del Furia (VZWMBQL256)` certificate in the Keychain.
- Android: the Android workload and SDK installed.

## Android: custom keystore

By default the APK is signed with the debug keystore (fine for a demo). To use your own:

```bash
export FLOWERY_ANDROID_KEYSTORE=/path/file.jks
export FLOWERY_ANDROID_KEY_ALIAS=alias
export FLOWERY_ANDROID_KEYSTORE_PASS=password
./build-android.sh
```

## Launch tests

Each package was started on a real or emulated target and checked for a running process and a visible window (or the main activity on Android).

| Package | Tested on | How | Result |
|---|---|---|---|
| macOS `.app` (arm64) | macOS 15, Apple Silicon | `open` on the `.app` | starts, window "Flowery.Uno Gallery" |
| macOS `.app` (x64) | same Mac, Rosetta | `app-x64` payload, and launcher forced to x86_64 | stays running; launcher picks the right payload |
| Linux `.deb` (arm64) | Ubuntu 24.04 arm64 (Parallels VM) | extracted, started on the desktop session | stays running |
| Windows zip (arm64) | Windows 11 ARM64 (Parallels VM) | unzipped, started in the user session | window "Flowery.Uno Gallery" visible |
| Windows zip (x64) | same VM, x64 emulation | same as above | window visible |
| Android `.apk` | Android 14 (API 34) arm64 emulator | `adb install`, launched | `MainActivity` resumed, no crash in logcat |
| Linux `.deb` (amd64) | not tested | package structure checked with `dpkg-deb --info` | - |
| Android `.apk` (x64 ABI) | not tested | the emulator is arm64 | - |

Not covered: a real Intel Mac, installing the `.deb` with `apt` (the launcher in `/usr/bin` and the menu entry), and navigation inside the app beyond the first screen.

## Release workflow (CI)

`.github/workflows/release.yml` runs these scripts and attaches the packages to the GitHub release:

| Job | Runner | Script | Produces |
|---|---|---|---|
| `build-gallery-desktop` | ubuntu | `build-windows.sh`, `build-deb-linux.sh` | Windows zips, Linux `.deb` |
| `build-gallery-android` | ubuntu | `build-android.sh` | `.apk` |
| `build-gallery-macos` | macos | `build-dmg-macos.sh` | `.dmg` |

Optional repository secrets (without them the build still works, just unsigned):

| Secret | Used for |
|---|---|
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASS` | sign the APK with your own keystore instead of the debug key |
| `MACOS_CERTIFICATE_P12_BASE64`, `MACOS_CERTIFICATE_PASSWORD`, `MACOS_DEV_ID_CERT` | Developer ID signing (`MACOS_DEV_ID_CERT` is the certificate name) |

Environment variables read by the scripts: `FLOWERY_DEV_ID_CERT` (certificate name; if it is not in the Keychain the app is signed ad-hoc), `FLOWERY_NOTARIZE`, `FLOWERY_NOTARY_PROFILE`, and the `FLOWERY_ANDROID_*` variables above.

## Notes

- Windows is distributed as a portable zip: there is no installer `.exe`, so no Windows machine or VM is needed.
- Windows and Linux builds are cross-published from macOS.
- macOS: notarization is disabled (`NOTARIZE=false`). On first launch from another Mac you may need right-click > Open.
- macOS: the `.app` contains two payloads (`Resources/app-arm64` and `Resources/app-x64`) and a launcher that picks the right one. A universal binary is not possible, because the framework assemblies are architecture-specific.
- Linux: the `.deb` was tested on Ubuntu 24.04 arm64 only. Install with `sudo apt install ./file.deb`, and make sure the file matches the machine architecture.
- Intermediate macOS and Android build folders go to `/tmp`, to keep OneDrive and other sync tools out of them.
