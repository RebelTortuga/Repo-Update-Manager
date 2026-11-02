using Xunit;

namespace RepoUpdateManager.Tests;

public sealed class LocalGitUpdateServiceTests
{
    [Fact]
    public async Task Apply_verifies_the_checked_commit_after_a_safe_fast_forward()
    {
        var path = Path.Combine(Path.GetTempPath(), $"repo-update-{Guid.NewGuid():N}"); Directory.CreateDirectory(path);
        try
        {
            var item = new TrackedItem(1, "Tool", TrackingKind.AiSkill, "owner", "repo", UpstreamKind.GitHubCommit, null, "aaaaaaaaaaaa", path, "bbbbbbbbbbbb", "https://github.com/owner/repo/commit/b", CheckState.UpdateAvailable, null, null);
            var runner = new FakeRunner(["https://github.com/owner/repo.git", "", "aaaaaaaaaaaaaaaa", "https://github.com/owner/repo.git", "", "aaaaaaaaaaaaaaaa", "Already up to date.", "bbbbbbbbbbbbbbbb"]);
            var service = new LocalGitUpdateService(runner); var plan = await service.PlanAsync(item, CancellationToken.None); var result = await service.ApplyAsync(plan, CancellationToken.None);
            Assert.True(plan.IsEligible); Assert.Equal(UpdateResultState.UpdatedVerified, result.State); Assert.Equal("bbbbbbbbbbbb", result.VerifiedVersion);
            Assert.Contains(runner.Calls, call => call.Contains("--no-verify"));
        }
        finally { Directory.Delete(path); }
    }

    [Fact]
    public async Task Dirty_worktree_is_unsupported_and_never_pulled()
    {
        var path = Path.Combine(Path.GetTempPath(), $"repo-update-{Guid.NewGuid():N}"); Directory.CreateDirectory(path);
        try
        {
            var item = new TrackedItem(1, "Tool", TrackingKind.Plugin, "owner", "repo", UpstreamKind.GitHubCommit, null, null, path, "bbbb", null, CheckState.UpdateAvailable, null, null);
            var runner = new FakeRunner(["https://github.com/owner/repo.git", " M README.md"]); var plan = await new LocalGitUpdateService(runner).PlanAsync(item, CancellationToken.None);
            Assert.False(plan.IsEligible); Assert.DoesNotContain(runner.Calls, call => call.Contains("pull"));
        }
        finally { Directory.Delete(path); }
    }

    [Fact]
    public async Task Npm_packages_are_manual_and_never_execute_install_commands()
    {
        var item = new TrackedItem(1, "Codex", TrackingKind.ManualTool, "npm", "@openai/codex", UpstreamKind.NpmPackage, null, "1.0.0", "C:\\npm\\codex", "2.0.0", null, CheckState.UpdateAvailable, null, null);
        var runner = new FakeRunner([]); var service = new LocalGitUpdateService(runner);
        var plan = await service.PlanAsync(item, CancellationToken.None); var result = await service.ApplyAsync(plan, CancellationToken.None);
        Assert.False(plan.IsEligible); Assert.Equal(UpdateResultState.Unsupported, result.State); Assert.Empty(runner.Calls);
    }

    private sealed class FakeRunner(IEnumerable<string> output) : ICommandRunner
    {
        private readonly Queue<string> _output = new(output); public List<string> Calls { get; } = [];
        public Task<CommandResult> RunAsync(string fileName, string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken) { Calls.Add(string.Join(' ', arguments)); return Task.FromResult(new CommandResult(0, _output.Dequeue(), "")); }
    }
}
