using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DocumentConfiguration());
    }
}
