using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents;

public sealed class DocumentChunkingStatusResolverTests
{
    [Theory]
    [InlineData(true, true, TextExtractionStatus.Completed, null, "Pending")]
    [InlineData(true, true, TextExtractionStatus.Pending, null, "NotAvailable")]
    [InlineData(false, true, TextExtractionStatus.Completed, null, "Disabled")]
    [InlineData(false, true, TextExtractionStatus.Completed, DocumentChunkingStatus.Pending, "Paused")]
    [InlineData(true, false, TextExtractionStatus.Completed, DocumentChunkingStatus.Pending, "Paused")]
    [InlineData(true, true, TextExtractionStatus.Completed, DocumentChunkingStatus.Processing, "Processing")]
    [InlineData(false, false, TextExtractionStatus.Completed, DocumentChunkingStatus.Completed, "Completed")]
    [InlineData(true, true, TextExtractionStatus.Completed, DocumentChunkingStatus.Failed, "Failed")]
    [InlineData(true, true, TextExtractionStatus.Completed, DocumentChunkingStatus.DeadLetter, "DeadLetter")]
    [InlineData(false, false, TextExtractionStatus.Pending, DocumentChunkingStatus.Failed, "Failed")]
    [InlineData(false, false, TextExtractionStatus.Pending, DocumentChunkingStatus.DeadLetter, "DeadLetter")]
    [InlineData(false, false, TextExtractionStatus.Pending, null, "Disabled")]
    [InlineData(true, false, TextExtractionStatus.Pending, null, "Paused")]
    [InlineData(false, true, TextExtractionStatus.Completed, DocumentChunkingStatus.Processing, "Paused")]
    public void Resolve_DerivesAvailabilityWithoutDatabaseAccess(
        bool formatEnabled, 
        bool workerEnabled,
        TextExtractionStatus extractionStatus, 
        DocumentChunkingStatus? storedStatus, 
        string expectedStatus)
    {
        // Arrange
        var options = new DocumentChunkingOptions();
        options.EnabledFormats["markdown"] = formatEnabled;
        var resolver = new DocumentChunkingStatusResolver(new DocumentChunkerRegistry([]), Options.Create(options),
            Options.Create(new DocumentChunkingWorkerOptions { Enabled = workerEnabled }));
        var completedAt = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
        var projection = storedStatus is { } status
            ? new DocumentChunkingStatusProjection(status, status == DocumentChunkingStatus.Completed ? 3 : null, completedAt, null)
            : null;

        // Act
        var result = resolver.Resolve(DocumentStatus.Uploaded, "notes.md", "verified", extractionStatus, projection);

        // Assert
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(storedStatus == DocumentChunkingStatus.Completed ? completedAt : (DateTime?)null, result.CompletedAt);
    }
}
