namespace NexusControlCenter.Models;
public sealed record FileEntry(string Name, string FullPath, bool IsDirectory, string Size, string Modified, string Type);
