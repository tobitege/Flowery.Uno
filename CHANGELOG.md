<!-- markdownlint-disable MD022 MD024 -->
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.2] - 2026-02-12

- Refined size design tokens around a 4px grid and introduced mobile-specific sizing via `PlatformCompatibility.IsMobile`, keeping desktop/Windows ergonomics with the existing size presets.
- Increased mobile touch-target defaults for interactive controls and aligned global default size to `DaisySize.Medium`.
- Added line-height size tokens and applied token-driven text vertical alignment improvements in `DaisyButton`, `DaisyInput`, and `DaisyNumericUpDown` (including Android-specific centering adjustments).
- Updated `DaisyToggle` sizing behavior to enforce mobile-friendly minimum tap targets while preserving compact desktop rendering.
- Fixed browser build scripts to force browser-only framework evaluation and suppress EOL-check noise on preview SDKs (`build_browser.ps1`, `build_core.ps1`, `run-browser.ps1`).
- Migrated Android targets from `net9.0-android` to `net10.0-android36.0` across Flowery projects and Android build/run scripts.

## [0.1.1] - 2026-02-07

- Centralized versioning in `Directory.Build.props`.
- Improved Gallery startup responsiveness by prioritizing initial Home rendering and deferring restore navigation to heavy pages.
- Reduced Gallery startup overhead by deferring Kanban-specific initialization and resource loading until first Kanban usage.
- Library: mitigated repeated first-chance `NotImplementedException` churn for unsupported stroke APIs by caching runtime support checks.
- Common versions across projects updated.

## [0.1.0] - 2026-02-03

Initial port to Uno Platform (alpha!).
