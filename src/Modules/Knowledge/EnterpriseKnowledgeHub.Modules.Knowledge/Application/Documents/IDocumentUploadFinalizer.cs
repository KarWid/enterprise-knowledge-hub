using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public interface IDocumentUploadFinalizer
{
    Task ProcessAsync(UploadInboxItem work, CancellationToken cancellationToken);
}

