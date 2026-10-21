namespace Forgotten.Nft.WalletWeb;

/// <summary>
/// Self-contained wallet-verification page (no external scripts/styles, so it works
/// under a strict CSP). Uses the injected Solana wallet provider (Phantom/Solflare
/// and compatible), requests a server-built challenge, signs it, and submits the
/// signature for server-side Ed25519 verification. Private keys never leave the wallet.
/// </summary>
internal static class VerificationPage
{
    public static string Html(string token)
    {
        // token is URL-safe Base64 (no quotes/backslashes); embed directly.
        return Template.Replace("__TOKEN__", token);
    }

    private const string Template = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta name="robots" content="noindex" />
  <title>FORGOTTEN — Wallet Verification</title>
  <style>
    :root { color-scheme: dark; }
    body { margin:0; min-height:100vh; display:flex; align-items:center; justify-content:center;
           font-family:system-ui,Segoe UI,Roboto,sans-serif; background:#0b0d12; color:#e6e8ec; }
    .card { width:min(92vw,440px); background:#141821; border:1px solid #232a37; border-radius:16px;
            padding:28px; box-shadow:0 10px 40px rgba(0,0,0,.4); }
    h1 { font-size:20px; margin:0 0 4px; }
    p.sub { margin:0 0 20px; color:#9aa4b2; font-size:14px; }
    button { width:100%; padding:14px; font-size:15px; font-weight:600; border:0; border-radius:10px;
             background:#5865f2; color:#fff; cursor:pointer; }
    button:disabled { opacity:.5; cursor:default; }
    .wallet { font-family:ui-monospace,monospace; font-size:13px; color:#9aa4b2; margin:14px 0; word-break:break-all; }
    .status { margin-top:18px; padding:12px; border-radius:10px; font-size:14px; background:#0f1420; border:1px solid #232a37; min-height:20px; }
    .ok { color:#3fb950; } .err { color:#f85149; }
  </style>
</head>
<body>
  <div class="card">
    <h1>🎪 FORGOTTEN Wallet Verification</h1>
    <p class="sub">Connect your Solana wallet and sign the verification message. This proves you control the wallet — no transaction, no funds moved, no keys shared.</p>
    <button id="go">Connect wallet &amp; sign</button>
    <div class="wallet" id="wallet"></div>
    <div class="status" id="status"></div>
  </div>
  <script>
    const token = "__TOKEN__";
    const el = (id) => document.getElementById(id);
    const setStatus = (msg, cls) => { const s = el("status"); s.textContent = msg; s.className = "status " + (cls||""); };

    async function run() {
      const btn = el("go");
      btn.disabled = true;
      try {
        const provider = window.solana || (window.phantom && window.phantom.solana);
        if (!provider || typeof provider.signMessage !== "function") {
          setStatus("No compatible Solana wallet detected. Install Phantom (or Solflare) and reload.", "err");
          btn.disabled = false;
          return;
        }

        setStatus("Connecting wallet…");
        const resp = await provider.connect();
        const wallet = resp.publicKey.toString();
        el("wallet").textContent = wallet;

        setStatus("Requesting challenge…");
        const chRes = await fetch(`/api/verify/${token}/challenge`, {
          method: "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ walletAddress: wallet })
        });
        if (!chRes.ok) { setStatus("This verification link is invalid or has expired. Run /wallet connect again.", "err"); btn.disabled = false; return; }
        const { message } = await chRes.json();

        setStatus("Please sign the message in your wallet…");
        const encoded = new TextEncoder().encode(message);
        const signed = await provider.signMessage(encoded, "utf8");
        const sigBytes = signed.signature || signed;
        let binary = ""; for (let i = 0; i < sigBytes.length; i++) binary += String.fromCharCode(sigBytes[i]);
        const signatureB64 = btoa(binary);

        setStatus("Verifying…");
        const vRes = await fetch(`/api/verify/${token}`, {
          method: "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ walletAddress: wallet, signature: signatureB64 })
        });
        const vJson = await vRes.json().catch(() => ({}));
        if (vRes.ok && vJson.success) {
          setStatus("✅ Wallet verified! You can close this tab and return to Discord.", "ok");
        } else {
          setStatus("❌ " + (vJson.message || vJson.error || "Verification failed."), "err");
          btn.disabled = false;
        }
      } catch (e) {
        setStatus("❌ " + (e && e.message ? e.message : "Something went wrong."), "err");
        btn.disabled = false;
      }
    }

    el("go").addEventListener("click", run);
  </script>
</body>
</html>
""";
}
