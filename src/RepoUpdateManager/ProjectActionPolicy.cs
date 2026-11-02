namespace RepoUpdateManager;

public static class ProjectActionPolicy
{
    public static string RepositoryUrl(TrackedItem item) => $"https://github.com/{item.Owner}/{item.Repository}";

    public static bool CanViewExactMatch(TrackedItem? item) =>
        item is { LatestUrl.Length: > 0 };

    public static bool CanPrepareUpdate(TrackedItem? item) =>
        item is { State: CheckState.UpdateAvailable, LatestVersion.Length: > 0, LatestUrl.Length: > 0 };

    public static string UpdateGuidance(TrackedItem item) =>
        $"Open the exact upstream match for {item.Name}, follow that project's published installation instructions, then verify the installed version locally before recording {item.LatestVersion} here. This tracker does not run installers or extract archives.";
}
