using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;

internal sealed record GetDocumentsQuery : IRequest<IReadOnlyList<DocumentResult>>;
