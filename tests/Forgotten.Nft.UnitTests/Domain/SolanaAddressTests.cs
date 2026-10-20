using Forgotten.Nft.Domain.ValueObjects;

namespace Forgotten.Nft.UnitTests.Domain;

public class SolanaAddressTests
{
    [Theory]
    [InlineData("So11111111111111111111111111111111111111112")] // wrapped SOL mint (44 chars)
    [InlineData("11111111111111111111111111111111")]            // system program (32 chars)
    public void TryParse_AcceptsValidAddresses(string input)
    {
        Assert.True(SolanaAddress.TryParse(input, out var address));
        Assert.Equal(input, address.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]                                   // too short
    [InlineData("0OIl0OIl0OIl0OIl0OIl0OIl0OIl0OIl")]      // non-Base58 chars (0 O I l)
    [InlineData("not a valid address!!")]                 // spaces / punctuation
    [InlineData("So1111111111111111111111111111111111111111299999")] // too long
    public void TryParse_RejectsInvalidAddresses(string? input)
    {
        Assert.False(SolanaAddress.TryParse(input, out _));
    }

    [Fact]
    public void Parse_ThrowsOnInvalid()
    {
        Assert.Throws<FormatException>(() => SolanaAddress.Parse("nope"));
    }
}
