using CryptoPriceTraker.Core.Interfaces;
using CryptoPriceTraker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoPriceTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    public DbSet<CryptoAsset> CryptoAssets { get; set; }
    public DbSet<CryptoPriceHistory> CryptoPriceHistories { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CryptoAsset>().HasData(
            new CryptoAsset { Id = 1, Name = "Bitcoin", Symbol = "BTC", ExternalId = "bitcoin" },
            new CryptoAsset { Id = 2, Name = "Ethereum", Symbol = "ETH", ExternalId = "ethereum" }
        );
    }
}