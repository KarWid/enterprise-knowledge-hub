using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Identity.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
}
