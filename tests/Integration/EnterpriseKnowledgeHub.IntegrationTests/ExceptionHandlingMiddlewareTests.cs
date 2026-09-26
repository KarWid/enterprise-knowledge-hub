using System.Text.Json;
using EnterpriseKnowledgeHub.Api.Middleware;
using EnterpriseKnowledgeHub.Application.Exceptions;
using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Contracts.Errors;
using EnterpriseKnowledgeHub.Modules.Identity.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnterpriseKnowledgeHub.IntegrationTests;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [InlineData("organization_access_denied", StatusCodes.Status403Forbidden)]
    [InlineData("organizations_domain_error", StatusCodes.Status400BadRequest)]
    [InlineData("identity_domain_error", StatusCodes.Status400BadRequest)]
    [InlineData("user_not_invited", StatusCodes.Status403Forbidden)]
    [InlineData("domain_validation_failed", StatusCodes.Status400BadRequest)]
    [InlineData("invalid_request", StatusCodes.Status400BadRequest)]
    [InlineData("unauthorized", StatusCodes.Status401Unauthorized)]
    [InlineData("concurrency_conflict", StatusCodes.Status409Conflict)]
    [InlineData("internal_server_error", StatusCodes.Status500InternalServerError)]
    public async Task InvokeAsync_MapsExceptionsToExpectedErrorResult(string expectedCode, int expectedStatusCode)
    {
        var exception = CreateException(expectedCode);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ErrorResult>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Equal(expectedCode, error?.Code);
    }

    [Fact]
    public async Task InvokeAsync_LogsUnhandledExceptionWithRequestContext()
    {
        var exception = new Exception("Unexpected.");
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/organizations";
        context.TraceIdentifier = "test-trace-id";
        context.Response.Body = new MemoryStream();
        var logger = new CapturingLogger();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Error, entry.LogLevel);
        Assert.Same(exception, entry.Exception);
        Assert.Contains("POST /organizations", entry.Message);
        Assert.Contains("test-trace-id", entry.Message);
        Assert.Contains("StatusCode: 500", entry.Message);
        Assert.Contains("ErrorCode: internal_server_error", entry.Message);
    }

    private static Exception CreateException(string code) => code switch
    {
        "organization_access_denied" => new OrganizationAccessDeniedException("Denied."),
        "organizations_domain_error" => new OrganizationsDomainException("Invalid organization request."),
        "identity_domain_error" => new IdentityException("Invalid identity request."),
        "user_not_invited" => new UserNotInvitedException("Not invited."),
        "domain_validation_failed" => new DomainException("Domain validation failed."),
        "invalid_request" => new ArgumentException("Invalid request."),
        "unauthorized" => new UnauthorizedAccessException(),
        "concurrency_conflict" => new DbUpdateConcurrencyException(),
        _ => new Exception("Unexpected.")
    };

    private sealed class CapturingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, Exception? Exception, string Message);
}
