using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Id)
            .IsRequired();

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.KeyHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(k => k.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(k => k.RateLimitPerMinute)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(k => k.Created)
            .IsRequired();

        builder.Property(k => k.Updated);

        builder.Property(k => k.Deleted);

        builder.Property(k => k.CreatedBy);

        builder.Property(k => k.UpdatedBy);

        builder.Property(k => k.DeletedBy);

        builder.HasIndex(k => k.KeyHash)
            .IsUnique();

        builder.HasIndex(k => new { k.IsActive, k.ExpiresAt });

        builder.HasQueryFilter(k => k.Deleted == null);
    }
}

