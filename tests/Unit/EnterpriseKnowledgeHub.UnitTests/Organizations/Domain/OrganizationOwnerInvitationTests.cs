using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Domain;

public class OrganizationOwnerInvitationTests
{
    [Fact]
    public void Create_SetsPendingStatusAndNoAcceptedAt()
    {
        var invitation = OrganizationOwnerInvitation.Create("owner@example.com", "token-hash", DateTime.UtcNow.AddDays(7));

        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.AcceptedAt);
        Assert.Equal("owner@example.com", invitation.Email);
    }

    [Fact]
    public void Accept_SetsAcceptedStatusAndTimestamp()
    {
        var invitation = OrganizationOwnerInvitation.Create("owner@example.com", "token-hash", DateTime.UtcNow.AddDays(7));

        invitation.Accept();

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(invitation.AcceptedAt);
    }

    [Fact]
    public void Revoke_SetsRevokedStatus()
    {
        var invitation = OrganizationOwnerInvitation.Create("owner@example.com", "token-hash", DateTime.UtcNow.AddDays(7));

        invitation.Revoke();

        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
    }
}
