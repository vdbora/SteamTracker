using Cs2MarketApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cs2MarketApi.Infrastructure.Persistence;

public class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
    public DbSet<ItemPrice> ItemPrices => Set<ItemPrice>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ItemPrice>(e =>
        {
            e.Property(p => p.MarketHashName).HasMaxLength(255).IsRequired();
            e.Property(p => p.Source).HasMaxLength(32).IsRequired();
            e.Property(p => p.Price).HasPrecision(18, 4);
            e.HasIndex(p => new { p.MarketHashName, p.Source }).IsUnique();
            e.HasIndex(p => p.UpdatedAtUtc);
        });
    }
}
