using Microsoft.EntityFrameworkCore;
using cs_api_v1.Entities;

namespace cs_api_v1.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Load semua IEntityTypeConfiguration di assembly ini.
        // Tabel ke-3 s/d ke-100 tinggal tambah file di Data/Configs, DbContext tidak berubah.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
