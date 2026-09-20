using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using cs_api_v1.Entities;

namespace cs_api_v1.Data.Configs;

// Template config per tabel: 1 file per entity.
// DbContext otomatis load semua via ApplyConfigurationsFromAssembly.
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> e)
    {
        e.ToTable("products");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).UseIdentityAlwaysColumn();
        e.Property(x => x.Uuid)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1000);
        e.Property(x => x.Price).HasPrecision(18, 2);
        e.HasIndex(x => x.Uuid).HasMethod("hash");
        e.HasIndex(x => x.Name);
        e.HasIndex(x => x.CategoryId);

        e.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
