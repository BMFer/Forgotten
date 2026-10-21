namespace Forgotten.Nft.Application.DTOs;

/// <summary>A newly created verification session: the secure URL to open and when it expires.</summary>
public sealed record WalletConnectSession(string Url, DateTimeOffset ExpiresAt);

/// <summary>The exact message a wallet must sign, plus its expiry.</summary>
public sealed record WalletChallengeResult(string Message, DateTimeOffset ExpiresAt);

/// <summary>Outcome of a signature submission.</summary>
public sealed record WalletVerificationOutcome
{
    public bool Success { get; init; }

    public string? WalletAddress { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static WalletVerificationOutcome Ok(string walletAddress) =>
        new() { Success = true, WalletAddress = walletAddress };

    public static WalletVerificationOutcome Fail(string errorCode, string errorMessage) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
