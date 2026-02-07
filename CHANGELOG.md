<!-- markdownlint-disable MD022 MD024 -->
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-02-07

- Centralized versioning in `Directory.Build.props`.
- Improved Gallery startup responsiveness by prioritizing initial Home rendering and deferring restore navigation to heavy pages.
- Reduced Gallery startup overhead by deferring Kanban-specific initialization and resource loading until first Kanban usage.
- Library: mitigated repeated first-chance `NotImplementedException` churn for unsupported stroke APIs by caching runtime support checks.
- Common versions across projects updated.

## [0.1.0] - 2026-02-03

Initial port to Uno Platform (alpha!).
