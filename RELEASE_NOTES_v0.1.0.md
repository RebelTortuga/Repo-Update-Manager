# Repo-Update-Manager v0.1.0

First Windows pre-release of Repo-Update-Manager, a free, open-source update manager created by RebelTortuga for repositories, AI skills, plugins, and developer tools.

## Included

- GitHub release, tag, and commit tracking with stream filters.
- Local SQLite storage, import/export, edit/delete, optional weekly checks, and Windows notifications.
- Exact repository and matched-source links with manual update guidance.
- Confirmed, clean-worktree Git fast-forward updates for commit-tracked repositories only.
- npm package identity/version checks. npm installation is intentionally disabled in v0.1.0.

## Safety

Update All never installs npm packages, release assets, ZIPs, skills, plugins, or arbitrary executables. Unsupported entries are identified as manual. Git updates require a clean matching origin, explicit confirmation, disabled hooks/submodules/filters, and post-update commit verification.

## Portable Windows

Extract `Repo-Update-Manager-v0.1.0-win-x64-portable.zip` and launch `Repo-Update-Manager.exe`. It stores data in `%LOCALAPPDATA%\RebelTortuga\RepoUpdateManager`; removing the extracted folder does not remove that data.

## License

This release and its source are available under the MIT License. The portable ZIP includes `LICENSE`.

## Known limits

This is an early release. Automatic discovery, broader skill/plugin/MCP support, additional safe update methods, installer packaging, and Linux/macOS validation are future work.
