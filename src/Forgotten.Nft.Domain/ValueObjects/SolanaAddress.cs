namespace Forgotten.Nft.Domain.ValueObjects;

/// <summary>
/// A validated Solana address (a Base58-encoded 32-byte Ed25519 public key).
/// Used at service/API boundaries to reject malformed input before any provider
/// call (spec §16.6). Persistence stores the raw string form (spec §9 schema).
/// A single type covers asset / wallet / collection addresses — they share the
/// same on-chain format; the distinction is contextual, not structural.
/// </summary>
public readonly record struct SolanaAddress
{
    public const int PublicKeyLength = 32;

    private SolanaAddress(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? input, out SolanaAddress address)
    {
        address = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        // Base58 of a 32-byte key is 32–44 chars; cheap length gate before decoding.
        if (input.Length is < 32 or > 44)
        {
            return false;
        }

        if (!Base58.TryDecode(input, out var bytes) || bytes.Length != PublicKeyLength)
        {
            return false;
        }

        address = new SolanaAddress(input);
        return true;
    }

    public static SolanaAddress Parse(string input) =>
        TryParse(input, out var address)
            ? address
            : throw new FormatException($"'{input}' is not a valid Solana address.");

    public override string ToString() => Value;
}
