namespace NexusControlCenter.Services;
public sealed class FileBrowserService
{
    public string CurrentPath { get; private set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public Task<IReadOnlyList<FileSystemInfo>> BrowseAsync(string? path = null, string? query = null, CancellationToken ct = default) => Task.Run<IReadOnlyList<FileSystemInfo>>(() =>
    {
        var target = string.IsNullOrWhiteSpace(path) ? CurrentPath : Path.GetFullPath(path);
        if (!Directory.Exists(target)) throw new DirectoryNotFoundException($"Folder not found: {target}");
        CurrentPath = target;
        var directory = new DirectoryInfo(target);
        var items = directory.EnumerateFileSystemInfos().Where(x => string.IsNullOrWhiteSpace(query) || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x is DirectoryInfo).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Take(1000).ToArray();
        return items;
    }, ct);
    public string ParentPath() => Directory.GetParent(CurrentPath)?.FullName ?? CurrentPath;
}
