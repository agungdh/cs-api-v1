using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using cs_api_v1.Entities;

namespace cs_api_v1.Data.Configs;

// Template config per tabel: 1 file per entity.
// DbContext otomatis load semua via ApplyConfigurationsFromAssembly.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.ToTable("categories");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).UseIdentityAlwaysColumn();
        e.Property(x => x.Uuid)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1000);
        e.HasIndex(x => x.Uuid).HasMethod("hash");
        e.HasIndex(x => x.Name).IsUnique();
    }
}
