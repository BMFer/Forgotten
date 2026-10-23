#!/usr/bin/env bash
# Publishes the bot + wallet web app and deploys them to the EC2 host over SSH.
# Run from the repo root (works in Git Bash on Windows). Prerequisites on the server
# are handled by cloud-init; /etc/forgotten/forgotten.env must exist with your secrets.
#
#   deploy/publish-and-deploy.sh <host-or-ip> <path-to-key.pem> [ssh-user]
#
# Set RID=linux-arm64 if you used a t4g (ARM) instance.
set -euo pipefail

HOST="${1:?usage: publish-and-deploy.sh <host-or-ip> <key.pem> [ssh-user]}"
KEY="${2:?ssh key path required}"
SSH_USER="${3:-ubuntu}"
RID="${RID:-linux-x64}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SSHOPTS=(-i "$KEY" -o StrictHostKeyChecking=accept-new)

echo "==> [1/4] Building WalletWeb client bundle (TipLink + injected wallets)"
( cd "$ROOT/src/Forgotten.Nft.WalletWeb/ClientApp" && npm ci && npm run build )

echo "==> [2/4] Publishing self-contained ($RID)"
rm -rf "$ROOT/artifacts"
dotnet publish "$ROOT/src/Forgotten.Bot/Forgotten.Bot.csproj" \
  -c Release -r "$RID" --self-contained true -o "$ROOT/artifacts/bot"
dotnet publish "$ROOT/src/Forgotten.Nft.WalletWeb/Forgotten.Nft.WalletWeb.csproj" \
  -c Release -r "$RID" --self-contained true -o "$ROOT/artifacts/walletweb"

echo "==> [3/4] Packaging + uploading"
tar -C "$ROOT/artifacts/bot" -czf "$ROOT/artifacts/bot.tgz" .
tar -C "$ROOT/artifacts/walletweb" -czf "$ROOT/artifacts/walletweb.tgz" .
scp "${SSHOPTS[@]}" \
  "$ROOT/artifacts/bot.tgz" "$ROOT/artifacts/walletweb.tgz" \
  "$ROOT/deploy/systemd/forgotten-bot.service" "$ROOT/deploy/systemd/forgotten-walletweb.service" \
  "$SSH_USER@$HOST:/tmp/"

echo "==> [4/4] Installing + restarting services"
ssh "${SSHOPTS[@]}" "$SSH_USER@$HOST" 'bash -s' <<'REMOTE'
set -e
sudo mkdir -p /opt/forgotten/bot /opt/forgotten/walletweb
sudo rm -rf /opt/forgotten/bot/* /opt/forgotten/walletweb/*
sudo tar -C /opt/forgotten/bot -xzf /tmp/bot.tgz
sudo tar -C /opt/forgotten/walletweb -xzf /tmp/walletweb.tgz
sudo chmod +x /opt/forgotten/bot/Forgotten.Bot /opt/forgotten/walletweb/Forgotten.Nft.WalletWeb
sudo chown -R forgotten:forgotten /opt/forgotten
sudo mv /tmp/forgotten-bot.service /tmp/forgotten-walletweb.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now forgotten-bot forgotten-walletweb
sudo systemctl restart forgotten-bot forgotten-walletweb
rm -f /tmp/bot.tgz /tmp/walletweb.tgz
sleep 2
systemctl --no-pager --lines=8 status forgotten-bot forgotten-walletweb || true
REMOTE

echo "==> Done. Verify: curl -sS https://<your-domain>/health"
