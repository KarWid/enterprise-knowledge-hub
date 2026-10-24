using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;

internal sealed record GetDocumentQuery(Guid DocumentId) : IRequest<DocumentResult?>;
