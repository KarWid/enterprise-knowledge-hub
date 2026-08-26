using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Persistence.Configurations;

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships", "organizations");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.OrganizationId)
            .IsRequired();

        builder.HasIndex(m => new { m.UserId, m.OrganizationId })
            .IsUnique();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(m => m.CreatedAt)
            .IsRequired();
    }
}
