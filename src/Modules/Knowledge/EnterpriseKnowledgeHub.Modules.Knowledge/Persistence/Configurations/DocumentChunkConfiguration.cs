using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks", "knowledge");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Content).IsUnicode();
        builder.Property(item => item.HeadingPath).HasMaxLength(2000);
        builder.HasIndex(item => new { item.ChunkingId, item.Ordinal }).IsUnique();

        builder.HasOne<DocumentChunking>()
            .WithMany()
            .HasForeignKey(item => new { item.OrganizationId, item.DocumentId, item.ChunkingId })
            .HasPrincipalKey(chunking => new { chunking.OrganizationId, chunking.DocumentId, chunking.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
