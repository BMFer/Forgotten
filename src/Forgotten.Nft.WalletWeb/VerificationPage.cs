using System.Text.Json;

namespace Forgotten.Nft.WalletWeb;

/// <summary>
/// Wallet-verification page. Renders markup + a small JSON config, then loads the
/// bundled client (<c>/js/verify.js</c>, built from ClientApp) which handles both
/// injected browser wallets (Phantom/Solflare) and TipLink. Private keys never leave
/// the wallet. The "Connect with TipLink" button appears only when a TipLink client
/// id is configured.
/// </summary>
internal static class VerificationPage
{
    public static string Html(string token, string? tipLinkClientId)
    {
        var config = JsonSerializer.Serialize(new { token, tipLinkClientId = tipLinkClientId ?? string.Empty });
        var tipLinkButton = string.IsNullOrWhiteSpace(tipLinkClientId)
            ? string.Empty
            : """<button id="tiplink" class="secondary">Connect with TipLink (Google)</button>""";

        return Template
            .Replace("__CONFIG__", config)
            .Replace("__TIPLINK_BUTTON__", tipLinkButton);
    }

    private const string Template = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta name="robots" content="noindex" />
  <link rel="icon" type="image/x-icon" href="/favicon.ico" />
  <title>FORGOTTEN — Wallet Verification</title>
  <style>
    :root { color-scheme: dark; }
    body { margin:0; min-height:100vh; display:flex; align-items:center; justify-content:center;
           font-family:system-ui,Segoe UI,Roboto,sans-serif; background:#0b0d12; color:#e6e8ec; }
    .card { width:min(92vw,460px); background:#141821; border:1px solid #232a37; border-radius:16px;
            padding:28px; box-shadow:0 10px 40px rgba(0,0,0,.4); }
    h1 { font-size:20px; margin:0 0 4px; }
    p.sub { margin:0 0 20px; color:#9aa4b2; font-size:14px; }
    button { width:100%; padding:14px; font-size:15px; font-weight:600; border:0; border-radius:10px;
             cursor:pointer; margin-bottom:10px; background:#5865f2; color:#fff; }
    button.secondary { background:#202634; color:#e6e8ec; border:1px solid #2d3647; }
    button:disabled { opacity:.5; cursor:default; }
    .wallet { font-family:ui-monospace,monospace; font-size:13px; color:#9aa4b2; margin:14px 0; word-break:break-all; }
    .status { margin-top:16px; padding:12px; border-radius:10px; font-size:14px; background:#0f1420; border:1px solid #232a37; min-height:20px; }
    .ok { color:#3fb950; } .err { color:#f85149; }
    .hidden { display:none; }
  </style>
</head>
<body>
  <div class="card">
    <h1>🎪 FORGOTTEN Wallet Verification</h1>
    <p class="sub">Connect a Solana wallet and sign the verification message to prove you control it — no transaction, no funds moved, no keys shared.</p>
    <button id="injected">Connect browser wallet (Phantom / Solflare)</button>
    <div id="mobile-wallets" class="hidden">
      <button id="open-phantom">📱 Open in Phantom</button>
      <button id="open-solflare" class="secondary">📱 Open in Solflare</button>
    </div>
    <button id="sign" class="hidden">✍️ Sign to verify</button>
    __TIPLINK_BUTTON__
    <div class="wallet" id="wallet"></div>
    <div class="status" id="status"></div>
  </div>
  <script type="application/json" id="forgotten-config">__CONFIG__</script>
  <script src="/js/verify.js"></script>
</body>
</html>
""";
}
