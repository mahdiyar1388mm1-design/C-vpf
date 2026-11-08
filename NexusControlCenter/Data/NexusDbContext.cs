using Microsoft.EntityFrameworkCore;
using NexusControlCenter.Models;
namespace NexusControlCenter.Data;
public sealed class NexusDbContext(DbContextOptions<NexusDbContext> options) : DbContext(options)
{
    public DbSet<ActivityRecord> Activities => Set<ActivityRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<ActivityRecord>().HasIndex(x => x.Timestamp);
}
