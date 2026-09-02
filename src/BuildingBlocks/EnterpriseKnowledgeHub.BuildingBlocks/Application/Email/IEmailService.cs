namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Email;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject,  string body, CancellationToken cancellationToken);
}
