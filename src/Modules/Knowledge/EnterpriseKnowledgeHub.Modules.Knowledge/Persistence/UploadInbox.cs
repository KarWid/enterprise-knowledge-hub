using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

internal sealed class UploadInbox(
    KnowledgeDbContext db,
    TimeProvider clock,
    IOptions<DocumentUploadWorkerOptions> options) : IUploadInbox
{
    public async Task EnqueueAsync(IEnumerable<UploadInboxItem> items, CancellationToken ct)
    {
        // One committed insert at a time. A partial batch failure is retried by Event Grid;
        // already committed items are deduplicated by the unique database index.
        foreach (var item in items)
        {
            if (await db.UploadInbox.AnyAsync(x => x.Source == item.Source && x.EventId == item.EventId, ct))
            {
                continue;
            }

            db.UploadInbox.Add(item);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                db.Entry(item).State = EntityState.Detached;
                // Only a confirmed duplicate is successful; all other database failures propagate.
                if (!await db.UploadInbox.AnyAsync(x => x.Source == item.Source && x.EventId == item.EventId, ct))
                    throw;
            }
        }
    }

    public async Task<UploadInboxItem?> ClaimAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await Eligible(now)
            .OrderBy(x => x.NextAttemptAt)
            .Select(x => x.Id)
            .Take(options.Value.BatchSize)
            .ToListAsync(ct);

        foreach (var id in candidates)
        {
            var owner = Guid.NewGuid();

            // The predicate and update execute as ONE database statement. A select alone is not a lease.
            var updated = await Eligible(now).Where(x => x.Id == id).ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, UploadWorkStatus.Processing)
                .SetProperty(x => x.LeaseOwner, owner)
                .SetProperty(x => x.LeaseExpiresAt, now.AddSeconds(options.Value.LeaseSeconds))
                .SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);

            if (updated == 1)
            {
                return await db.UploadInbox.AsNoTracking().SingleAsync(x => x.Id == id && x.LeaseOwner == owner, ct);
            }
        }

        return null;
    }

    public Task<int> CompleteAsync(UploadInboxItem item, CancellationToken ct) => Owned(item)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Status, UploadWorkStatus.Completed)
            .SetProperty(x => x.CompletedAt, clock.GetUtcNow().UtcDateTime)
            .SetProperty(x => x.LastError, (string?)null)
            .SetProperty(x => x.LeaseOwner, (Guid?)null)
            .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), ct);

    public Task<int> RetryAsync(UploadInboxItem item, string errorCode, CancellationToken ct)
    {
        var exhausted = item.Attempts >= options.Value.MaxAttempts;
        var delay = Math.Min(options.Value.RetryMaxSeconds,
            options.Value.RetryBaseSeconds * Math.Pow(2, Math.Min(item.Attempts - 1, 20)));
            
        return Owned(item).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Status, exhausted ? UploadWorkStatus.DeadLetter : UploadWorkStatus.Pending)
            .SetProperty(x => x.NextAttemptAt, clock.GetUtcNow().UtcDateTime.AddSeconds(delay))
            .SetProperty(x => x.CompletedAt, exhausted ? clock.GetUtcNow().UtcDateTime : (DateTime?)null)
            .SetProperty(x => x.LastError, errorCode)
            .SetProperty(x => x.LeaseOwner, (Guid?)null)
            .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), ct);
    }

    private IQueryable<UploadInboxItem> Eligible(DateTime now) => db.UploadInbox.Where(x =>
        (x.Status == UploadWorkStatus.Pending && x.NextAttemptAt <= now) ||
        (x.Status == UploadWorkStatus.Processing && x.LeaseExpiresAt <= now));

    private IQueryable<UploadInboxItem> Owned(UploadInboxItem item)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return db.UploadInbox.Where(x => x.Id == item.Id && x.Status == UploadWorkStatus.Processing &&
            x.LeaseOwner == item.LeaseOwner && x.LeaseExpiresAt > now);
    }
}
