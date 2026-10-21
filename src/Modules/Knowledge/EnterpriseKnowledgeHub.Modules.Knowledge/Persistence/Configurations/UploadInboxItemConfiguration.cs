using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;

internal sealed class UploadInboxItemConfiguration : IEntityTypeConfiguration<UploadInboxItem>
{
    public void Configure(EntityTypeBuilder<UploadInboxItem> builder)
    {
        builder.ToTable("UploadInbox", "knowledge");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Source).HasMaxLength(512);
        builder.Property(item => item.EventId).HasMaxLength(128);
        builder.Property(item => item.BlobReference).HasMaxLength(512);
        builder.Property(item => item.ETag).HasMaxLength(128);
        builder.Property(item => item.LastError).HasMaxLength(64);
        builder.Property(item => item.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(item => new { item.Source, item.EventId }).IsUnique();
        builder.HasIndex(item => new { item.Kind, item.BlobReference });
        builder.HasIndex(item => new { item.Status, item.NextAttemptAt, item.LeaseExpiresAt });
    }
}
