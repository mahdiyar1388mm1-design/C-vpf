using NexusControlCenter.Commands;
using NexusControlCenter.Models;
using NexusControlCenter.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace NexusControlCenter.ViewModels;
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly SystemMetricsService _metrics; private readonly LoggingService _logging; private readonly NotificationService _notifications; private readonly SettingsService _settings; private readonly FileBrowserService _files; private readonly ExportService _export;
    private CancellationTokenSource? _monitorCts; private SystemSnapshot? _snapshot; private string _activeModule = "Overview"; private int _selectedTab; private string _filePath = ""; private string _fileQuery = ""; private string _globalSearch = ""; private string _statusMessage = "All systems operational"; private ProcessInfo? _selectedProcess; private string _toastMessage = ""; private bool _showToast;
    public MainViewModel(SystemMetricsService metrics, LoggingService logging, NotificationService notifications, SettingsService settings, FileBrowserService files, ExportService export)
    {
        _metrics = metrics; _logging = logging; _notifications = notifications; _settings = settings; _files = files; _export = export;
        _notifications.NotificationRaised += (_, message) => { if (!_settings.NotificationsEnabled) return; ToastMessage = message; ShowToast = true; _ = HideToastAsync(); };
        NavigateCommand = new RelayCommand<string>(Navigate);
        RefreshProcessesCommand = new AsyncRelayCommand(RefreshProcessesAsync);
        KillProcessCommand = new AsyncRelayCommand(KillSelectedProcessAsync, () => SelectedProcess is not null);
        BrowseCommand = new AsyncRelayCommand(() => LoadFilesAsync(FilePath));
        ParentFolderCommand = new AsyncRelayCommand(() => LoadFilesAsync(_files.ParentPath()));
        RefreshFilesCommand = new AsyncRelayCommand(() => LoadFilesAsync(FilePath));
        OpenFileCommand = new AsyncRelayCommand(OpenSelectedFileAsync);
        SearchFilesCommand = new AsyncRelayCommand(() => LoadFilesAsync(FilePath, FileQuery));
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
        ToggleThemeCommand = new RelayCommand(() => { _settings.ToggleTheme(); OnPropertyChanged(nameof(ThemeLabel)); });
        OpenMonitorCommand = new RelayCommand(() => Navigate("System Monitor"));
        OpenFilesCommand = new RelayCommand(() => Navigate("File Manager"));
        RefreshCommand = new AsyncRelayCommand(RefreshSnapshotAsync);
        SearchCommand = new RelayCommand(ExecuteGlobalSearch);
        CloseToastCommand = new RelayCommand(() => ShowToast = false);
    }
    public SystemSnapshot? Snapshot { get => _snapshot; private set { if (Set(ref _snapshot, value)) { OnPropertyChanged(nameof(CpuText)); OnPropertyChanged(nameof(MemoryText)); OnPropertyChanged(nameof(DiskText)); OnPropertyChanged(nameof(NetworkText)); OnPropertyChanged(nameof(HostSummary)); OnPropertyChanged(nameof(UptimeText)); } } }
    public ObservableCollection<SystemSnapshot> SnapshotHistory { get; } = new();
    public ObservableCollection<double> CpuHistory { get; } = new();
    public ObservableCollection<double> MemoryHistory { get; } = new();
    public ObservableCollection<ProcessInfo> Processes { get; } = new();
    public ObservableCollection<FileEntry> Files { get; } = new();
    public ObservableCollection<ActivityRecord> Activities { get; } = new();
    public string CpuText => Snapshot is null ? "—" : $"{Snapshot.CpuPercent:F0}%";
    public string MemoryText => Snapshot is null ? "—" : $"{Snapshot.MemoryUsedGb:F1} / {Snapshot.MemoryTotalGb:F1} GB";
    public string DiskText => Snapshot is null ? "—" : $"{Snapshot.DiskUsedGb:F0} / {Snapshot.DiskTotalGb:F0} GB";
    public string NetworkText => Snapshot is null ? "—" : $"↓ {Snapshot.NetworkDownMbps:F1}   ↑ {Snapshot.NetworkUpMbps:F1} Mbps";
    public string HostSummary => Snapshot is null ? "Connecting to system" : $"{Snapshot.HostName}  ·  {Snapshot.OperatingSystem}";
    public string UptimeText => Snapshot?.Uptime ?? "—";
    public string ActiveModule { get => _activeModule; private set => Set(ref _activeModule, value); }
    public int SelectedTab { get => _selectedTab; set => Set(ref _selectedTab, value); }
    public string FilePath { get => _filePath; set => Set(ref _filePath, value); }
    public string FileQuery { get => _fileQuery; set => Set(ref _fileQuery, value); }
    public string GlobalSearch { get => _globalSearch; set => Set(ref _globalSearch, value); }
    public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public ProcessInfo? SelectedProcess { get => _selectedProcess; set { Set(ref _selectedProcess, value); KillProcessCommand.RaiseCanExecuteChanged(); } }
    public string ToastMessage { get => _toastMessage; private set => Set(ref _toastMessage, value); }
    public bool ShowToast { get => _showToast; private set => Set(ref _showToast, value); }
    public string ThemeLabel => _settings.IsLightTheme ? "Switch to dark mode" : "Switch to light mode";
    public bool NotificationsEnabled { get => _settings.NotificationsEnabled; set { _settings.NotificationsEnabled = value; OnPropertyChanged(); } }
    public RelayCommand<string> NavigateCommand { get; }
    public AsyncRelayCommand RefreshProcessesCommand { get; }
    public AsyncRelayCommand KillProcessCommand { get; }
    public AsyncRelayCommand BrowseCommand { get; }
    public AsyncRelayCommand ParentFolderCommand { get; }
    public AsyncRelayCommand RefreshFilesCommand { get; }
    public AsyncRelayCommand OpenFileCommand { get; }
    public AsyncRelayCommand SearchFilesCommand { get; }
    public AsyncRelayCommand ExportCsvCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand OpenMonitorCommand { get; }
    public RelayCommand OpenFilesCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand CloseToastCommand { get; }

    public async Task InitializeAsync()
    {
        await RefreshSnapshotAsync();
        await _logging.RecordAsync("Session", "Nexus Control Center started");
        await LoadActivitiesAsync();
        _monitorCts = new CancellationTokenSource();
        _ = MonitorLoopAsync(_monitorCts.Token);
    }
    private async Task MonitorLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1)); int ticks = 0;
        try { while (await timer.WaitForNextTickAsync(ct)) { await RefreshSnapshotAsync(ct); ticks++; if (ActiveModule == "Task Manager" && ticks % 2 == 0) await RefreshProcessesAsync(); } }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusMessage = "Live monitoring paused"; _notifications.Show($"Monitoring error: {ex.Message}"); }
    }
    private async Task RefreshSnapshotAsync(CancellationToken ct = default)
    {
        try
        {
            Snapshot = await _metrics.ReadSnapshotAsync(ct);
            SnapshotHistory.Add(Snapshot);
            while (SnapshotHistory.Count > 60) SnapshotHistory.RemoveAt(0);
            CpuHistory.Add(Snapshot.CpuPercent); MemoryHistory.Add(Snapshot.MemoryPercent);
            while (CpuHistory.Count > 60) CpuHistory.RemoveAt(0); while (MemoryHistory.Count > 60) MemoryHistory.RemoveAt(0);
            StatusMessage = $"Live · updated {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex) { StatusMessage = "Unable to read system metrics"; _notifications.Show(ex.Message); }
    }
    public async Task LoadActivitiesAsync()
    {
        try { Activities.Clear(); foreach (var item in await _logging.GetRecentAsync()) Activities.Add(item); }
        catch (Exception ex) { StatusMessage = $"Activity history unavailable: {ex.Message}"; }
    }
    private async Task RefreshProcessesAsync()
    {
        try { Processes.Clear(); foreach (var p in await _metrics.GetProcessesAsync()) Processes.Add(p); StatusMessage = $"{Processes.Count} active processes"; }
        catch (Exception ex) { _notifications.Show($"Could not load processes: {ex.Message}"); }
    }
    private async Task KillSelectedProcessAsync()
    {
        if (SelectedProcess is not { } selected) return;
        if (MessageBox.Show($"End process {selected.Name} (PID {selected.Id})? Unsaved work in this process may be lost.", "Confirm process termination", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await _metrics.TerminateProcessAsync(selected.Id); await _logging.RecordAsync("Process", $"Terminated {selected.Name} (PID {selected.Id})"); _notifications.Show($"Process {selected.Name} ended"); await RefreshProcessesAsync(); await LoadActivitiesAsync(); }
        catch (Exception ex) { _notifications.Show($"Unable to end process: {ex.Message}"); }
    }
    private async Task LoadFilesAsync(string path, string? query = null)
    {
        try
        {
            FilePath = Path.GetFullPath(path); var items = await _files.BrowseAsync(FilePath, query);
            Files.Clear(); foreach (var f in items) { var dir = f is DirectoryInfo; var size = dir ? "—" : FormatSize(((FileInfo)f).Length); Files.Add(new(f.Name, f.FullName, dir, size, f.LastWriteTime.ToString("g"), dir ? "Folder" : ((FileInfo)f).Extension.ToUpperInvariant().TrimStart('.'))); }
            StatusMessage = $"{Files.Count} items · {FilePath}";
        }
        catch (Exception ex) { _notifications.Show($"Could not open folder: {ex.Message}"); }
    }
    private async Task OpenSelectedFileAsync()
    {
        if (Application.Current.MainWindow is not NexusControlCenter.Views.MainWindow w || w.FileGrid.SelectedItem is not FileEntry item) return;
        if (item.IsDirectory) await LoadFilesAsync(item.FullPath);
        else try { Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true }); } catch (Exception ex) { _notifications.Show($"Unable to open file: {ex.Message}"); }
    }
    private async Task ExportCsvAsync() => await ExportAsync(false);
    private async Task ExportPdfAsync() => await ExportAsync(true);
    private async Task ExportAsync(bool pdf)
    {
        try { var path = pdf ? await _export.ExportPdfAsync(GetHistory()) : await _export.ExportCsvAsync(GetHistory()); StatusMessage = $"Report exported · {path}"; _notifications.Show($"Report saved to Documents: {Path.GetFileName(path)}"); await _logging.RecordAsync("Analytics", $"Exported {(pdf ? "PDF" : "CSV")} report"); }
        catch (Exception ex) { _notifications.Show($"Export failed: {ex.Message}"); }
    }
    private IEnumerable<SystemSnapshot> GetHistory() => SnapshotHistory.ToArray();
    private void Navigate(string? page)
    {
        if (string.IsNullOrWhiteSpace(page)) return;
        ActiveModule = page;
        SelectedTab = page switch { "Overview" => 0, "System Monitor" => 1, "Task Manager" => 2, "File Manager" => 3, "Analytics" => 4, "Settings" => 5, _ => 0 };
        if (page == "Task Manager") _ = RefreshProcessesAsync();
        if (page == "File Manager") _ = LoadFilesAsync(string.IsNullOrWhiteSpace(FilePath) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : FilePath);
        if (page == "Analytics") _ = LoadActivitiesAsync();
    }
    private void ExecuteGlobalSearch()
    {
        var query = GlobalSearch.Trim(); if (query.Length == 0) return;
        if ("file manager files folders".Contains(query, StringComparison.OrdinalIgnoreCase)) Navigate("File Manager");
        else if ("process task manager applications".Contains(query, StringComparison.OrdinalIgnoreCase)) Navigate("Task Manager");
        else if ("analytics report history".Contains(query, StringComparison.OrdinalIgnoreCase)) Navigate("Analytics");
        else if ("monitor system cpu memory network".Contains(query, StringComparison.OrdinalIgnoreCase)) Navigate("System Monitor");
        else { _notifications.Show($"Search Nexus modules for “{query}”"); }
    }
    private async Task HideToastAsync() { await Task.Delay(4500); await Application.Current.Dispatcher.InvokeAsync(() => ShowToast = false); }
    public void Dispose() { _monitorCts?.Cancel(); _monitorCts?.Dispose(); }
    private static string FormatSize(long bytes) => bytes switch { < 1024 => $"{bytes} B", < 1024 * 1024 => $"{bytes / 1024d:F1} KB", < 1024L * 1024 * 1024 => $"{bytes / 1024d / 1024:F1} MB", _ => $"{bytes / 1024d / 1024 / 1024:F2} GB" };
}
