// Forgotten.Nft.WalletWeb — HTTPS wallet-verification site (spec §7).
// Flow: /wallet connect (bot) issues a one-time URL -> the member opens it here,
// connects a Solana wallet, signs the server-built challenge, and the signature is
// verified server-side (Ed25519). Private keys are never requested or transmitted.
//
// This app shares the SAME SQLite database as the bot (Database:ConnectionString
// must point at the same file). A public HTTPS origin is required in production
// (spec §7.3); localhost may use HTTP for development.

using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DependencyInjection;
using Forgotten.Nft.Infrastructure.DependencyInjection;
using Forgotten.Nft.WalletWeb;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddForgottenApplication(builder.Configuration);
builder.Services.AddForgottenInfrastructure(builder.Configuration);

var app = builder.Build();

// Serves the bundled client at /js/verify.js (built from ClientApp into wwwroot).
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", component = "wallet-web" }));

// The signing page. The token is a one-time capability embedded in the URL.
app.MapGet("/verify/{token}", (string token, IOptions<WalletVerificationOptions> options) =>
    Results.Content(VerificationPage.Html(token, options.Value.TipLinkClientId), "text/html; charset=utf-8"));

// Step 1: bind the connected wallet and return the exact message to sign.
app.MapPost("/api/verify/{token}/challenge", async (
    string token, ChallengeRequest request, IWalletVerificationService service, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.WalletAddress))
    {
        return Results.BadRequest(new { error = "wallet_address_required" });
    }

    var challenge = await service.IssueChallengeAsync(token, request.WalletAddress, ct);
    return challenge is null
        ? Results.BadRequest(new { error = "invalid_or_expired_session" })
        : Results.Ok(new { message = challenge.Message, expiresAt = challenge.ExpiresAt });
});

// Step 2: submit the signature for server-side verification.
app.MapPost("/api/verify/{token}", async (
    string token, SubmitRequest request, IWalletVerificationService service, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.WalletAddress) || string.IsNullOrWhiteSpace(request.Signature))
    {
        return Results.BadRequest(new { success = false, error = "missing_fields" });
    }

    byte[] signature;
    try
    {
        signature = Convert.FromBase64String(request.Signature);
    }
    catch (FormatException)
    {
        return Results.BadRequest(new { success = false, error = "bad_signature_encoding" });
    }

    var outcome = await service.VerifyAsync(token, request.WalletAddress, signature, ct);
    return outcome.Success
        ? Results.Ok(new { success = true, wallet = outcome.WalletAddress })
        : Results.BadRequest(new { success = false, error = outcome.ErrorCode, message = outcome.ErrorMessage });
});

app.Run();

internal sealed record ChallengeRequest(string WalletAddress);

internal sealed record SubmitRequest(string WalletAddress, string Signature);
