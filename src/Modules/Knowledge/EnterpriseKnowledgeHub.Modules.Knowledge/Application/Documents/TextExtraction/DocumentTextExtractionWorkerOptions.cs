using EnterpriseKnowledgeHub.BuildingBlocks.Application;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public sealed class DocumentTextExtractionWorkerOptions : DurableWorkerOptions
{
    public const string SectionName = "DocumentTextExtractionWorker";
}
