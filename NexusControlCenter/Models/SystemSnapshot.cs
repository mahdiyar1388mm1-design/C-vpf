namespace NexusControlCenter.Models;
public sealed record SystemSnapshot(DateTime Timestamp, double CpuPercent, double MemoryPercent, double MemoryUsedGb, double MemoryTotalGb, double DiskPercent, double DiskUsedGb, double DiskTotalGb, double NetworkDownMbps, double NetworkUpMbps, int ProcessCount, string HostName, string OperatingSystem, string Uptime);
