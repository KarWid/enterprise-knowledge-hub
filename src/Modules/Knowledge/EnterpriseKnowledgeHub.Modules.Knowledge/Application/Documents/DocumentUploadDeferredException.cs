namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public sealed class DocumentUploadDeferredException : Exception
{
    internal DocumentUploadDeferredException(string errorCode) : base(errorCode)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

