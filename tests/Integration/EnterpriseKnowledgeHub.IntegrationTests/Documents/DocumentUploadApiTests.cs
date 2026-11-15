using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Contracts.Knowledge;
using EnterpriseKnowledgeHub.Contracts.Errors;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
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
    [InlineData(true, "OrganizationOwner")]
    [InlineData(false, "OrganizationOwner")]
    [InlineData(true, "OrganizationAdmin")]
    [InlineData(false, "OrganizationAdmin")]
    [InlineData(true, "KnowledgeManager")]
    [InlineData(false, "KnowledgeManager")]
    public async Task DocumentDiagnostics_RequiresEnabledFlagAndOwnerMembershipInCurrentOrganization(bool diagnosticsEnabled, string role)
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory { DiagnosticsEnabled = diagnosticsEnabled };
        var membership = new TestMembership(role);
        await using var db = storageFactory.Database.CreateContext();
        var document = await DocumentChunkingTests.SeedExtractionAsync(
            db, "# Private document\n\nDiagnostic content.", organizationId: membership.OrganizationId);
        await DocumentChunkingTests.CompleteAsync(storageFactory.Database, db);
        using var factory = ConfigureDocumentsApi(storageFactory, membership);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");
        client.DefaultRequestHeaders.Add("X-Organization-Id", membership.OrganizationId.ToString());
        // Simulate an Owner role claim for a different Organization. Only the
        // validated current Organization membership may grant diagnostic access.
        client.DefaultRequestHeaders.Add("X-Test-Owner-Organization", Guid.NewGuid().ToString());

        // Act
        var details = await client.GetFromJsonAsync<DocumentResponse>($"/api/documents/{document.Id}");
        var documents = await client.GetFromJsonAsync<List<DocumentResponse>>("/api/documents");
        using var textPreview = await client.GetAsync($"/api/documents/{document.Id}/text-preview");
        using var chunks = await client.GetAsync($"/api/documents/{document.Id}/chunks");

        // Assert
        var listedDocument = Assert.Single(documents!);
        Assert.NotNull(details);
        Assert.Equal("Completed", details.TextExtraction!.Status);
        Assert.Equal("Completed", listedDocument.TextExtraction!.Status);
        if (diagnosticsEnabled && role == "OrganizationOwner")
        {
            Assert.Equal("Completed", details.Chunking!.Status);
            Assert.Equal("Completed", listedDocument.Chunking!.Status);
            Assert.Equal(HttpStatusCode.OK, textPreview.StatusCode);
            Assert.Equal(HttpStatusCode.OK, chunks.StatusCode);
        }
        else
        {
            Assert.Null(details.Chunking);
            Assert.Null(listedDocument.Chunking);
            Assert.Equal(HttpStatusCode.NotFound, textPreview.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, chunks.StatusCode);
            Assert.DoesNotContain("Diagnostic content", await textPreview.Content.ReadAsStringAsync());
            Assert.DoesNotContain("Diagnostic content", await chunks.Content.ReadAsStringAsync());
        }

        // Disabling diagnostics does not disable processing or remove its results.
        Assert.Single(await db.DocumentChunks.ToListAsync());
    }

    [Fact]
    public async Task CompletedChunking_ReturnsDevelopmentStatusesAndPagedPreviews()
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        var membership = new TestMembership("OrganizationOwner");
        await using var db = storageFactory.Database.CreateContext();
        var document = await DocumentChunkingTests.SeedExtractionAsync(db,
            "# First\n\nPrivate first passage.\n\n# Second\n\nPrivate second passage.", organizationId: membership.OrganizationId);
        await DocumentChunkingTests.CompleteAsync(storageFactory.Database, db);
        using var factory = ConfigureDocumentsApi(storageFactory, membership);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");

        // Act
        var status = await client.GetFromJsonAsync<DocumentResponse>($"/api/documents/{document.Id}");
        var chunks = await client.GetFromJsonAsync<DocumentChunksResponse>($"/api/documents/{document.Id}/chunks?offset=1&limit=1");
        var listJson = await client.GetStringAsync("/api/documents");

        // Assert
        Assert.NotNull(status);
        Assert.Equal("Completed", status.Chunking!.Status);
        Assert.Equal(2, status.Chunking.ChunkCount);
        Assert.Equal(1, Assert.Single(chunks!.Items).Ordinal);
        Assert.False(chunks.HasMore);
        Assert.Contains("Private second passage.", chunks.Items[0].Content);
        Assert.DoesNotContain("Private first passage.", listJson);
        Assert.DoesNotContain("configurationKey", listJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChunkPreview_BoundsTextInSqlAndDoesNotSplitUnicode()
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        var membership = new TestMembership("OrganizationOwner");
        await using var db = storageFactory.Database.CreateContext();
        const string prefix = "Document: notes.md\n\n";
        var text = new string('a', 4000 - prefix.Length - 1) + "🙂" + new string('a', 1000);
        var document = await DocumentChunkingTests.SeedExtractionAsync(db, text, organizationId: membership.OrganizationId);
        await DocumentChunkingTests.CompleteAsync(storageFactory.Database, db);
        using var factory = ConfigureDocumentsApi(storageFactory, membership);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");

        // Act
        var preview = await client.GetFromJsonAsync<DocumentChunksResponse>($"/api/documents/{document.Id}/chunks?limit=1");

        // Assert
        var chunk = Assert.Single(preview!.Items);
        Assert.True(chunk.IsTruncated);
        Assert.Equal(3999, chunk.Content.Length);
        Assert.False(char.IsSurrogate(chunk.Content[^1]));
    }

    [Theory]
    [InlineData(false, "OrganizationOwner", HttpStatusCode.Unauthorized, true)]
    [InlineData(false, "OrganizationOwner", HttpStatusCode.Unauthorized, false)]
    [InlineData(true, "Employee", HttpStatusCode.Forbidden, true)]
    [InlineData(true, "Employee", HttpStatusCode.Forbidden, false)]
    [InlineData(true, null, HttpStatusCode.Forbidden, true)]
    [InlineData(true, null, HttpStatusCode.Forbidden, false)]
    public async Task ChunkPreview_RequiresAuthenticationAndDocumentsRead(bool signedIn, string? role, HttpStatusCode expected, bool diagnosticsEnabled)
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory { DiagnosticsEnabled = diagnosticsEnabled };
        using var factory = ConfigureDocumentsApi(storageFactory, role is null ? null : new TestMembership(role));
        using var client = factory.CreateClient();
        if (signedIn)
        {
            client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");
        }

        // Act
        using var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/chunks");

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(-1, 1, "offset", "Offset must be zero or greater.")]
    [InlineData(0, 0, "limit", "Limit must be between 1 and 20.")]
    [InlineData(0, -1, "limit", "Limit must be between 1 and 20.")]
    [InlineData(0, 21, "limit", "Limit must be between 1 and 20.")]
    public async Task ChunkPreview_RejectsUnboundedPaginationWithHelpfulErrors(int offset, int limit, string field, string message)
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        using var factory = ConfigureDocumentsApi(storageFactory, new TestMembership("OrganizationOwner"));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");

        // Act
        using var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/chunks?offset={offset}&limit={limit}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResult>();
        Assert.NotNull(error);
        Assert.Equal("invalid_request", error.Code);
        Assert.Contains(field, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(message, error.Message);
    }

    [Fact]
    public async Task ChunkPreview_ValidPaginationForUnavailableDocumentReturnsNotFound()
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        using var factory = ConfigureDocumentsApi(storageFactory, new TestMembership("OrganizationOwner"));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");

        // Act
        using var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/chunks?offset=0&limit=20");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompletedExtraction_ReturnsMetadataAndBoundedTextPreview()
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        var membership = new TestMembership("OrganizationOwner");

        await using var db = storageFactory.Database.CreateContext();
        var document = await DocumentTextExtractionTests.ReserveAsync(db, organizationId: membership.OrganizationId);
        document.CompleteUpload("\"0x123\"");
        storageFactory.Database.ExtractionScheduler(db).Stage(document);
        await db.SaveChangesAsync();

        var work = await storageFactory.Database
            .ExtractionQueue(db)
            .ClaimAsync(CancellationToken.None);
        var text = "<script>untrusted source</script>" + new string('a', 5000);
        
        await storageFactory.Database.ExtractionQueue(db).CompleteAsync(work!, text, CancellationToken.None);
        using var factory = ConfigureDocumentsApi(storageFactory, membership);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");

        // Act
        var status = await client.GetFromJsonAsync<DocumentResponse>($"/api/documents/{document.Id}");
        var preview = await client.GetFromJsonAsync<DocumentTextPreviewResponse>($"/api/documents/{document.Id}/text-preview");
        var listJson = await client.GetStringAsync("/api/documents");

        // Assert
        Assert.Equal("Uploaded", status!.Status);
        Assert.Equal("Completed", status.TextExtraction!.Status);
        Assert.Equal(text.Length, status.TextExtraction.CharacterCount);
        Assert.Equal(text[..4000], preview!.Text);
        Assert.True(preview.IsTruncated);
        Assert.DoesNotContain("untrusted source", listJson);
        Assert.DoesNotContain("validatedETag", listJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, "OrganizationOwner", HttpStatusCode.Unauthorized, true)]
    [InlineData(false, "OrganizationOwner", HttpStatusCode.Unauthorized, false)]
    [InlineData(true, "Employee", HttpStatusCode.Forbidden, true)]
    [InlineData(true, "Employee", HttpStatusCode.Forbidden, false)]
    [InlineData(true, null, HttpStatusCode.Forbidden, true)]
    [InlineData(true, null, HttpStatusCode.Forbidden, false)]
    public async Task TextPreview_RequiresAuthenticationAndDocumentsRead(bool signedIn, string? role, HttpStatusCode expected, bool diagnosticsEnabled)
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory { DiagnosticsEnabled = diagnosticsEnabled };
        using var factory = ConfigureDocumentsApi(storageFactory, role is null ? null : new TestMembership(role));
        using var client = factory.CreateClient();
        if (signedIn)
        {
            client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");
        }

        // Act
        using var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/text-preview");

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/text-preview")]
    [InlineData("/chunks")]
    public async Task ExtractionEndpoints_HideOtherOrganizationsDocuments(string suffix)
    {
        // Arrange
        using var storageFactory = new StorageWebhookFactory();
        await using var db = storageFactory.Database.CreateContext();
        var requestingMembership = new TestMembership("OrganizationOwner");
        var documentOwnerOrganizationId = Guid.NewGuid();

        var otherOrganizationDocument = await DocumentTextExtractionTests.ReserveAsync(
            db, organizationId: documentOwnerOrganizationId);
        otherOrganizationDocument.CompleteUpload("\"0x123\"");
        storageFactory.Database.ExtractionScheduler(db).Stage(otherOrganizationDocument);
        await db.SaveChangesAsync();

        var work = await storageFactory.Database.ExtractionQueue(db).ClaimAsync(CancellationToken.None);
        await storageFactory.Database.ExtractionQueue(db).CompleteAsync(work!, "private content", CancellationToken.None);
        await DocumentChunkingTests.CompleteAsync(storageFactory.Database, db);
        Assert.Single(await db.DocumentChunks.ToListAsync());

        using var factory = ConfigureDocumentsApi(storageFactory, requestingMembership);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Signed-In", "true");
        // Deliberately request the document owner's Organization. The fake access service
        // ignores headers and returns requestingMembership as the established context.
        // This tests handler isolation, not the real header-to-membership validation.
        client.DefaultRequestHeaders.Add("X-Organization-Id", documentOwnerOrganizationId.ToString());

        // Act
        using var response = await client.GetAsync($"/api/documents/{otherOrganizationDocument.Id}{suffix}");

        // Assert
        Assert.Equal(documentOwnerOrganizationId, otherOrganizationDocument.OrganizationId);
        Assert.NotEqual(requestingMembership.OrganizationId, otherOrganizationDocument.OrganizationId);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("private content", await response.Content.ReadAsStringAsync());
    }

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
    private static WebApplicationFactory<Program> ConfigureDocumentsApi(
        StorageWebhookFactory factory,
        TestMembership? membership) =>
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

    // The identity may claim ownership elsewhere. The real policies must authorize
    // using the validated membership supplied by the Organization access service.
    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Signed-In"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
            if (Request.Headers.TryGetValue("X-Test-Owner-Organization", out var organizationId))
            {
                claims.Add(new(ClaimTypes.Role, "OrganizationOwner"));
                claims.Add(new("organizationId", organizationId.ToString()));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
