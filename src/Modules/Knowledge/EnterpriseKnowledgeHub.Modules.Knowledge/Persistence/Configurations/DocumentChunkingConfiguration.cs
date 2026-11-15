using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;

internal sealed class DocumentChunkingConfiguration : IEntityTypeConfiguration<DocumentChunking>
{
    public void Configure(EntityTypeBuilder<DocumentChunking> builder)
    {
        builder.ToTable("DocumentChunkings", "knowledge");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.HasAlternateKey(item => new { item.OrganizationId, item.DocumentId, item.Id });
        
        builder.Property(item => item.SourceETag).HasMaxLength(128);
        builder.Property(item => item.FormatId).HasMaxLength(32);
        builder.Property(item => item.ChunkerVersion).HasMaxLength(64);
        builder.Property(item => item.ConfigurationKey).HasMaxLength(64);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.LastError).HasMaxLength(64);

        builder.HasIndex(item => new { item.ExtractionId, item.ConfigurationKey }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.NextAttemptAt, item.LeaseExpiresAt });
        
        builder.HasOne<DocumentTextExtraction>().WithMany()
            .HasForeignKey(item => new { item.OrganizationId, item.DocumentId, item.ExtractionId })
            .HasPrincipalKey(extraction => new { extraction.OrganizationId, extraction.DocumentId, extraction.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
