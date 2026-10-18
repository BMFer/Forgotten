// Forgotten.Nft.WalletWeb hosts the HTTPS wallet-verification site (spec §7).
// Phase 0 provides only a health endpoint so the project builds and runs; the
// challenge/sign/verify endpoints and browser wallet integration are built in
// Phase 3 (see docs/phases/PHASE-3-WALLET.md).

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", component = "wallet-web" }));

app.Run();
