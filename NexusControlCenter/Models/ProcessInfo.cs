namespace NexusControlCenter.Models;
public sealed record ProcessInfo(int Id, string Name, double CpuPercent, double MemoryMb, string WindowTitle, string Status);
