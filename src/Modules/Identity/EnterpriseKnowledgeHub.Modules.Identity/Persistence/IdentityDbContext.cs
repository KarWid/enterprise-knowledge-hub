using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Identity.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
    }
}
