using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Api.Authorization;

public sealed class DocumentDiagnosticsAccessService(
    IOptions<DocumentDiagnosticsOptions> options,
    IAuthorizationService authorization,
    IHttpContextAccessor httpContextAccessor) : IDocumentDiagnosticsAccessService
{
    public async ValueTask<bool> CanViewDiagnosticsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!options.Value.Enabled)
        {
            return false;
        }

        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return false;
        }

        var result = await authorization.AuthorizeAsync(user, null, Policies.Documents.Diagnostics);

        cancellationToken.ThrowIfCancellationRequested();
        return result.Succeeded;
    }
}
