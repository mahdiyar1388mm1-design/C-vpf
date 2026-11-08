using Microsoft.EntityFrameworkCore;
using NexusControlCenter.Data;
using NexusControlCenter.Models;
namespace NexusControlCenter.Repositories;
public sealed class ActivityRepository(IDbContextFactory<NexusDbContext> factory) : IActivityRepository
{
    public async Task AddAsync(ActivityRecord activity, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Activities.Add(activity);
        await db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<ActivityRecord>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Activities.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(Math.Clamp(count, 0, 500)).ToListAsync(ct);
    }
}
