using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Domain;

public class OrganizationInvitationTests
{
    private static OrganizationInvitation CreateInvitation(DateTime? expiresAt = null) =>
        OrganizationInvitation.Create(
            Guid.NewGuid(),
            " User@Example.com ",
            "token-hash",
            Guid.NewGuid(),
            expiresAt ?? DateTime.UtcNow.AddDays(7));

    [Fact]
    public void Create_NormalizesEmailAndSetsPendingStatus()
    {
        var invitation = CreateInvitation();

        Assert.Equal("user@example.com", invitation.Email);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.AcceptedAt);
    }

    [Fact]
    public void Create_WhenExpiresAtIsInThePast_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => OrganizationInvitation.Create(
            Guid.NewGuid(), "user@example.com", "token-hash", Guid.NewGuid(), DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void Accept_WhenPendingAndNotExpired_SetsAcceptedStatusAndTimestamp()
    {
        var invitation = CreateInvitation();

        invitation.Accept();

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(invitation.AcceptedAt);
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_ThrowsInvalidOperationException()
    {
        var invitation = CreateInvitation();
        invitation.Accept();

        Assert.Throws<InvalidOperationException>(() => invitation.Accept());
    }

    [Fact]
    public async Task Accept_WhenExpired_MarksExpiredAndThrows()
    {
        // Arrange
        // One second gives Create enough time to validate a future expiration date.
        // Waiting slightly longer ensures the invitation has expired before exercising Accept.
        var invitation = CreateInvitation(DateTime.UtcNow.AddSeconds(1));
        await Task.Delay(TimeSpan.FromMilliseconds(1100));

        // Act
        void AcceptExpiredInvitation() => invitation.Accept();

        // Assert
        Assert.Throws<InvalidOperationException>(AcceptExpiredInvitation);
        Assert.Equal(InvitationStatus.Expired, invitation.Status);
    }

    [Fact]
    public void Revoke_WhenPending_SetsRevokedStatus()
    {
        var invitation = CreateInvitation();

        invitation.Revoke();

        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
    }

    [Fact]
    public void Revoke_WhenNotPending_ThrowsInvalidOperationException()
    {
        var invitation = CreateInvitation();
        invitation.Revoke();

        Assert.Throws<InvalidOperationException>(() => invitation.Revoke());
    }

    [Fact]
    public async Task IsExpired_WhenExpiresAtInPastAndStillPending_ReturnsTrue()
    {
        // Arrange
        // Avoid the original one-millisecond race during creation, then allow the
        // invitation to expire naturally before checking its state.
        var invitation = CreateInvitation(DateTime.UtcNow.AddSeconds(1));
        await Task.Delay(TimeSpan.FromMilliseconds(1100));

        // Act
        var isExpired = invitation.IsExpired();

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsExpired_WhenNotYetExpired_ReturnsFalse()
    {
        // Arrange
        var invitation = CreateInvitation();

        // Act
        var isExpired = invitation.IsExpired();

        // Assert
        Assert.False(isExpired);
    }
}
