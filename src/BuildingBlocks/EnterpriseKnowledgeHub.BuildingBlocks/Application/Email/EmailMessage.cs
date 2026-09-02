namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Email;

public sealed record EmailMessage(
    string Recipient,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
