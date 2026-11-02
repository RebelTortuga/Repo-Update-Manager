using System.Diagnostics;

namespace RepoUpdateManager;

public sealed class WeeklyScheduleService
{
    public const string TaskName = "RebelTortuga Repo-Update-Manager Weekly Check";

    public static string BuildCreateArguments(string executablePath, TimeOnly time) =>
        $"/Create /F /TN \"{TaskName}\" /SC WEEKLY /D MON /ST {time:HH\\:mm} /RL LIMITED /TR \"\\\"{executablePath}\\\" --scheduled-check\"";

    public async Task ApplyAsync(bool enabled, TimeOnly time, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Weekly background scheduling is currently available on Windows only.");
        var arguments = enabled ? BuildCreateArguments(Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine this application's executable path."), time) : $"/Delete /F /TN \"{TaskName}\"";
        var start = new ProcessStartInfo("schtasks.exe", arguments) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardError = true };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Windows Task Scheduler.");
        var error = await process.StandardError.ReadToEndAsync(cancellationToken); await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0 && !(string.IsNullOrWhiteSpace(error) && !enabled)) throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Windows Task Scheduler rejected the change." : error.Trim());
    }
}
