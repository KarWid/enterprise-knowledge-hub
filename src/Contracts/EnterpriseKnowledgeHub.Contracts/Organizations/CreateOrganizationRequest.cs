using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Organizations;

public sealed record CreateOrganizationRequest(
    [Required(ErrorMessage = "Organization name is required.")] string Name);
