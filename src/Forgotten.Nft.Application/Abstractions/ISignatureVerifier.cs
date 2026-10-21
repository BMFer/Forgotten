namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Verifies Solana Ed25519 signatures server-side over the exact signed message
/// bytes (spec §7.3, §16.4). Never handles private keys.
/// </summary>
public interface ISignatureVerifier
{
    /// <summary>
    /// Returns true iff <paramref name="signature"/> is a valid Ed25519 signature of
    /// UTF-8 <paramref name="message"/> by the key encoded in <paramref name="walletAddress"/>
    /// (Base58). Returns false for malformed inputs rather than throwing.
    /// </summary>
    bool VerifyEd25519(string walletAddress, string message, byte[] signature);
}
