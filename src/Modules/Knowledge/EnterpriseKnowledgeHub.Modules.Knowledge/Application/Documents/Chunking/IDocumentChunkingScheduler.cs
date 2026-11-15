namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public interface IDocumentChunkingScheduler
{
    Task ScheduleAsync(CancellationToken cancellationToken);
}
