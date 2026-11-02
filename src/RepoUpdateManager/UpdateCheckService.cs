namespace RepoUpdateManager;

public sealed class UpdateCheckService(UpdateDatabase database, GitHubUpdateSource source, NpmUpdateSource? npm = null)
{
    public async Task<IReadOnlyList<TrackedItem>> CheckAsync(IEnumerable<TrackedItem> items, CancellationToken cancellationToken)
    {
        var checkedItems = new List<TrackedItem>();
        foreach (var original in items)
        {
            var item = original;
            try
            {
                var upstream = item.Upstream == UpstreamKind.NpmPackage
                    ? await (npm ?? new NpmUpdateSource(new HttpClient { Timeout = TimeSpan.FromSeconds(20) })).GetLatestAsync(item, cancellationToken)
                    : await source.GetLatestAsync(item, cancellationToken);
                var state = string.IsNullOrWhiteSpace(item.InstalledVersion) ? CheckState.Unknown : VersionComparer.IsUpdate(item.InstalledVersion, upstream.Version) ? CheckState.UpdateAvailable : CheckState.Current;
                item = item with { LatestVersion = upstream.Version, LatestUrl = upstream.Url, State = state, Detail = $"{upstream.Source} · checked {DateTimeOffset.Now:t}", LastCheckedUtc = DateTimeOffset.UtcNow };
            }
            catch (GitHubRateLimitException ex) { item = item with { State = CheckState.RateLimited, Detail = ex.Message, LastCheckedUtc = DateTimeOffset.UtcNow }; }
            catch (HttpRequestException ex) { item = item with { State = CheckState.Offline, Detail = $"Network or GitHub error: {ex.Message}", LastCheckedUtc = DateTimeOffset.UtcNow }; }
            catch (Exception ex) { item = item with { State = CheckState.Error, Detail = ex.Message, LastCheckedUtc = DateTimeOffset.UtcNow }; }
            await database.SaveAsync(item); checkedItems.Add(item);
        }
        return checkedItems;
    }
}
