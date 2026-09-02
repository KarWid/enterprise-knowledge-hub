using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Persistence.Configurations
{
    internal sealed class OrganizationInvitationConfiguration
        : IEntityTypeConfiguration<OrganizationInvitation>
    {
        public void Configure(
            EntityTypeBuilder<OrganizationInvitation> builder)
        {
            builder.ToTable("OrganizationInvitations", "organizations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.OrganizationId,
                x.Email,
                x.Status
            });
        }
    }
}
