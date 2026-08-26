using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Identity.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers", "identity");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.ExternalIdentityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.ExternalIdentityId)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
