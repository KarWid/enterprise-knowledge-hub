using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Contracts.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class DocumentUploadApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CapabilitiesAndReservationsUseTheSameConfiguration(bool markdownEnabled)
    {
        // Arrange
        // Run both cases against the real API: Markdown enabled and Markdown disabled.
        // An OrganizationOwner has permission to read capabilities and reserve uploads.
        using var storageFactory = new StorageWebhookFactory { MarkdownEnabled = markdownEnabled };
        var membership = new TestMembership("OrganizationOwner");
        using var factory = ConfigureDocumentsApi(storageFactory, membership);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");

        // Act
        // Read the formats advertised to the frontend, then request a Markdown upload.
        // The uppercase extension also checks that format matching ignores letter case.
        var capabilities = await client.GetFromJsonAsync<DocumentUploadCapabilitiesResponse>("/api/documents/upload-capabilities");
        using var response = await client.PostAsJsonAsync("/api/documents/uploads", new CreateDocumentUploadRequest("Notes.MD"));

        // Assert
        // The advertised formats and the reservation endpoint must agree with the same setting.
        Assert.NotNull(capabilities);
        Assert.Equal(markdownEnabled, capabilities.Formats.Any(format => format.Extension == ".md"));
        Assert.Equal(DocumentUploadOptions.DefaultMaxUploadSizeBytes, capabilities.MaxSizeBytes);

        Assert.Equal(markdownEnabled ? HttpStatusCode.Created : HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = storageFactory.Database.CreateContext();
        if (!markdownEnabled)
        {
            // Rejecting a disabled format must leave no pending document in the database.
            Assert.Empty(await db.Documents.ToListAsync());
            return;
        }

        // An accepted upload returns the canonical content type and persists a reservation
        // owned by the authorized Organization, with the same ID returned to the client.
        var session = await response.Content.ReadFromJsonAsync<DocumentUploadSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("text/markdown", session.ContentType);
        var document = await db.Documents.SingleAsync();
        Assert.Equal(membership.OrganizationId, document.OrganizationId);
        Assert.Equal(session.DocumentId, document.Id);
        Assert.EndsWith(".md", document.BlobReference);
    }

    [Theory]
    // No signed-in identity: authentication challenges the request with 401.
    [InlineData(false, "OrganizationOwner", HttpStatusCode.Unauthorized)]
    // A signed-in Employee lacks the role required by the Documents.Read policy: 403.
    [InlineData(true, "Employee", HttpStatusCode.Forbidden)]
    // A signed-in user without an authorized Organization membership is also denied: 403.
    [InlineData(true, null, HttpStatusCode.Forbidden)]
    public async Task CapabilitiesRequireAuthenticationAndDocumentReadAccess(bool signedIn, string? role, HttpStatusCode expected)
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        using var factory = ConfigureDocumentsApi(storageFactory, role is null ? null : new TestMembership(role));
        using var client = factory.CreateClient();
        if (signedIn)
        {
            client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");
        }

        // Act
        using var response = await client.GetAsync("/api/documents/upload-capabilities");

        // Assert
        // Exercise the API authentication/authorization pipeline for each denied-access case.
        Assert.Equal(expected, response.StatusCode);
    }

    // Host the real controllers, application handlers and authorization policies.
    // Supply a controlled identity and Organization membership, and use the fixture's
    // test database and blob storage fake so no Entra sign-in or Azure calls are needed.
    private static WebApplicationFactory<Program> ConfigureDocumentsApi(StorageWebhookFactory factory, TestMembership? membership) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "DocumentUploadTest";
                options.DefaultChallengeScheme = "DocumentUploadTest";
                options.DefaultForbidScheme = "DocumentUploadTest";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("DocumentUploadTest", _ => { });
            
            services.RemoveAll<IOrganizationAccessService>();
            services.AddSingleton<IOrganizationAccessService>(new TestOrganizationAccess(membership));
            services.RemoveAll<IDocumentBlobStorage>();
            services.AddSingleton<IDocumentBlobStorage>(factory.Database.Storage);
        }));

    // Represents the application membership returned after resolving the current Organization.
    private sealed record TestMembership(string Role) : IOrganizationMembership
    {
        public Guid MembershipId { get; } = Guid.NewGuid();
        public Guid OrganizationId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
    }

    // A null membership simulates a user who cannot access the current Organization.
    private sealed class TestOrganizationAccess(TestMembership? membership) : IOrganizationAccessService
    {
        public Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken) =>
            membership is null
                ? throw new OrganizationAccessDeniedException("No authorized Organization context.")
                : Task.FromResult<IOrganizationMembership>(membership);

        public void ClearCache(Guid userId, Guid organizationId) { }
    }

    // The test header controls authentication only; roles are supplied by the membership
    // service above, so the real document authorization policy still decides access.
    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            Request.Headers.ContainsKey("X-Test-Signed-In")
                ? AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")], Scheme.Name)), Scheme.Name))
                : AuthenticateResult.NoResult());
    }
}
