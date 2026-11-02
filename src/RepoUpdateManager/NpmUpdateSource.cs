using System.Text.Json;

namespace RepoUpdateManager;

public sealed class NpmUpdateSource(HttpClient client)
{
    public async Task<UpstreamResult> GetLatestAsync(TrackedItem item, CancellationToken cancellationToken)
    {
        if (item.Upstream != UpstreamKind.NpmPackage || string.IsNullOrWhiteSpace(item.Repository)) throw new InvalidOperationException("This is not a verified npm package.");
        using var response = await client.GetAsync($"https://registry.npmjs.org/{Uri.EscapeDataString(item.Repository)}/latest", cancellationToken);
        response.EnsureSuccessStatusCode(); using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var version = json.RootElement.GetProperty("version").GetString() ?? throw new InvalidOperationException("npm did not provide a package version.");
        var name = json.RootElement.GetProperty("name").GetString() ?? throw new InvalidOperationException("npm did not provide a package identity.");
        if (!string.Equals(name, item.Repository, StringComparison.Ordinal)) throw new InvalidOperationException("npm package identity did not match the tracked package.");
        return new(version, $"https://www.npmjs.com/package/{Uri.EscapeDataString(name)}", DateTimeOffset.UtcNow, "npm registry");
    }
}
