namespace NexusControlCenter.Models;
public sealed class ActivityRecord
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = "System";
    public string Message { get; set; } = "";
}
