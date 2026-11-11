using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;

internal sealed class DocumentTextExtractionConfiguration : IEntityTypeConfiguration<DocumentTextExtraction>
{
    public void Configure(EntityTypeBuilder<DocumentTextExtraction> builder)
    {
        builder.ToTable("DocumentTextExtractions", "knowledge");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.SourceETag).HasMaxLength(128);
        builder.Property(item => item.FormatId).HasMaxLength(32);
        builder.Property(item => item.ExtractorVersion).HasMaxLength(64);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.LastError).HasMaxLength(64);
        // SQL Server maps an unbounded Unicode string to nvarchar(max).
        builder.Property(item => item.Text).IsUnicode();
        builder.HasIndex(item => new { item.OrganizationId, item.DocumentId, item.SourceETag }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.NextAttemptAt, item.LeaseExpiresAt });
        builder.HasOne<Document>().WithMany()
            .HasForeignKey(item => new { item.OrganizationId, item.DocumentId })
            .HasPrincipalKey(document => new { document.OrganizationId, document.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
