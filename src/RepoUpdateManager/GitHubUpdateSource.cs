using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RepoUpdateManager;

public sealed class GitHubUpdateSource(HttpClient client)
{
    public async Task<UpstreamResult> GetLatestAsync(TrackedItem item, CancellationToken cancellationToken)
    {
        var endpoint = item.Upstream switch
        {
            UpstreamKind.GitHubRelease => $"https://api.github.com/repos/{item.Owner}/{item.Repository}/releases?per_page=100",
            UpstreamKind.GitHubTag => $"https://api.github.com/repos/{item.Owner}/{item.Repository}/tags?per_page=100",
            _ => $"https://api.github.com/repos/{item.Owner}/{item.Repository}/commits?per_page=1"
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Repo-Update-Manager", "0.1"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden && response.Headers.TryGetValues("X-RateLimit-Remaining", out var values) && values.FirstOrDefault() == "0")
            throw new GitHubRateLimitException("GitHub API rate limit reached. Try again later or configure a token in a future release.");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var result = item.Upstream switch
        {
            UpstreamKind.GitHubRelease => ParseRelease(json.RootElement, item.StreamPattern),
            UpstreamKind.GitHubTag => ParseTag(json.RootElement, item.StreamPattern),
            _ => ParseCommit(json.RootElement)
        };
        if (result is null) throw new InvalidOperationException("No upstream entry matched the selected stream.");
        // The tag API exposes a commit API URL; provide a human-facing exact tag page instead.
        return item.Upstream == UpstreamKind.GitHubTag
            ? result with { Url = $"https://github.com/{item.Owner}/{item.Repository}/tree/{Uri.EscapeDataString(result.Version)}" }
            : result;
    }

    private static UpstreamResult? ParseRelease(JsonElement root, string? stream)
    {
        return root.EnumerateArray()
            .Where(x => !x.GetProperty("draft").GetBoolean() && !x.GetProperty("prerelease").GetBoolean())
            .Select(x => new { Tag = x.GetProperty("tag_name").GetString()!, Entry = x })
            .Where(x => ReleaseStreamFilter.Matches(x.Tag, stream))
            .OrderByDescending(x => x.Tag, Comparer<string>.Create(VersionComparer.Compare))
            .Select(x => new UpstreamResult(x.Tag, x.Entry.GetProperty("html_url").GetString()!, x.Entry.GetProperty("published_at").GetDateTimeOffset(), "GitHub release"))
            .FirstOrDefault();
    }
    private static UpstreamResult? ParseTag(JsonElement root, string? stream)
    {
        return root.EnumerateArray()
            .Select(x => new { Tag = x.GetProperty("name").GetString()!, Entry = x })
            .Where(x => ReleaseStreamFilter.Matches(x.Tag, stream))
            .OrderByDescending(x => x.Tag, Comparer<string>.Create(VersionComparer.Compare))
            .Select(x => new UpstreamResult(x.Tag, x.Entry.GetProperty("commit").GetProperty("url").GetString()!, DateTimeOffset.UtcNow, "GitHub tag"))
            .FirstOrDefault();
    }
    private static UpstreamResult ParseCommit(JsonElement root)
    {
        var entry = root[0]; var sha = entry.GetProperty("sha").GetString()!;
        return new(sha[..Math.Min(12, sha.Length)], entry.GetProperty("html_url").GetString()!, entry.GetProperty("commit").GetProperty("author").GetProperty("date").GetDateTimeOffset(), "GitHub commit");
    }
}

public sealed class GitHubRateLimitException(string message) : Exception(message);

