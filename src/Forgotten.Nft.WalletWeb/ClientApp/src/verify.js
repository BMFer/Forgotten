// FORGOTTEN wallet-verification client. Supports injected browser wallets
// (Phantom/Solflare via window.solana) and TipLink (Google sign-in web wallet via
// the TipLink Wallet Adapter). Both produce an Ed25519 signature over the exact
// server-built challenge message; the server verifies it. Keys never leave the wallet.
import { TipLinkWalletAdapter } from '@tiplink/wallet-adapter';

// Config is delivered as a non-executed JSON block (not an inline script) so the page
// can enforce a strict Content-Security-Policy (script-src 'self', no 'unsafe-inline').
const cfg = (() => {
  try {
    const node = document.getElementById('forgotten-config');
    return node ? JSON.parse(node.textContent) : {};
  } catch {
    return {};
  }
})();
const el = (id) => document.getElementById(id);
const setStatus = (msg, cls) => {
  const s = el('status');
  if (s) { s.textContent = msg; s.className = 'status ' + (cls || ''); }
};

function toBase64(bytes) {
  let binary = '';
  for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
  return btoa(binary);
}

async function challengeAndSubmit(walletAddress, signMessage) {
  setStatus('Requesting challenge…');
  const chRes = await fetch(`/api/verify/${cfg.token}/challenge`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ walletAddress }),
  });
  if (!chRes.ok) {
    setStatus('This verification link is invalid or has expired. Run /wallet connect again.', 'err');
    return false;
  }
  const { message } = await chRes.json();

  setStatus('Please approve the signature request in your wallet…');
  const signature = toBase64(await signMessage(new TextEncoder().encode(message)));

  setStatus('Verifying…');
  const vRes = await fetch(`/api/verify/${cfg.token}`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ walletAddress, signature }),
  });
  const vJson = await vRes.json().catch(() => ({}));
  if (vRes.ok && vJson.success) {
    setStatus('✅ Wallet verified! You can close this tab and return to Discord.', 'ok');
    return true;
  }
  setStatus('❌ ' + (vJson.message || vJson.error || 'Verification failed.'), 'err');
  return false;
}

async function connectInjected() {
  const btn = el('injected');
  if (btn) btn.disabled = true;
  try {
    const provider = window.solana || (window.phantom && window.phantom.solana);
    if (!provider || typeof provider.signMessage !== 'function') {
      setStatus('No compatible browser wallet detected. Install Phantom/Solflare, or use TipLink.', 'err');
      if (btn) btn.disabled = false;
      return;
    }
    setStatus('Connecting wallet…');
    const resp = await provider.connect();
    const wallet = resp.publicKey.toString();
    if (el('wallet')) el('wallet').textContent = wallet;
    const ok = await challengeAndSubmit(wallet, async (bytes) => {
      const signed = await provider.signMessage(bytes, 'utf8');
      return signed.signature || signed;
    });
    if (!ok && btn) btn.disabled = false;
  } catch (e) {
    const base = (e && e.message) ? e.message : 'Something went wrong.';
    const hint = isMobileUA() ? ' — on mobile, use “Open in Phantom/Solflare” below instead.' : '';
    setStatus('❌ ' + base + hint, 'err');
    if (btn) btn.disabled = false;
  }
}

async function connectTipLink() {
  const btn = el('tiplink');
  if (btn) btn.disabled = true;
  let adapter;
  try {
    adapter = new TipLinkWalletAdapter({ title: 'FORGOTTEN', clientId: cfg.tipLinkClientId, theme: 'dark' });
    setStatus('Opening TipLink…');
    await adapter.connect();
    const wallet = adapter.publicKey.toBase58();
    if (el('wallet')) el('wallet').textContent = wallet;
    await challengeAndSubmit(wallet, async (bytes) => adapter.signMessage(bytes));
  } catch (e) {
    setStatus('❌ ' + (e && e.message ? e.message : 'TipLink connection failed.'), 'err');
    if (btn) btn.disabled = false;
  } finally {
    try { if (adapter) await adapter.disconnect(); } catch { /* ignore */ }
  }
}

// On a phone, a normal mobile browser has no wallet extension (and Brave's built-in
// wallet can't sign — it throws "An internal error has occurred"). The reliable fix is
// to reopen this page inside the wallet app's own in-app browser via a universal link,
// where window.solana is injected and the normal connect/sign flow below just works.
function isMobileUA() {
  return /Android|iPhone|iPad|iPod/i.test(navigator.userAgent || '');
}

function setupMobile() {
  if (!isMobileUA()) return; // desktop: extension flow is fine

  // Always offer the wallet-app deep-links on mobile. We can't reliably detect a *real*
  // wallet in-app browser because Brave's built-in wallet impersonates Phantom
  // (window.solana.isPhantom = true) yet can't sign — so trust nothing, always show these.
  const target = window.location.href;
  const ref = window.location.origin;
  const links = {
    'open-phantom': `https://phantom.com/ul/browse/${encodeURIComponent(target)}?ref=${encodeURIComponent(ref)}`,
    'open-solflare': `https://solflare.com/ul/v1/browse/${encodeURIComponent(target)}?ref=${encodeURIComponent(ref)}`,
  };
  for (const id of Object.keys(links)) {
    el(id)?.addEventListener('click', () => { window.location.href = links[id]; });
  }
  el('mobile-wallets')?.classList.remove('hidden');

  // Only hide the in-page button when there's no injected wallet at all (plain mobile browser).
  const hasProvider = !!(window.solana || (window.phantom && window.phantom.solana) || window.solflare);
  if (!hasProvider) el('injected')?.classList.add('hidden');

  setStatus('On your phone, use “Open in Phantom” or “Open in Solflare” to connect inside your wallet app.');
}

document.addEventListener('DOMContentLoaded', () => {
  el('injected')?.addEventListener('click', connectInjected);
  el('tiplink')?.addEventListener('click', connectTipLink);
  setupMobile();
});
