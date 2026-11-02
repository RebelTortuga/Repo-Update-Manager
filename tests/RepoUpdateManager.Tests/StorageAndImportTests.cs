using System.Text;
using Microsoft.Data.Sqlite;
using Xunit;

namespace RepoUpdateManager.Tests;

public sealed class StorageAndImportTests
{
    [Fact]
    public async Task Database_persists_updates_deletes_and_settings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"repo-update-manager-{Guid.NewGuid():N}.db");
        try
        {
            var database = new UpdateDatabase(path); await database.InitializeAsync();
            var item = new TrackedItem(1, "Skill", TrackingKind.AiSkill, "owner", "repo", UpstreamKind.GitHubRelease, "skill-v*", "1.0.0", null, null, null, CheckState.Unknown, "Not checked", null);
            await database.SaveAsync(item); await database.SetSettingAsync("schedule", "weekly");
            Assert.Single(await database.GetItemsAsync()); Assert.Equal("weekly", await database.GetSettingAsync("schedule"));
            await database.DeleteAsync(item.Id); Assert.Empty(await database.GetItemsAsync());
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Import_rejects_duplicate_project_streams()
    {
        const string json = """[{"Id":1,"Name":"One","Kind":0,"Owner":"owner","Repository":"repo","Upstream":0,"State":0},{"Id":2,"Name":"Two","Kind":0,"Owner":"owner","Repository":"repo","Upstream":0,"State":0}]""";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await Assert.ThrowsAsync<InvalidDataException>(() => TrackerImportExport.ImportAsync(stream));
    }

    [Fact]
    public void Weekly_schedule_uses_the_app_executable_and_requested_time()
    {
        var arguments = WeeklyScheduleService.BuildCreateArguments("C:\\Apps\\RepoUpdateManager.exe", new TimeOnly(9, 30));
        Assert.Contains("/SC WEEKLY", arguments); Assert.Contains("/D MON", arguments); Assert.Contains("/ST 09:30", arguments);
        Assert.Contains("C:\\Apps\\RepoUpdateManager.exe", arguments); Assert.Contains("--scheduled-check", arguments);
    }
}
