using System;

namespace Healthcare_API.Models;

public class RefreshToken
{
    public Guid RefreshTokenId { get; private set; } = Guid.NewGuid();
    public Guid ApplicationUserId { get; private set; }
    public string TokenHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsReplaced => ReplacedByTokenHash is not null;
    public bool IsActive => !IsRevoked && !IsReplaced && !IsExpired;

    public RefreshToken(Guid applicationUserId, string tokenHash, DateTime expiresAt)
    {
        if (applicationUserId == Guid.Empty)
            throw new ArgumentException("Application user id must not be empty.", nameof(applicationUserId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash must not be empty.", nameof(tokenHash));

        RefreshTokenId = Guid.NewGuid();
        ApplicationUserId = applicationUserId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
    }

    public void MarkReplaced(string replacementTokenHash)
    {
        if (string.IsNullOrWhiteSpace(replacementTokenHash))
            throw new ArgumentException("Replacement token hash must not be empty.", nameof(replacementTokenHash));

        ReplacedByTokenHash = replacementTokenHash;
        Revoke();
    }
}
