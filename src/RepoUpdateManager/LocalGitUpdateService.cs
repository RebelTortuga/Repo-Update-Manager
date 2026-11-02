using System.Diagnostics;

namespace RepoUpdateManager;

public enum UpdateMethod { Unsupported, GitFastForward }
public enum UpdateResultState { UpdatedVerified, Failed, Unsupported }

public sealed record UpdatePlanItem(TrackedItem Item, UpdateMethod Method, string DetectedVersion, string Detail)
{
    public bool IsEligible => Method != UpdateMethod.Unsupported;
}
public sealed record UpdateResult(TrackedItem Item, UpdateResultState State, string Detail, string? VerifiedVersion);
public sealed record CommandResult(int ExitCode, string Output, string Error);

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string fileName, string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}

public sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(string fileName, string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(fileName) { WorkingDirectory = workingDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken); var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new(process.ExitCode, (await output).Trim(), (await error).Trim());
    }
}

public sealed class LocalGitUpdateService(ICommandRunner runner)
{
    private static readonly string[] SafeGit = ["-c", "core.hooksPath=NUL", "-c", "core.fsmonitor=false", "-c", "submodule.recurse=false"];

    public async Task<UpdatePlanItem> PlanAsync(TrackedItem item, CancellationToken cancellationToken)
    {
        if (item.Upstream == UpstreamKind.NpmPackage) return Unsupported(item, "npm packages can be checked and linked, but automatic npm installation is disabled in v0.1.0.");
        if (item.State != CheckState.UpdateAvailable || item.Upstream != UpstreamKind.GitHubCommit || string.IsNullOrWhiteSpace(item.LocalPath))
            return Unsupported(item, "Automatic update needs an available GitHub commit update and a local path.");
        if (!Directory.Exists(item.LocalPath)) return Unsupported(item, "The recorded local path does not exist.");
        var remote = await Git(item.LocalPath, ["remote", "get-url", "origin"], cancellationToken);
        if (remote.ExitCode != 0 || !MatchesRepository(remote.Output, item)) return Unsupported(item, "The local origin does not exactly match the tracked GitHub repository.");
        var status = await Git(item.LocalPath, ["status", "--porcelain"], cancellationToken);
        if (status.ExitCode != 0) return Unsupported(item, "Could not inspect the local Git worktree.");
        if (!string.IsNullOrWhiteSpace(status.Output)) return Unsupported(item, "The local Git worktree has changes; it will not be overwritten.");
        if (HasGitFilterAttributes(item.LocalPath)) return Unsupported(item, "The worktree declares Git filters, so automatic checkout is disabled.");
        var head = await Git(item.LocalPath, ["rev-parse", "HEAD"], cancellationToken);
        if (head.ExitCode != 0) return Unsupported(item, "The local path is not a usable Git worktree.");
        return new(item, UpdateMethod.GitFastForward, Short(head.Output), "Git fast-forward: network access and write access to this clean local worktree. Hooks, submodules, and filters are disabled.");
    }

    public async Task<UpdateResult> ApplyAsync(UpdatePlanItem plan, CancellationToken cancellationToken)
    {
        if (!plan.IsEligible) return new(plan.Item, UpdateResultState.Unsupported, plan.Detail, null);
        var current = await PlanAsync(plan.Item, cancellationToken);
        if (!current.IsEligible) return new(plan.Item, UpdateResultState.Unsupported, current.Detail, null);
        var pull = await Git(plan.Item.LocalPath!, ["pull", "--ff-only", "--no-rebase", "--no-verify", "--no-recurse-submodules"], cancellationToken);
        if (pull.ExitCode != 0) return new(plan.Item, UpdateResultState.Failed, FirstLine(pull.Error, "Git fast-forward failed."), null);
        var head = await Git(plan.Item.LocalPath!, ["rev-parse", "HEAD"], cancellationToken);
        var expected = plan.Item.LatestVersion ?? string.Empty;
        if (head.ExitCode != 0 || !head.Output.StartsWith(expected, StringComparison.OrdinalIgnoreCase)) return new(plan.Item, UpdateResultState.Failed, "Git completed, but the checked upstream commit could not be verified locally.", null);
        return new(plan.Item, UpdateResultState.UpdatedVerified, "Updated and verified against the checked GitHub commit.", Short(head.Output));
    }

    private async Task<CommandResult> Git(string path, IReadOnlyList<string> arguments, CancellationToken cancellationToken) =>
        await runner.RunAsync("git", path, [.. SafeGit, .. arguments], cancellationToken);
    private static UpdatePlanItem Unsupported(TrackedItem item, string detail) => new(item, UpdateMethod.Unsupported, item.InstalledVersion ?? "Not detected", detail);
    private static string Short(string sha) => sha.Trim()[..Math.Min(12, sha.Trim().Length)];
    private static string FirstLine(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];
    private static bool MatchesRepository(string remote, TrackedItem item)
    {
        var expected = $"github.com/{item.Owner}/{item.Repository}";
        return remote.Trim().Replace(":", "/", StringComparison.Ordinal).Replace(".git", "", StringComparison.OrdinalIgnoreCase).EndsWith(expected, StringComparison.OrdinalIgnoreCase);
    }
    private static bool HasGitFilterAttributes(string path)
    {
        try { return Directory.EnumerateFiles(path, ".gitattributes", SearchOption.AllDirectories).Where(file => !file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)).Any(file => File.ReadLines(file).Any(line => line.Contains("filter=", StringComparison.OrdinalIgnoreCase))); }
        catch { return true; }
    }
}
