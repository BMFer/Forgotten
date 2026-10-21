using Forgotten.Nft.Infrastructure.Security;

namespace Forgotten.Nft.SecurityTests.Wallet;

public class Ed25519SignatureVerifierTests
{
    private readonly Ed25519SignatureVerifier _verifier = new();

    [Fact]
    public void ValidSignature_Verifies()
    {
        var kp = TestKeypair.Generate();
        const string message = "FORGOTTEN Wallet Verification\n\nNonce: abc";

        var signature = kp.Sign(message);

        Assert.True(_verifier.VerifyEd25519(kp.Address, message, signature));
    }

    [Fact]
    public void WrongMessage_Fails()
    {
        var kp = TestKeypair.Generate();
        var signature = kp.Sign("original message");

        Assert.False(_verifier.VerifyEd25519(kp.Address, "tampered message", signature));
    }

    [Fact]
    public void SignatureFromDifferentKey_Fails()
    {
        var signer = TestKeypair.Generate();
        var other = TestKeypair.Generate();
        const string message = "hello";

        var signature = signer.Sign(message);

        // Correct message + signature, but claiming a different wallet's address.
        Assert.False(_verifier.VerifyEd25519(other.Address, message, signature));
    }

    [Theory]
    [InlineData("not-base58-!!!")]
    [InlineData("")]
    public void MalformedAddress_ReturnsFalse(string address)
    {
        var kp = TestKeypair.Generate();
        Assert.False(_verifier.VerifyEd25519(address, "m", kp.Sign("m")));
    }

    [Fact]
    public void WrongLengthSignature_ReturnsFalse()
    {
        var kp = TestKeypair.Generate();
        Assert.False(_verifier.VerifyEd25519(kp.Address, "m", new byte[10]));
    }
}
