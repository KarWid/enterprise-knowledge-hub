using EnterpriseKnowledgeHub.BuildingBlocks.Application;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public sealed class DocumentUploadWorkerOptions : DurableWorkerOptions
{
    public const string SectionName = "DocumentUploadWorker";
    public int ReservationGraceSeconds { get; init; } = 300;

    public override bool IsValid() =>
        base.IsValid() &&
        ReservationGraceSeconds is >= 60 and <= 86400;
}
