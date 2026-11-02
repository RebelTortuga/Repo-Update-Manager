using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace RepoUpdateManager;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly UpdateDatabase _database = new();
    private readonly GitHubUpdateSource _source = new(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
    private readonly WeeklyScheduleService _schedule = new();
    private readonly LocalGitUpdateService _updates = new(new ProcessCommandRunner());
    private IReadOnlyList<UpdatePlanItem> _updatePlan = [];
    public ObservableCollection<TrackedItem> Items { get; } = [];
    public string ItemSummary => $"{Items.Count} items";
    public new event PropertyChangedEventHandler? PropertyChanged;
    public MainWindow() { InitializeComponent(); DataContext = this; Opened += async (_, _) => await LoadAsync(); }

    private async Task LoadAsync()
    {
        try { await _database.InitializeAsync(); foreach (var item in await _database.GetItemsAsync()) Items.Add(item); WeeklyEnabledInput.IsChecked = await _database.GetSettingAsync("weekly-enabled") != "false"; WeeklyTimeInput.Text = await _database.GetSettingAsync("weekly-time") ?? "09:00"; Raise(nameof(ItemSummary)); }
        catch (Exception ex) { StatusText.Text = $"Could not open local storage: {ex.Message}"; }
    }
    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        var isNpm = SourceInput.SelectedIndex == 3;
        var parts = Array.Empty<string>();
        if (isNpm) { var package = UrlInput.Text?.Trim().Replace("https://www.npmjs.com/package/", "", StringComparison.OrdinalIgnoreCase); if (string.IsNullOrWhiteSpace(package)) { FormStatus.Text = "Enter the exact npm package name, for example @openai/codex."; return; } parts = ["npm", package]; }
        else { if (!Uri.TryCreate(UrlInput.Text, UriKind.Absolute, out var uri) || uri.Host != "github.com") { FormStatus.Text = "Enter a complete GitHub URL, for example https://github.com/your-account/your-repository."; return; } parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 2) { FormStatus.Text = "That URL must include both an owner and repository."; return; } }
        var kind = KindInput.SelectedIndex switch { 0 => TrackingKind.AiSkill, 1 => TrackingKind.Repository, 2 => TrackingKind.Plugin, 3 => TrackingKind.ManualTool, _ => TrackingKind.AiSkill };
        var name = string.IsNullOrWhiteSpace(NameInput.Text) ? parts[1] : NameInput.Text.Trim();
        var source = SourceInput.SelectedIndex switch { 1 => UpstreamKind.GitHubTag, 2 => UpstreamKind.GitHubCommit, 3 => UpstreamKind.NpmPackage, _ => UpstreamKind.GitHubRelease };
        var existing = ItemsList.SelectedItem as TrackedItem;
        var item = new TrackedItem(existing?.Id ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), name, kind, parts[0], parts[1].Replace(".git", "", StringComparison.OrdinalIgnoreCase), source, Empty(PatternInput.Text), Empty(VersionInput.Text), Empty(PathInput.Text), existing?.LatestVersion, existing?.LatestUrl, existing?.State ?? CheckState.Unknown, existing?.Detail ?? "Not checked yet", existing?.LastCheckedUtc);
        await _database.SaveAsync(item);
        if (existing is null) { Items.Add(item); Raise(nameof(ItemSummary)); FormStatus.Text = $"Added {item.Name}. Use Check now when you are ready."; }
        else { Items[Items.IndexOf(existing)] = item; FormStatus.Text = $"Saved changes to {item.Name}."; }
        ResetForm();
    }
    private async void CheckNow_Click(object? sender, RoutedEventArgs e)
    {
        if (Items.Count == 0) { StatusText.Text = "Add a project first. Your data stays local in SQLite."; return; }
        StatusText.Text = "Checking GitHub…"; var snapshot = Items.ToArray(); var checkedItems = await new UpdateCheckService(_database, _source).CheckAsync(snapshot, CancellationToken.None);
        for (var i = 0; i < snapshot.Length; i++) Items[Items.IndexOf(snapshot[i])] = checkedItems[i];
        StatusText.Text = $"Finished {snapshot.Length} read-only GitHub checks at {DateTimeOffset.Now:t}.";
    }
    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Export tracked projects", SuggestedFileName = "repo-update-manager-export.json", FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }] });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        await TrackerImportExport.ExportAsync(stream, Items);
        StatusText.Text = "Exported tracked projects.";
    }
    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Import tracked projects", AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }] });
        if (files.Count == 0) return;
        try
        {
            await using var stream = await files[0].OpenReadAsync(); var imported = await TrackerImportExport.ImportAsync(stream); var count = 0;
            foreach (var item in imported)
            {
                var fresh = item with { Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + count++ };
                await _database.SaveAsync(fresh); Items.Add(fresh);
            }
            Raise(nameof(ItemSummary)); StatusText.Text = $"Imported {count} tracked project{(count == 1 ? string.Empty : "s")}.";
        }
        catch (Exception ex) { StatusText.Text = $"Import failed: {ex.Message}"; }
    }
    private void Items_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ItemsList.SelectedItem is not TrackedItem item) { ResetForm(); return; }
        FormTitle.Text = $"Edit {item.Name}"; NameInput.Text = item.Name; UrlInput.Text = $"https://github.com/{item.Owner}/{item.Repository}"; VersionInput.Text = item.InstalledVersion; PatternInput.Text = item.StreamPattern; PathInput.Text = item.LocalPath;
        KindInput.SelectedIndex = item.Kind switch { TrackingKind.AiSkill => 0, TrackingKind.Repository => 1, TrackingKind.Plugin => 2, _ => 3 };
        SourceInput.SelectedIndex = item.Upstream switch { UpstreamKind.GitHubTag => 1, UpstreamKind.GitHubCommit => 2, UpstreamKind.NpmPackage => 3, _ => 0 };
        SaveButton.Content = "Save changes"; DeleteButton.IsVisible = CancelEditButton.IsVisible = true;
        ViewRepositoryButton.IsEnabled = true; ViewMatchButton.IsEnabled = ProjectActionPolicy.CanViewExactMatch(item); InstructionsButton.IsEnabled = ProjectActionPolicy.CanPrepareUpdate(item);
        UpdateSelectedButton.IsEnabled = ProjectActionPolicy.CanPrepareUpdate(item);
        VerifiedInstalledCheck.IsChecked = false; VerifiedInstalledCheck.IsEnabled = ProjectActionPolicy.CanPrepareUpdate(item); RecordVerifiedButton.IsEnabled = ProjectActionPolicy.CanPrepareUpdate(item);
        VerificationHint.Text = ProjectActionPolicy.CanPrepareUpdate(item) ? $"After you update {item.Name} outside this app, confirm its installed version before recording {item.LatestVersion}." : "A checked, available update is required before a version can be recorded.";
        FormStatus.Text = "Edit the recorded details, then save or delete this tracker entry.";
    }
    private async void PrepareSelectedUpdate_Click(object? sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is TrackedItem item) await PrepareUpdatesAsync([item]);
    }
    private async void PrepareAllUpdates_Click(object? sender, RoutedEventArgs e) => await PrepareUpdatesAsync(Items.Where(ProjectActionPolicy.CanPrepareUpdate));
    private async Task PrepareUpdatesAsync(IEnumerable<TrackedItem> items)
    {
        var candidates = items.ToArray();
        if (candidates.Length == 0) { UpdatePlanSummary.Text = "No checked updates are available."; return; }
        StatusText.Text = $"Preparing {candidates.Length} update plan(s)…";
        _updatePlan = await Task.WhenAll(candidates.Select(item => _updates.PlanAsync(item, CancellationToken.None)));
        var eligible = _updatePlan.Count(x => x.IsEligible);
        UpdatePlanSummary.Text = $"{eligible} eligible of {_updatePlan.Count}";
        UpdatePlanDetail.Text = string.Join(Environment.NewLine, _updatePlan.Select(x => $"{x.Item.Name}: {x.DetectedVersion} → {x.Item.LatestVersion}; {x.Detail}"));
        ConfirmUpdatesCheck.IsChecked = false; ConfirmUpdatesCheck.IsEnabled = eligible > 0; RunUpdatesButton.IsEnabled = eligible > 0;
        StatusText.Text = eligible > 0 ? "Review the affected projects, method, and permissions, then approve to run the eligible updates." : "No safe automatic updates are eligible; use the exact match and instructions links instead.";
    }
    private async void RunUpdates_Click(object? sender, RoutedEventArgs e)
    {
        if (ConfirmUpdatesCheck.IsChecked != true) { StatusText.Text = "Approve the displayed local Git updates before running them."; return; }
        var eligible = _updatePlan.Where(x => x.IsEligible).ToArray();
        if (eligible.Length == 0) return;
        RunUpdatesButton.IsEnabled = false; StatusText.Text = $"Updating {eligible.Length} eligible project(s)…";
        var results = new List<UpdateResult>();
        foreach (var plan in eligible)
        {
            try { results.Add(await _updates.ApplyAsync(plan, CancellationToken.None)); }
            catch (Exception ex) { results.Add(new(plan.Item, UpdateResultState.Failed, ex.Message, null)); }
        }
        foreach (var result in results.Where(x => x.State == UpdateResultState.UpdatedVerified))
        {
            var updated = result.Item with { InstalledVersion = result.VerifiedVersion, State = CheckState.Current, Detail = result.Detail, LastCheckedUtc = DateTimeOffset.UtcNow };
            await _database.SaveAsync(updated); Items[Items.IndexOf(result.Item)] = updated;
        }
        UpdatePlanDetail.Text = string.Join(Environment.NewLine, results.Select(x => $"{x.Item.Name}: {x.State}; {x.Detail}"));
        StatusText.Text = $"Update run complete: {results.Count(x => x.State == UpdateResultState.UpdatedVerified)} verified, {results.Count(x => x.State == UpdateResultState.Failed)} failed, {results.Count(x => x.State == UpdateResultState.Unsupported)} unsupported.";
        ConfirmUpdatesCheck.IsChecked = false; ConfirmUpdatesCheck.IsEnabled = RunUpdatesButton.IsEnabled = false;
    }
    private void ViewRepository_Click(object? sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not TrackedItem item) return;
        OpenUrl(ProjectActionPolicy.RepositoryUrl(item));
    }
    private void ViewMatch_Click(object? sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not TrackedItem item || !ProjectActionPolicy.CanViewExactMatch(item)) return;
        OpenUrl(item.LatestUrl!);
    }
    private void Instructions_Click(object? sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not TrackedItem item || !ProjectActionPolicy.CanPrepareUpdate(item)) return;
        FormStatus.Text = ProjectActionPolicy.UpdateGuidance(item);
        OpenUrl(item.LatestUrl!);
    }
    private async void RecordVerified_Click(object? sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not TrackedItem item || !ProjectActionPolicy.CanPrepareUpdate(item)) return;
        if (VerifiedInstalledCheck.IsChecked != true) { FormStatus.Text = "Confirm that you verified the installed version before recording it."; return; }
        var verified = item with { InstalledVersion = item.LatestVersion, State = CheckState.Current, Detail = $"Version recorded as user-verified at {DateTimeOffset.Now:t}", LastCheckedUtc = DateTimeOffset.UtcNow };
        await _database.SaveAsync(verified); Items[Items.IndexOf(item)] = verified;
        StatusText.Text = $"Recorded {verified.LatestVersion} for {verified.Name} after your verification.";
        ItemsList.SelectedItem = verified;
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not TrackedItem item) return;
        await _database.DeleteAsync(item.Id); Items.Remove(item); Raise(nameof(ItemSummary)); StatusText.Text = $"Deleted {item.Name} from this tracker. No local tool was modified."; ResetForm();
    }
    private void CancelEdit_Click(object? sender, RoutedEventArgs e) { ResetForm(); }
    private async void SaveSchedule_Click(object? sender, RoutedEventArgs e)
    {
        if (!TimeOnly.TryParse(WeeklyTimeInput.Text, out var time)) { StatusText.Text = "Enter the weekly check time as HH:mm, for example 09:00."; return; }
        try
        {
            var enabled = WeeklyEnabledInput.IsChecked == true; await _schedule.ApplyAsync(enabled, time);
            await _database.SetSettingAsync("weekly-enabled", enabled ? "true" : "false"); await _database.SetSettingAsync("weekly-time", time.ToString("HH:mm"));
            StatusText.Text = enabled ? $"Weekly background check scheduled every Monday at {time:HH:mm}." : "Weekly background check disabled.";
        }
        catch (Exception ex) { StatusText.Text = $"Could not save the weekly schedule: {ex.Message}"; }
    }
    private void ResetForm()
    {
        ItemsList.SelectedItem = null; FormTitle.Text = "Add a project"; SaveButton.Content = "Add to tracker"; DeleteButton.IsVisible = CancelEditButton.IsVisible = false;
        NameInput.Text = UrlInput.Text = VersionInput.Text = PatternInput.Text = PathInput.Text = string.Empty; KindInput.SelectedIndex = SourceInput.SelectedIndex = 0;
        ViewRepositoryButton.IsEnabled = ViewMatchButton.IsEnabled = InstructionsButton.IsEnabled = UpdateSelectedButton.IsEnabled = false; VerifiedInstalledCheck.IsChecked = false; VerifiedInstalledCheck.IsEnabled = RecordVerifiedButton.IsEnabled = false; VerificationHint.Text = "Select an available update to record it only after you have installed and verified it yourself.";
    }
    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); StatusText.Text = "Opened the selected GitHub page in your browser."; }
        catch (Exception ex) { StatusText.Text = $"Could not open the browser: {ex.Message}"; }
    }
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void Raise(string name) => PropertyChanged?.Invoke(this, new(name));
}

