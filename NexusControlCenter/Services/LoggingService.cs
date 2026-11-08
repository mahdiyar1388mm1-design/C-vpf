using Microsoft.Extensions.Logging;
using NexusControlCenter.Models;
using NexusControlCenter.Repositories;
namespace NexusControlCenter.Services;
public sealed class LoggingService(IActivityRepository repository, ILogger<LoggingService> logger)
{
    public async Task RecordAsync(string category, string message, CancellationToken ct = default)
    {
        try { await repository.AddAsync(new ActivityRecord { Category = category, Message = message }, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not persist activity"); }
    }
    public async Task<IReadOnlyList<ActivityRecord>> GetRecentAsync(int count = 12, CancellationToken ct = default)
    {
        try { return await repository.GetRecentAsync(count, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not read recent activity"); return Array.Empty<ActivityRecord>(); }
    }
}
