using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroDocuments.Domain.Entities;

namespace MicroDocuments.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .IsRequired();

        builder.Property(d => d.Filename)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.CustomerId)
            .HasMaxLength(100);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Url)
            .HasMaxLength(1000);

        builder.Property(d => d.Size)
            .IsRequired();

        builder.Property(d => d.UploadDate)
            .IsRequired();

        builder.Property(d => d.CorrelationId)
            .HasMaxLength(100);

        builder.Property(d => d.Created)
            .IsRequired();

        builder.Property(d => d.Updated);

        builder.Property(d => d.Deleted);

        builder.Property(d => d.CreatedBy);

        builder.Property(d => d.UpdatedBy);

        builder.Property(d => d.DeletedBy);

        builder.HasIndex(d => d.CustomerId);
        builder.HasIndex(d => d.UploadDate);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.DocumentType);
        builder.HasIndex(d => d.Channel);
        builder.HasIndex(d => d.CorrelationId);

        builder.HasQueryFilter(d => d.Deleted == null);
    }
}

