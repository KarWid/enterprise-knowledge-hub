using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Organizations;

public sealed record InviteUserRequest(
    [Required(ErrorMessage = "Email is required.")] 
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    string Email);
