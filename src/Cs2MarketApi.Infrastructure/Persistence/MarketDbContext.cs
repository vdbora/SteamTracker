using Cs2MarketApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cs2MarketApi.Infrastructure.Persistence;

public class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
    public DbSet<ItemPrice> ItemPrices => Set<ItemPrice>();
    public DbSet<GamePriceEntry> GamePrices => Set<GamePriceEntry>();
    public DbSet<GameLowEntry> GameLows => Set<GameLowEntry>();

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

        b.Entity<GamePriceEntry>(e =>
        {
            e.HasKey(x => x.AppId);
            e.Property(x => x.AppId).ValueGeneratedNever();
            e.Property(x => x.Currency).HasMaxLength(8);
        });

        b.Entity<GameLowEntry>(e =>
        {
            e.HasKey(x => x.AppId);
            e.Property(x => x.AppId).ValueGeneratedNever();
            e.Property(x => x.Currency).HasMaxLength(8);
        });
    }
}
