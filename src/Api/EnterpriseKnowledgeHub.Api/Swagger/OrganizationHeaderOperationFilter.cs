using EnterpriseKnowledgeHub.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnterpriseKnowledgeHub.Api.Swagger;

public sealed class OrganizationHeaderOperationFilter : IOperationFilter
{
    private const string OrganizationHeaderName = "X-Organization-Id";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresOrganizationContext = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any(authorize => authorize.Policy is Policies.Documents.Read or Policies.Documents.Upload);

        if (!requiresOrganizationContext)
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = OrganizationHeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Organization selected for this request. Access is validated by the API.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid"
            }
        });
    }
}
