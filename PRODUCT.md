# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Stack

Inferred from the explicit brief: .NET 10, Avalonia UI, and SQLite. The first supported platform is Windows; the architecture is intended for Linux and macOS later.

## Users

People who manually track GitHub repositories, AI skills, plugins, scripts, and portable tools. They need a dependable view of what they have, what upstream has released, and whether attention is needed.

## Product Purpose

RebelTortuga Repo Update Manager is a read-only desktop tracker for selected development projects and manually installed components. It compares a user-entered installed version with the relevant upstream GitHub release, tag, or commit stream, then reports available updates without installing anything.

## Positioning

Unlike a package manager or a Windows-app updater, it treats a project as the unit of tracking: a repository can be monitored even when it is not installed, and a single repository can have separately filtered component streams such as `skill-v*`.

## Operating Context

Users add entries manually, optionally record an installed version and local path, select an upstream strategy, run an on-demand check or accept weekly background checks, and follow a notification back to the project’s release page.

## Capabilities and Constraints

- First MVP scope: GitHub repositories, AI skills, plugins, and manual tools.
- GitHub release streams can be filtered by wildcard patterns, including `skill-v*`.
- Tags and latest commits are supported when releases are not the source of truth.
- Stored data uses SQLite; checks are read-only.
- No automatic installations, no ghru dependency, no modification of existing ghru tasks or configuration.
- Offline, rate-limit, and upstream errors must be reported without destroying prior results.
- Weekly checking is the default; users control scheduling.
- Import/export is a secondary MVP capability.

## Brand Commitments

The application is named RebelTortuga Repo Update Manager. The requested interface is AppGeek-inspired: polished, practical, scannable, and recognizably a desktop manager rather than a generic software updater.

## Evidence on Hand

The source brief supplies no logo, screenshots, customer claims, or release telemetry. Demonstration entries must be labelled as sample data and must not be presented as live checks.

## Product Principles

1. Track projects, do not manage installations.
2. Make the source and comparison method visible.
3. Preserve user control over paths, schedules, and updates.
4. Prefer honest status over silent failure.
5. Keep the first release narrow enough to trust.

## Accessibility & Inclusion

Keyboard-accessible controls, semantic labels, clear focus states, and readable status distinctions are required for the desktop UI.
