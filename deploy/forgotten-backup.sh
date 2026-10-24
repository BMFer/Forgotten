#!/usr/bin/env bash
# FORGOTTEN database backup.
#   - WAL-safe snapshot via sqlite3 .backup (safe while the bot is running)
#   - gzip + keep the 14 most recent copies locally
#   - if BACKUP_S3_BUCKET is set, also upload off-instance to S3 (no keys needed:
#     the EC2 instance profile grants write access to that bucket only)
# Installed to /usr/local/bin/forgotten-backup.sh, run daily from /etc/cron.d/forgotten-backup.
set -euo pipefail

DB=/var/lib/forgotten/forgotten.db
DIR=/var/lib/forgotten/backups
KEEP_LOCAL=14

# Optional config: BACKUP_S3_BUCKET=forgotten-nft-backups-xxxx
[ -f /etc/forgotten/backup.env ] && . /etc/forgotten/backup.env

mkdir -p "$DIR"
TS=$(date -u +%Y%m%d-%H%M%S)
OUT="$DIR/forgotten-$TS.db"

sqlite3 "$DB" ".backup '$OUT'"
gzip -f "$OUT"
GZ="$OUT.gz"

# Prune old local copies.
ls -1t "$DIR"/forgotten-*.db.gz 2>/dev/null | tail -n "+$((KEEP_LOCAL + 1))" | xargs -r rm -f

# Off-instance copy (durability against instance/volume loss).
if [ -n "${BACKUP_S3_BUCKET:-}" ]; then
  aws s3 cp "$GZ" "s3://$BACKUP_S3_BUCKET/db/$(basename "$GZ")" --only-show-errors
  echo "uploaded s3://$BACKUP_S3_BUCKET/db/$(basename "$GZ")"
fi

echo "backup OK: $GZ"
