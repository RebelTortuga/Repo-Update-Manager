using Microsoft.Data.Sqlite;

namespace RepoUpdateManager;

public sealed class UpdateDatabase
{
    private readonly string _connectionString;
    public UpdateDatabase(string? databasePath = null)
    {
        var path = databasePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RebelTortuga", "RepoUpdateManager");
            Directory.CreateDirectory(data);
            path = Path.Combine(data, "updates.db");
        }
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }
    public async Task InitializeAsync()
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS tracked_items (
              id INTEGER PRIMARY KEY, name TEXT NOT NULL, kind INTEGER NOT NULL, owner TEXT NOT NULL, repository TEXT NOT NULL,
              upstream INTEGER NOT NULL, stream_pattern TEXT NULL, installed_version TEXT NULL, local_path TEXT NULL,
              latest_version TEXT NULL, latest_url TEXT NULL, state INTEGER NOT NULL, detail TEXT NULL, last_checked_utc TEXT NULL);
            CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            """;
        await command.ExecuteNonQueryAsync();
    }
    public async Task<IReadOnlyList<TrackedItem>> GetItemsAsync()
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(); var command = db.CreateCommand();
        command.CommandText = "SELECT id,name,kind,owner,repository,upstream,stream_pattern,installed_version,local_path,latest_version,latest_url,state,detail,last_checked_utc FROM tracked_items ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync(); var result = new List<TrackedItem>();
        while (await reader.ReadAsync()) result.Add(new(reader.GetInt64(0), reader.GetString(1), (TrackingKind)reader.GetInt32(2), reader.GetString(3), reader.GetString(4), (UpstreamKind)reader.GetInt32(5), reader.IsDBNull(6)?null:reader.GetString(6), reader.IsDBNull(7)?null:reader.GetString(7), reader.IsDBNull(8)?null:reader.GetString(8), reader.IsDBNull(9)?null:reader.GetString(9), reader.IsDBNull(10)?null:reader.GetString(10), (CheckState)reader.GetInt32(11), reader.IsDBNull(12)?null:reader.GetString(12), reader.IsDBNull(13)?null:DateTimeOffset.Parse(reader.GetString(13))));
        return result;
    }
    public async Task SaveAsync(TrackedItem item)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(); var command = db.CreateCommand();
        command.CommandText = """
            INSERT INTO tracked_items (id,name,kind,owner,repository,upstream,stream_pattern,installed_version,local_path,latest_version,latest_url,state,detail,last_checked_utc)
            VALUES ($id,$name,$kind,$owner,$repo,$upstream,$stream,$installed,$path,$latest,$url,$state,$detail,$checked)
            ON CONFLICT(id) DO UPDATE SET name=$name,kind=$kind,owner=$owner,repository=$repo,upstream=$upstream,stream_pattern=$stream,installed_version=$installed,local_path=$path,latest_version=$latest,latest_url=$url,state=$state,detail=$detail,last_checked_utc=$checked;
            """;
        foreach (var (key, value) in new Dictionary<string, object?> { ["$id"]=item.Id, ["$name"]=item.Name, ["$kind"]=(int)item.Kind, ["$owner"]=item.Owner, ["$repo"]=item.Repository, ["$upstream"]=(int)item.Upstream, ["$stream"]=item.StreamPattern, ["$installed"]=item.InstalledVersion, ["$path"]=item.LocalPath, ["$latest"]=item.LatestVersion, ["$url"]=item.LatestUrl, ["$state"]=(int)item.State, ["$detail"]=item.Detail, ["$checked"]=item.LastCheckedUtc?.ToString("O") }) command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }
    public async Task DeleteAsync(long id)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(); var command = db.CreateCommand();
        command.CommandText = "DELETE FROM tracked_items WHERE id = $id"; command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }
    public async Task<string?> GetSettingAsync(string key)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(); var command = db.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key = $key"; command.Parameters.AddWithValue("$key", key);
        return (string?)await command.ExecuteScalarAsync();
    }
    public async Task SetSettingAsync(string key, string value)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(); var command = db.CreateCommand();
        command.CommandText = "INSERT INTO settings (key,value) VALUES ($key,$value) ON CONFLICT(key) DO UPDATE SET value=$value";
        command.Parameters.AddWithValue("$key", key); command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }
}

