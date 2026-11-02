using Xunit;

namespace RepoUpdateManager.Tests;

public sealed class ProjectActionPolicyTests
{
    private static readonly TrackedItem Available = new(1, "Tool", TrackingKind.ManualTool, "owner", "repo", UpstreamKind.GitHubRelease, "tool-v*", "1.0.0", null, "tool-v2.0.0", "https://github.com/owner/repo/releases/tag/tool-v2.0.0", CheckState.UpdateAvailable, "GitHub release", DateTimeOffset.UtcNow);

    [Fact]
    public void Update_actions_require_a_checked_update_with_an_exact_url()
    {
        Assert.True(ProjectActionPolicy.CanViewExactMatch(Available));
        Assert.True(ProjectActionPolicy.CanPrepareUpdate(Available));
        Assert.False(ProjectActionPolicy.CanPrepareUpdate(Available with { State = CheckState.Current }));
        Assert.False(ProjectActionPolicy.CanPrepareUpdate(Available with { LatestUrl = null }));
    }

    [Fact]
    public void Guidance_never_claims_to_install_or_extract_an_update()
    {
        var guidance = ProjectActionPolicy.UpdateGuidance(Available);
        Assert.Contains("verify", guidance, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not run installers", guidance, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://github.com/owner/repo", ProjectActionPolicy.RepositoryUrl(Available));
    }
}
