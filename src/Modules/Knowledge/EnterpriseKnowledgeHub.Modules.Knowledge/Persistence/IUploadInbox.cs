namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public interface IUploadInbox
{
    Task EnqueueAsync(IEnumerable<UploadInboxItem> items, CancellationToken cancellationToken);
    Task<int> CompleteAsync(UploadInboxItem item, CancellationToken cancellationToken);
    Task<int> RetryAsync(UploadInboxItem item, string errorCode, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to acquire an exclusive, time-limited lease on the next inbox item that is
    /// ready for processing.
    /// </summary>
    /// <remarks>
    /// An item is eligible when it is pending and its next-attempt time has arrived, or
    /// when it was being processed but its previous lease has expired. Candidate IDs are
    /// read in next-attempt order, up to the configured batch size. Each candidate is then
    /// claimed with one conditional database update that marks it as processing, assigns a
    /// new lease owner, sets the lease expiration, and increments the attempt count.
    ///
    /// The conditional update makes claiming safe when multiple worker instances compete:
    /// only the worker that updates the row receives it. The method returns that leased
    /// item, or <see langword="null"/> when no eligible item can be claimed. No database
    /// transaction is kept open while the caller processes the returned work.
    /// </remarks>
    Task<UploadInboxItem?> ClaimAsync(CancellationToken cancellationToken);
}
