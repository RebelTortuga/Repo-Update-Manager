# Repo-Update-Manager release plan

Repo-Update-Manager is created by RebelTortuga.

## 0.1 — Windows MVP

Acceptance criteria:

1. Add, retain, and export manually tracked GitHub repositories, skills, plugins, and tools.
2. Check public GitHub release streams on demand without changing any tracked installation, including a visible Check All Now action.
3. Correctly filter a stream such as `skill-v*` and compare common SemVer-style versions.
4. Preserve last known data through offline, malformed-source, and API-rate-limit failures.
5. Safely fast-forward only clean, local Git worktrees that exactly match a GitHub commit-tracked item, after a review-and-confirm plan and post-update commit verification. npm installation is disabled for v0.1.0; other methods remain manual.
6. Build, unit test, and smoke-test the UI on a Windows machine with the .NET 10 SDK.

## 0.2 — remaining management work

- Add sortable filters and a details/release-link view.
- Add an in-app activity history and a cross-platform scheduler/notification adapter for Linux and macOS.
- Add a GitHub token setting stored using the platform credential store; never export the token.

## 0.3 — distribution and portability

- Package signed Windows releases and document verification hashes. v0.1.0 ships an unsigned self-contained portable Windows x64 ZIP; an installer remains future work.
- Test Avalonia builds on Linux and macOS; replace only the notification/scheduling adapters where necessary.
- Add a real import migration test suite and HTTP fixture tests for GitHub response/error cases.

## Release safeguards

- Never execute an update without in-app per-run confirmation, and never run repository scripts, hooks, submodules, installers, or archive extraction.
- Do not alter ghru, its scheduled task, or any existing repository.
- Validate each packaged build against public GitHub rate-limit, offline, and filtered-release fixtures before publication.

