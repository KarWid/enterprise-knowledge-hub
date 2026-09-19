using EnterpriseKnowledgeHub.Application.Exceptions;
using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Contracts.Errors;
using EnterpriseKnowledgeHub.Modules.Identity.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, error) = HandleException(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception while processing {RequestPath}", context.Request.Path);
            }
            else
            {
                logger.LogWarning(exception, "Request to {RequestPath} failed with {ErrorCode}", context.Request.Path, error.Code);
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(error, cancellationToken: context.RequestAborted);
        }
    }

    private static (int StatusCode, ErrorResult Error) HandleException(Exception exception) => exception switch
    {
        OrganizationAccessDeniedException accessDeniedException => HandleOrganizationsException(accessDeniedException),
        OrganizationsDomainException organizationsException => HandleOrganizationsException(organizationsException),
        IdentityException identityException => HandleIdentityException(identityException),
        UserNotInvitedException applicationException => HandleApplicationException(applicationException),
        DomainException domainException => BadRequest("domain_validation_failed", domainException.Message),
        ArgumentException argumentException => BadRequest("invalid_request", argumentException.Message),
        UnauthorizedAccessException => Unauthorized(),
        DbUpdateConcurrencyException => Conflict(),
        _ => InternalServerError()
    };

    private static (int StatusCode, ErrorResult Error) HandleOrganizationsException(
        OrganizationsDomainException exception) => exception switch
    {
        OrganizationAccessDeniedException =>
            (StatusCodes.Status403Forbidden, new ErrorResult("organization_access_denied", "Access to this organization is denied.")),
        _ => BadRequest("organizations_domain_error", exception.Message)
    };

    private static (int StatusCode, ErrorResult Error) HandleIdentityException(
        IdentityException exception) => BadRequest("identity_domain_error", exception.Message);

    private static (int StatusCode, ErrorResult Error) HandleApplicationException(
        UserNotInvitedException _) =>
        (StatusCodes.Status403Forbidden, new ErrorResult("user_not_invited", "This account is not authorized to access the application."));

    private static (int StatusCode, ErrorResult Error) BadRequest(string code, string message) =>
        (StatusCodes.Status400BadRequest, new ErrorResult(code, message));

    private static (int StatusCode, ErrorResult Error) Unauthorized() =>
        (StatusCodes.Status401Unauthorized, new ErrorResult("unauthorized", "Authentication is required."));

    private static (int StatusCode, ErrorResult Error) Conflict() =>
        (StatusCodes.Status409Conflict, new ErrorResult("concurrency_conflict", "The resource was changed by another request. Please retry."));

    private static (int StatusCode, ErrorResult Error) InternalServerError() =>
        (StatusCodes.Status500InternalServerError, new ErrorResult("internal_server_error", "An unexpected error occurred."));
}
