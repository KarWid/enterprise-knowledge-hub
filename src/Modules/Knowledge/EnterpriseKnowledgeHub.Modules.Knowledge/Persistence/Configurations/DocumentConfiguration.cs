using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", "knowledge");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .ValueGeneratedNever();

        builder.Property(document => document.OrganizationId)
            .IsRequired();

        builder.Property(document => document.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(document => document.BlobReference)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(document => document.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(document => document.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(document => document.CreatedBy)
            .IsRequired();

        builder.Property(document => document.CreatedAt)
            .IsRequired();

        builder.Property(document => document.UploadExpiresAt)
            .IsRequired(false);

        builder.HasIndex(document => new { document.OrganizationId, document.CreatedAt });
        builder.HasIndex(document => document.BlobReference).IsUnique();
    }
}
