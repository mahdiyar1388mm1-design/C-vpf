using Microsoft.Extensions.Logging;
using NexusControlCenter.Models;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NexusControlCenter.Services;
public sealed class SystemMetricsService(ILogger<SystemMetricsService> logger)
{
    private ulong _lastIdle, _lastKernel, _lastUser;
    private long _lastReceived, _lastSent;
    private DateTime _lastNetwork = DateTime.UtcNow;
    private readonly Dictionary<int, (TimeSpan Cpu, DateTime At)> _processCpu = new();
    private readonly object _processLock = new();

    public async Task<SystemSnapshot> ReadSnapshotAsync(CancellationToken ct = default)
    {
        try { return await Task.Run(ReadSnapshot, ct); }
        catch (Exception ex) { logger.LogError(ex, "Unable to read system metrics"); throw; }
    }
    private SystemSnapshot ReadSnapshot()
    {
        var now = DateTime.UtcNow;
        double cpu = 0;
        if (OperatingSystem.IsWindows() && GetSystemTimes(out var idle, out var kernel, out var user))
        {
            var i = ToUInt64(idle); var k = ToUInt64(kernel); var u = ToUInt64(user);
            var total = (k - _lastKernel) + (u - _lastUser); var idleDelta = i - _lastIdle;
            if (_lastKernel != 0 && total > 0) cpu = Math.Clamp(100d * (total - idleDelta) / total, 0, 100);
            _lastIdle = i; _lastKernel = k; _lastUser = u;
        }
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() }; ulong usedBytes, totalBytes;
        if (OperatingSystem.IsWindows() && GlobalMemoryStatusEx(ref memory)) { totalBytes = memory.TotalPhys; usedBytes = totalBytes - memory.AvailPhys; }
        else { totalBytes = 0; usedBytes = 0; }
        var root = Path.GetPathRoot(Environment.SystemDirectory) ?? Path.DirectorySeparatorChar.ToString();
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && string.Equals(d.Name, root, StringComparison.OrdinalIgnoreCase));
        double diskTotal = drive?.TotalSize / Math.Pow(1024, 3) ?? 0;
        double diskUsed = drive is { IsReady: true } ? (drive.TotalSize - drive.AvailableFreeSpace) / Math.Pow(1024, 3) : 0;
        long received = 0, sent = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        { try { var stats = nic.GetIPv4Statistics(); received += stats.BytesReceived; sent += stats.BytesSent; } catch (NetworkInformationException) { } }
        var seconds = Math.Max((now - _lastNetwork).TotalSeconds, .001);
        var downMbps = _lastNetwork == now || _lastReceived == 0 ? 0 : Math.Max(0, (received - _lastReceived) * 8 / seconds / 1_000_000d);
        var upMbps = _lastNetwork == now || _lastSent == 0 ? 0 : Math.Max(0, (sent - _lastSent) * 8 / seconds / 1_000_000d);
        _lastReceived = received; _lastSent = sent; _lastNetwork = now;
        var running = Process.GetProcesses(); var processCount = running.Length; foreach (var process in running) process.Dispose();
        return new SystemSnapshot(now, cpu, totalBytes == 0 ? 0 : usedBytes / (double)totalBytes * 100, usedBytes / Math.Pow(1024, 3), totalBytes / Math.Pow(1024, 3), diskTotal <= 0 ? 0 : diskUsed / diskTotal * 100, diskUsed, diskTotal, downMbps, upMbps, processCount, Environment.MachineName, RuntimeInformation.OSDescription, TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"d\.hh\:mm\:ss"));
    }
    public Task<IReadOnlyList<ProcessInfo>> GetProcessesAsync(CancellationToken ct = default) => Task.Run<IReadOnlyList<ProcessInfo>>(() =>
    {
        var now = DateTime.UtcNow; var list = new List<ProcessInfo>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var cpu = p.TotalProcessorTime; double percent = 0;
                lock (_processLock) { if (_processCpu.TryGetValue(p.Id, out var prior)) { var elapsed = (now - prior.At).TotalSeconds; if (elapsed > 0) percent = Math.Clamp((cpu - prior.Cpu).TotalSeconds / elapsed / Environment.ProcessorCount * 100, 0, 100); } _processCpu[p.Id] = (cpu, now); }
                list.Add(new ProcessInfo(p.Id, p.ProcessName, percent, p.WorkingSet64 / 1024d / 1024d, p.MainWindowTitle, p.Responding ? "Running" : "Not responding"));
            }
            catch { }
            finally { p.Dispose(); }
        }
        return list.OrderByDescending(p => p.MemoryMb).ToArray();
    }, ct);
    public async Task TerminateProcessAsync(int processId)
    {
        await Task.Run(() => { using var process = Process.GetProcessById(processId); if (process.Id == Environment.ProcessId) throw new InvalidOperationException("Nexus cannot terminate itself."); process.Kill(true); });
    }
    [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint Low; public uint High; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] private struct MemoryStatus { public uint Length; public uint MemoryLoad; public ulong TotalPhys; public ulong AvailPhys; public ulong TotalPageFile; public ulong AvailPageFile; public ulong TotalVirtual; public ulong AvailVirtual; public ulong AvailExtendedVirtual; }
    private static ulong ToUInt64(FileTime value) => ((ulong)value.High << 32) | value.Low;
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);
}
