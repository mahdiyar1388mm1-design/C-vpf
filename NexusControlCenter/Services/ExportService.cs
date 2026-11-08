using NexusControlCenter.Models;
using System.Text;
using System.Globalization;
namespace NexusControlCenter.Services;
public sealed class ExportService
{
    public async Task<string> ExportCsvAsync(IEnumerable<SystemSnapshot> snapshots, CancellationToken ct = default)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"Nexus-performance-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        var lines = new List<string> { "Timestamp,CPU %,Memory %,Memory Used GB,Disk %,Download Mbps,Upload Mbps,Processes" };
        lines.AddRange(snapshots.Select(s => string.Join(",", new[] { s.Timestamp.ToLocalTime().ToString("O", CultureInfo.InvariantCulture), s.CpuPercent.ToString("F1", CultureInfo.InvariantCulture), s.MemoryPercent.ToString("F1", CultureInfo.InvariantCulture), s.MemoryUsedGb.ToString("F2", CultureInfo.InvariantCulture), s.DiskPercent.ToString("F1", CultureInfo.InvariantCulture), s.NetworkDownMbps.ToString("F2", CultureInfo.InvariantCulture), s.NetworkUpMbps.ToString("F2", CultureInfo.InvariantCulture), s.ProcessCount.ToString(CultureInfo.InvariantCulture) })));
        await File.WriteAllLinesAsync(path, lines, new UTF8Encoding(true), ct); return path;
    }
    public async Task<string> ExportPdfAsync(IEnumerable<SystemSnapshot> snapshots, CancellationToken ct = default)
    {
        var lines = new[] { "NEXUS CONTROL CENTER | PERFORMANCE REPORT", $"Generated {DateTime.Now:g}", "", "TIME                 CPU    MEMORY   DISK   DOWN Mbps  UP Mbps  PROCESSES" }
            .Concat(snapshots.Select(s => $"{s.Timestamp.ToLocalTime():MM-dd HH:mm:ss}       {s.CpuPercent,5:F1}% {s.MemoryPercent,6:F1}% {s.DiskPercent,6:F1}% {s.NetworkDownMbps,8:F2}  {s.NetworkUpMbps,7:F2}  {s.ProcessCount,4}"));
        var content = "BT /F1 10 Tf 48 790 Td 14 TL " + string.Join(" T* ", lines.Take(48).Select(l => $"({Escape(l)}) Tj")) + " ET";
        var stream = Encoding.ASCII.GetBytes(content); var objects = new List<byte[]>();
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"));
        objects.Add(Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n{content}\nendstream"));
        using var ms = new MemoryStream(); using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, true); writer.Write("%PDF-1.4\n"); writer.Flush(); var offsets = new List<long>();
        foreach (var (obj, i) in objects.Select((o, i) => (o, i + 1))) { offsets.Add(ms.Position); writer.Write($"{i} 0 obj\n"); writer.Flush(); ms.Write(obj); writer.Write("\nendobj\n"); writer.Flush(); }
        var xref = ms.Position; writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n"); foreach (var offset in offsets) writer.Write($"{offset:0000000000} 00000 n \n"); writer.Write($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); writer.Flush();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"Nexus-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf"); await File.WriteAllBytesAsync(path, ms.ToArray(), ct); return path;
    }
    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Where(c => c <= 127).Aggregate(new StringBuilder(), (b, c) => b.Append(c)).ToString();
}
