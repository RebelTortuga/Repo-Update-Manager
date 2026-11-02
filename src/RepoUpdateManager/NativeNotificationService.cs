using System.Diagnostics;
using System.Text;

namespace RepoUpdateManager;

public sealed class NativeNotificationService
{
    public async Task NotifyUpdatesAsync(IReadOnlyCollection<TrackedItem> updates, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows() || updates.Count == 0) return;
        var title = updates.Count == 1 ? "Repo-Update-Manager: update found" : $"Repo-Update-Manager: {updates.Count} updates found";
        var names = string.Join(", ", updates.Take(3).Select(x => x.Name));
        var body = updates.Count > 3 ? $"{names}, and {updates.Count - 3} more." : names;
        var script = "$n=New-Object System.Windows.Forms.NotifyIcon;$n.Icon=[System.Drawing.SystemIcons]::Information;$n.Visible=$true;$n.ShowBalloonTip(8000,'" + Escape(title) + "','" + Escape(body) + "',[System.Windows.Forms.ToolTipIcon]::Info);Start-Sleep -Seconds 9;$n.Dispose()";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes("Add-Type -AssemblyName System.Windows.Forms;" + script));
        var start = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {encoded}") { CreateNoWindow = true, UseShellExecute = false };
        using var process = Process.Start(start);
        if (process is not null) await process.WaitForExitAsync(cancellationToken);
    }
    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
