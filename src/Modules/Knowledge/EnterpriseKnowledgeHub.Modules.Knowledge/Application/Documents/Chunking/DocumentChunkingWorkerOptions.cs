using EnterpriseKnowledgeHub.BuildingBlocks.Application;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public sealed class DocumentChunkingWorkerOptions : DurableWorkerOptions
{
    public const string SectionName = "DocumentChunkingWorker";
}
