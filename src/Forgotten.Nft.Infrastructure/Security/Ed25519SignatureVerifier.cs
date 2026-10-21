using System.Text;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.ValueObjects;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace Forgotten.Nft.Infrastructure.Security;

/// <summary>
/// Verifies Solana Ed25519 signatures over the exact UTF-8 message bytes using
/// BouncyCastle (pure-managed, no native dependency) — spec §7.3, §16.4. The
/// wallet address is the Base58-encoded 32-byte public key. Malformed inputs
/// return false rather than throwing (fail closed).
/// </summary>
public sealed class Ed25519SignatureVerifier : ISignatureVerifier
{
    private const int PublicKeyLength = 32;
    private const int SignatureLength = 64;

    public bool VerifyEd25519(string walletAddress, string message, byte[] signature)
    {
        try
        {
            if (signature is null || signature.Length != SignatureLength)
            {
                return false;
            }

            if (!Base58.TryDecode(walletAddress, out var publicKey) || publicKey.Length != PublicKeyLength)
            {
                return false;
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);
            return Ed25519.Verify(signature, 0, publicKey, 0, messageBytes, 0, messageBytes.Length);
        }
        catch
        {
            return false;
        }
    }
}
