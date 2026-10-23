namespace Forgotten.Nft.WalletWeb;

/// <summary>
/// Simple branded landing page served at "/". Verification happens only via the
/// one-time links the Discord bot issues (/verify/{token}), so this page just
/// explains what the site is and points people to Discord.
/// </summary>
internal static class IndexPage
{
    public const string Html = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>FORGOTTEN — Wallet Verification</title>
  <style>
    :root { color-scheme: dark; }
    body { margin:0; min-height:100vh; display:flex; align-items:center; justify-content:center;
           font-family:system-ui,Segoe UI,Roboto,sans-serif; background:#0b0d12; color:#e6e8ec; }
    .card { width:min(94vw,560px); background:#141821; border:1px solid #232a37; border-radius:16px;
            padding:24px; box-shadow:0 10px 40px rgba(0,0,0,.4); text-align:center; }
    .banner { width:100%; height:auto; display:block; border-radius:12px; margin-bottom:18px;
              border:1px solid #232a37; }
    h1 { font-size:26px; margin:0 0 6px; letter-spacing:.5px; }
    p { color:#9aa4b2; font-size:15px; line-height:1.6; margin:0 0 16px; }
    .steps { text-align:left; background:#0f1420; border:1px solid #232a37; border-radius:12px;
             padding:16px 18px; margin:18px 0; color:#c6ccd6; font-size:14px; }
    code { background:#202634; border:1px solid #2d3647; border-radius:6px; padding:2px 7px;
           font-family:ui-monospace,monospace; color:#e6e8ec; }
    a { color:#5865f2; text-decoration:none; } a:hover { text-decoration:underline; }
    .foot { margin-top:18px; font-size:12px; color:#6b7482; }
  </style>
</head>
<body>
  <div class="card">
    <img src="/img/banner.jpg" alt="FORGOTTEN NFT" class="banner" />
    <p>Wallet verification for the FORGOTTEN Solana NFT community.</p>
    <div class="steps">
      To link your wallet:
      <ol style="margin:8px 0 0; padding-left:20px;">
        <li>Open the FORGOTTEN Discord server.</li>
        <li>Run <code>/wallet connect</code>.</li>
        <li>Open the private link it sends you and sign the message.</li>
      </ol>
    </div>
    <p>You will only ever be asked to <strong>sign a message</strong> — never to approve a transaction, send funds, or share your keys.</p>
    <div class="foot">
      <a href="https://forgottennft.com">forgottennft.com</a> · Verification links are single-use and expire.
    </div>
  </div>
</body>
</html>
""";
}
