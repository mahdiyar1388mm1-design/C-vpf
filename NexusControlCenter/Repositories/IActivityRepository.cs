using NexusControlCenter.Models;
namespace NexusControlCenter.Repositories;
public interface IActivityRepository
{
    Task AddAsync(ActivityRecord activity, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityRecord>> GetRecentAsync(int count, CancellationToken ct = default);
}
