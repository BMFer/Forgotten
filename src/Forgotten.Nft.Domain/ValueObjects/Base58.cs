namespace Forgotten.Nft.Domain.ValueObjects;

/// <summary>
/// Minimal Base58 decoder (Bitcoin/Solana alphabet) used to validate that a
/// string is a well-formed Solana address (a 32-byte Ed25519 public key).
/// Dependency-free so the Domain layer carries no external packages.
/// </summary>
internal static class Base58
{
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    private static readonly int[] Map = BuildMap();

    private static int[] BuildMap()
    {
        var map = new int[128];
        Array.Fill(map, -1);
        for (var i = 0; i < Alphabet.Length; i++)
        {
            map[Alphabet[i]] = i;
        }

        return map;
    }

    /// <summary>
    /// Attempts to decode a Base58 string into its raw bytes. Returns false for
    /// null/empty input or any character outside the Base58 alphabet.
    /// </summary>
    public static bool TryDecode(string? input, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        // Count leading '1's — each maps to a leading zero byte.
        var leadingZeros = 0;
        while (leadingZeros < input.Length && input[leadingZeros] == '1')
        {
            leadingZeros++;
        }

        var result = new byte[input.Length];
        var resultLength = 0;

        foreach (var c in input)
        {
            if (c >= 128 || Map[c] == -1)
            {
                return false;
            }

            var carry = Map[c];
            for (var j = 0; j < resultLength; j++)
            {
                carry += result[j] * 58;
                result[j] = (byte)(carry & 0xFF);
                carry >>= 8;
            }

            while (carry > 0)
            {
                result[resultLength++] = (byte)(carry & 0xFF);
                carry >>= 8;
            }
        }

        var decoded = new byte[leadingZeros + resultLength];
        // result holds little-endian significant bytes; reverse into place after the leading zeros.
        for (var i = 0; i < resultLength; i++)
        {
            decoded[leadingZeros + i] = result[resultLength - 1 - i];
        }

        bytes = decoded;
        return true;
    }
}
