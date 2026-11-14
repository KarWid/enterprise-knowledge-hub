using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentTextPreview;

internal sealed record GetDocumentTextPreviewQuery(Guid DocumentId) : IRequest<DocumentTextPreviewResult?>;
