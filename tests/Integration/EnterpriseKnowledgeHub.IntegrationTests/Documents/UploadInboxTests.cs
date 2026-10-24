using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class UploadInboxTests
{
    private const string Source = "test-source";
    private const string BlobReference = "reserved.pdf";
    private const string DependencyFailure = "dependency_failure";

    [Fact]
    public async Task EnqueueAsync_WhenDeliveryIsDuplicated_PersistsOneItem()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var eventId = Guid.NewGuid().ToString();
        var duplicateDeliveries = new[]
        {
            Item(fixture, eventId),
            Item(fixture, eventId)
        };

        // Act
        await fixture.Inbox(db).EnqueueAsync(duplicateDeliveries, CancellationToken.None);

        // Assert
        await using var assertDb = fixture.CreateContext();
        var persistedItems = await assertDb.UploadInbox.AsNoTracking().ToListAsync();
        Assert.True(
            persistedItems.Count == 1,
            "Deliveries with the same source and event ID must create only one durable inbox item.");
    }

    [Fact]
    public async Task DatabaseIndex_WhenSourceAndEventIdAreDuplicated_RejectsSecondItem()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await using var db = fixture.CreateContext();
        var eventId = Guid.NewGuid().ToString();
        db.UploadInbox.Add(Item(fixture, eventId));
        await db.SaveChangesAsync();
        db.UploadInbox.Add(Item(fixture, eventId));

        // Act
        Task SaveDuplicate() => db.SaveChangesAsync();

        // Assert
        // The database index is the final defense against concurrent duplicate deliveries.
        await Assert.ThrowsAsync<DbUpdateException>(SaveDuplicate);
    }

    [Fact]
    public async Task ClaimAsync_WhenTwoWorkersCompete_GrantsOneLease()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await EnqueueOneItemAsync(fixture);

        // Act
        var claims = await Task.WhenAll(
            Task.Run(() => ClaimUsingNewContextAsync(fixture)),
            Task.Run(() => ClaimUsingNewContextAsync(fixture)));

        // Assert
        var grantedClaims = claims.Where(claim => claim is not null).ToList();
        Assert.True(
            grantedClaims.Count == 1,
            "An inbox item must have only one active lease even when workers claim it concurrently.");
    }

    [Fact]
    public async Task ClaimAsync_WhenLeaseIsActive_DoesNotGrantAnotherLease()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await EnqueueOneItemAsync(fixture);
        await ClaimRequiredAsync(fixture);

        // Act
        var competingClaim = await ClaimUsingNewContextAsync(fixture);

        // Assert
        Assert.True(
            competingClaim is null,
            "A worker must not claim work that is still leased to another worker.");
    }

    [Fact]
    public async Task ClaimAsync_WhenLeaseExpires_RecoversWorkWithNewOwner()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await EnqueueOneItemAsync(fixture);
        var expiredClaim = await ClaimRequiredAsync(fixture);
        fixture.Clock.Advance(fixture.Options.LeaseSeconds + 1);

        // Act
        var recoveredClaim = await ClaimUsingNewContextAsync(fixture);

        // Assert
        Assert.NotNull(recoveredClaim);
        Assert.True(
            recoveredClaim.LeaseOwner != expiredClaim.LeaseOwner,
            "Recovered work must receive a new lease owner so the expired owner cannot update it.");
        Assert.True(
            recoveredClaim.Attempts == 2,
            "Recovering an expired lease is another processing attempt and must increment the attempt count.");
    }

    [Fact]
    public async Task CompleteAndRetryAsync_WhenLeaseWasRecovered_RejectExpiredOwner()
    {
        // Arrange
        using var fixture = new UploadTestDatabase();
        await EnqueueOneItemAsync(fixture);
        var expiredClaim = await ClaimRequiredAsync(fixture);

        // The first worker stops owning the item when its lease expires.
        fixture.Clock.Advance(fixture.Options.LeaseSeconds + 1);

        // ClaimAsync recovers the same item and assigns a new LeaseOwner GUID.
        var recoveredClaim = await ClaimRequiredAsync(fixture);
        await using var db = fixture.CreateContext();
        var inbox = fixture.Inbox(db);

        // Act
        var completedByExpiredOwner = await inbox.CompleteAsync(expiredClaim, CancellationToken.None);
        var retriedByExpiredOwner = await inbox.RetryAsync(
            expiredClaim,
            DependencyFailure,
            CancellationToken.None);

        // Assert
        Assert.NotEqual(expiredClaim.LeaseOwner, recoveredClaim.LeaseOwner);
        Assert.True(
            completedByExpiredOwner == 0,
            "A worker must not complete work after its lease has expired and another worker has recovered it.");
        Assert.True(
            retriedByExpiredOwner == 0,
            "A worker must not reschedule work after its lease has expired and another worker has recovered it.");
    }

    [Fact]
    public async Task CompleteAsync_WhenCallerOwnsActiveLease_CompletesWorkOnce()
    {
        // Arrange
        // Create one pending item and lease it to the worker represented by activeClaim.
        using var fixture = new UploadTestDatabase();
        await EnqueueOneItemAsync(fixture);
        var activeClaim = await ClaimRequiredAsync(fixture);
        await using var db = fixture.CreateContext();
        var inbox = fixture.Inbox(db);

        // Act
        // CompleteAsync receives the same lease owner that ClaimAsync assigned.
        var completedItems = await inbox.CompleteAsync(activeClaim, CancellationToken.None);

        // Try to claim the item again after it has been completed.
        var claimAfterCompletion = await ClaimUsingNewContextAsync(fixture);

        // Assert
        Assert.True(
            completedItems == 1,
            "The worker that owns the active lease must complete exactly one inbox item.");
        Assert.True(
            claimAfterCompletion is null,
            "Completed work must not become eligible for processing again.");
    }

    [Fact]
    public async Task RetryAsync_WhenMaximumAttemptsAreExhausted_MovesWorkToDeadLetter()
    {
        // Arrange
        // Create an isolated in-memory SQLite database with a controllable clock.
        using var fixture = new UploadTestDatabase();

        // Insert one pending inbox item that is immediately eligible for processing.
        await EnqueueOneItemAsync(fixture);

        var recordedAttemptCounts = new List<int>();
        var immediateClaimsAfterRetry = new List<UploadInboxItem?>();

        // Act
        // Simulate a transient processing failure for every permitted attempt.
        for (var attempt = 1; attempt <= fixture.Options.MaxAttempts; attempt++)
        {
            // Claiming leases the item and atomically increments its Attempts value.
            var claim = await ClaimRequiredAsync(fixture);

            // Preserve the observed attempt sequence for assertions after the workflow finishes.
            recordedAttemptCounts.Add(claim.Attempts);

            // Use a separate DbContext, as a real worker iteration would use its own scope.
            await using var retryDb = fixture.CreateContext();

            // RetryAsync schedules another attempt, or dead-letters the item on the final failure.
            await fixture.Inbox(retryDb).RetryAsync(
                claim,
                DependencyFailure,
                CancellationToken.None);

            // A retry must not be claimable immediately; the final dead-letter item is never claimable.
            immediateClaimsAfterRetry.Add(await ClaimUsingNewContextAsync(fixture));

            // Advance beyond the maximum possible backoff so the next retry becomes eligible.
            fixture.Clock.Advance(fixture.Options.RetryMaxSeconds + 1);
        }

        // Assert
        // Read the final persisted state through a fresh context.
        await using var assertDb = fixture.CreateContext();

        // Only one inbox item should exist throughout all retry attempts.
        var item = await assertDb.UploadInbox.AsNoTracking().SingleAsync();

        // Every claim must increment Attempts exactly once, from 1 through MaxAttempts.
        Assert.Equal(Enumerable.Range(1, fixture.Options.MaxAttempts), recordedAttemptCounts);

        // Backoff and the terminal state must both prevent immediate reprocessing.
        Assert.True(
            immediateClaimsAfterRetry.All(claim => claim is null),
            "Failed work must not be claimable immediately: retries wait for backoff and exhausted work remains dead-lettered.");

        // Exhausting MaxAttempts moves the work into its terminal failure state.
        Assert.Equal(UploadWorkStatus.DeadLetter, item.Status);

        // Dead-lettering records when processing was permanently stopped.
        Assert.NotNull(item.CompletedAt);

        // The persisted error is the sanitized code supplied by the worker.
        Assert.Equal(DependencyFailure, item.LastError);
    }

    internal static UploadInboxItem Item(UploadTestDatabase fixture, string? eventId = null) => new()
    {
        Source = Source,
        EventId = eventId ?? Guid.NewGuid().ToString(),
        BlobReference = BlobReference,
        CreatedAt = fixture.Clock.Now.UtcDateTime,
        NextAttemptAt = fixture.Clock.Now.UtcDateTime
    };

    private static async Task EnqueueOneItemAsync(UploadTestDatabase fixture)
    {
        await using var db = fixture.CreateContext();
        await fixture.Inbox(db).EnqueueAsync([Item(fixture)], CancellationToken.None);
    }

    private static async Task<UploadInboxItem?> ClaimUsingNewContextAsync(UploadTestDatabase fixture)
    {
        await using var db = fixture.CreateContext();
        return await fixture.Inbox(db).ClaimAsync(CancellationToken.None);
    }

    private static async Task<UploadInboxItem> ClaimRequiredAsync(UploadTestDatabase fixture) =>
        await ClaimUsingNewContextAsync(fixture)
        ?? throw new InvalidOperationException("Expected the inbox item to be eligible for claiming.");
}
