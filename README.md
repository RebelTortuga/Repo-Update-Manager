# Repo-Update-Manager

**Created by RebelTortuga.** A free, open-source update manager for repositories, AI skills, plugins, and developer tools.

Source: <https://github.com/RebelTortuga/Repo-Update-Manager>

Windows is the first supported target; Avalonia keeps the UI architecture portable to Linux and macOS. This is an early release: it checks many sources, while only a deliberately narrow, confirmed Git fast-forward method can update automatically.

## MVP capabilities

- Add a GitHub project with an optional installed version and local path.
- Classify it as a repository, AI skill, plugin, or manual tool.
- Use **Check All Now** to check every tracked source while preserving the selected stream, then open the repository or exact matched release/tag/commit from the selected row.
- Compare conventional version tags (including `v` prefixes) and report current, update-available, unavailable, offline, rate-limited, or error states. Draft and prerelease GitHub releases are intentionally excluded in this MVP.
- Track and check exact npm package identities; npm updates are manual in v0.1.0.
- Persist tracked projects in a local SQLite database under `%LOCALAPPDATA%\RebelTortuga\RepoUpdateManager\updates.db`.
- Export the tracked-project list to JSON.
- Import validated JSON exports, edit entries, and remove tracking entries without changing local installations.
- Optionally register a Windows Task Scheduler job for a weekly Monday check; disabling it removes only the app's own task.
- On Windows, scheduled checks show a native shell notification when updates are found.
- For an available update, open upstream instructions and, only after independently installing and verifying it, explicitly record the verified version. The app never runs an installer, extracts an archive, or marks an update installed on its own.
- **Update selected** and **Update All** support only clean local Git worktrees that exactly match a tracked GitHub repository using the latest-commit source. npm packages, release assets, ZIPs, plugins, skills, and other unsupported entries are shown as manual and cannot execute through an update action. The plan shows every affected project, detected commit, required local write/network permissions, and unsupported reason before an explicit confirmation.

Weekly scheduling is opt-in. The app creates only `RebelTortuga Repo-Update-Manager Weekly Check`, never changes ghru or any other scheduled task. Scheduled runs call the same read-only GitHub check and notify only when an update is available.

## Run locally

Requirements: .NET 10 SDK and Internet access for the first package restore.

```powershell
dotnet restore .\RepoUpdateManager.sln
dotnet test .\RepoUpdateManager.sln
dotnet run --project .\src\RepoUpdateManager\RepoUpdateManager.csproj
```

To test the stream-filter use case, add `https://github.com/pbakaus/impeccable`, set the installed version you recorded locally, and use `skill-v*` as the release stream. The app queries public GitHub release data and does not inspect or modify that local installation.

## Portable Windows release

Download `Repo-Update-Manager-v0.1.0-win-x64-portable.zip`, extract it anywhere, and run `Repo-Update-Manager.exe`. No .NET SDK or runtime installation is required. The portable app stores its SQLite database and settings in `%LOCALAPPDATA%\RebelTortuga\RepoUpdateManager`, so deleting the extracted folder does not delete your tracked data. To remove user data, delete that folder separately.

## Current limits

- npm installation/update is intentionally disabled for this release. npm package checks and package links remain available, but update manually using your preferred package-management workflow.
- Release assets, ZIPs, package managers, installed executables, and generic GitHub repositories remain manual. They have no verified installation contract in this version, so there is intentionally no Download or arbitrary Install command.
- GitHub releases, tags, and the latest commit can be selected for a tracked item. Release filtering is only meaningful for releases/tags.
- GitHub authentication and packaged installers remain release-plan work.
- Public GitHub checks are subject to GitHub rate limits; the app surfaces that condition rather than pretending the result is current.

## Architecture

- `src/RepoUpdateManager` — Avalonia desktop UI, SQLite persistence, GitHub update source, comparison/filter logic.
- `tests/RepoUpdateManager.Tests` — unit coverage for version and stream-filter rules.
- All upstream access is isolated in `GitHubUpdateSource`; platform-specific notifications and schedulers can be added behind interfaces without changing the comparison or storage model.

## Future Development

- Review-first automatic discovery for installed tools, skills, plugins, MCP servers, and local repositories.
- Expanded safe update methods for skills, plugins, MCP servers, and package managers after installation targeting and verification are proven.
- More interface filtering, activity history, and accessibility refinements.
- Signed installer packaging and eventual Linux/macOS validation.

## License

This project is licensed under the [MIT License](LICENSE). Copyright (c) 2026 RebelTortuga.

