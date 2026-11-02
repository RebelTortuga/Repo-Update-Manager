namespace RepoUpdateManager;

public enum TrackingKind { Repository, AiSkill, Plugin, ManualTool }
public enum UpstreamKind { GitHubRelease, GitHubTag, GitHubCommit, NpmPackage }
public enum CheckState { Unknown, Current, UpdateAvailable, Error, Offline, RateLimited }

public sealed record TrackedItem(
    long Id, string Name, TrackingKind Kind, string Owner, string Repository, UpstreamKind Upstream,
    string? StreamPattern, string? InstalledVersion, string? LocalPath, string? LatestVersion,
    string? LatestUrl, CheckState State, string? Detail, DateTimeOffset? LastCheckedUtc);

public sealed record UpstreamResult(string Version, string Url, DateTimeOffset PublishedUtc, string Source);

