using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<UploadInboxItem> UploadInbox => Set<UploadInboxItem>();
    public DbSet<DocumentTextExtraction> DocumentTextExtractions => Set<DocumentTextExtraction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        modelBuilder.ApplyConfiguration(new UploadInboxItemConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentTextExtractionConfiguration());
    }
}
