using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Persistence;

public class OrganizationsDbContext(DbContextOptions<OrganizationsDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Membership> Memberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new MembershipConfiguration());
    }
}
