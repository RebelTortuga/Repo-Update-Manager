using System.Text.Json;

namespace RepoUpdateManager;

public static class TrackerImportExport
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public static Task ExportAsync(Stream stream, IEnumerable<TrackedItem> items, CancellationToken cancellationToken = default) =>
        JsonSerializer.SerializeAsync(stream, items, JsonOptions, cancellationToken);

    public static async Task<IReadOnlyList<TrackedItem>> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var items = await JsonSerializer.DeserializeAsync<List<TrackedItem>>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The import file does not contain a project list.");
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Owner) || string.IsNullOrWhiteSpace(item.Repository))
                throw new InvalidDataException("Every imported project needs a name, GitHub owner, and repository.");
            if (!Enum.IsDefined(item.Kind) || !Enum.IsDefined(item.Upstream) || !Enum.IsDefined(item.State))
                throw new InvalidDataException($"{item.Name} has an unsupported tracking value.");
            if (!duplicates.Add($"{item.Owner}/{item.Repository}/{item.Upstream}/{item.StreamPattern}"))
                throw new InvalidDataException($"The import contains duplicate tracking entries for {item.Owner}/{item.Repository}.");
        }
        return items;
    }
}
