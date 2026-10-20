namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public interface IDocumentUploadReconciler
{
    Task ScheduleAsync(CancellationToken cancellationToken);
}
