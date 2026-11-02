using Avalonia;

namespace RepoUpdateManager;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--scheduled-check", StringComparer.OrdinalIgnoreCase)) { RunScheduledCheckAsync().GetAwaiter().GetResult(); return; }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task RunScheduledCheckAsync()
    {
        var database = new UpdateDatabase(); await database.InitializeAsync();
        var source = new GitHubUpdateSource(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
        var checkedItems = await new UpdateCheckService(database, source).CheckAsync(await database.GetItemsAsync(), CancellationToken.None);
        await new NativeNotificationService().NotifyUpdatesAsync(checkedItems.Where(x => x.State == CheckState.UpdateAvailable).ToArray());
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

