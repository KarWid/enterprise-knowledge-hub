using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentTextPreview;

internal sealed class GetDocumentTextPreviewQueryHandler(KnowledgeDbContext db, IOrganizationAccessService access)
    : IRequestHandler<GetDocumentTextPreviewQuery, DocumentTextPreviewResult?>
{
    public async ValueTask<DocumentTextPreviewResult?> Handle(GetDocumentTextPreviewQuery request, CancellationToken cancellationToken)
    {
        var membership = await access.GetCurrentOrganizationMembershipAsync(cancellationToken);
        const int limit = DocumentTextExtractionOptions.PreviewCharacterLimit;
        
        var preview = await db.Documents
            .AsNoTracking()
            .Where(document =>
                document.Id == request.DocumentId &&
                document.OrganizationId == membership.OrganizationId &&
                document.Status == DocumentStatus.Uploaded)
            .Join(
                db.DocumentTextExtractions
                    .AsNoTracking()
                    .Where(extraction =>
                        extraction.Status == TextExtractionStatus.Completed &&
                        extraction.Text != null),
                document => new
                {
                    document.OrganizationId,
                    DocumentId = document.Id,
                    SourceETag = document.ValidatedETag
                },
                extraction => new
                {
                    extraction.OrganizationId,
                    extraction.DocumentId,
                    SourceETag = (string?)extraction.SourceETag
                },
                (_, extraction) => new
                {
                    // Substring is translated to SQL: the full source text never reaches this endpoint.
                    // Always bound the SQL projection: SQL Server LEN ignores trailing spaces.
                    // One extra character allows UTF-16-safe truncation after materialization.
                    Text = extraction.Text!.Substring(0, limit + 1),
                    extraction.CharacterCount,
                    extraction.CompletedAt
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (preview is null)
        {
            return null;
        }

        var text = preview.Text.Length > limit ? preview.Text[..limit] : preview.Text;
        if (text.Length > 0 && char.IsHighSurrogate(text[^1]))
        {
            text = text[..^1];
        }

        return new(text, preview.CharacterCount!.Value, preview.CharacterCount > text.Length, preview.CompletedAt!.Value);
    }
}
