#!/usr/bin/env bash
set -euo pipefail

# .env 読み込み
if [[ -f .env ]]; then
  set -a; source .env; set +a
fi

REMOTE="${DEPLOY_HOST:?DEPLOY_HOST が未設定（.env を確認）}"
APP_DIR="/home/${DEPLOY_USER:?DEPLOY_USER が未設定（.env を確認）}/apps/webforms-migration"

echo "==> [1/2] rsync"
rsync -az --delete \
  --exclude='.git/' \
  --exclude='node_modules/' \
  --exclude='src/Web/dist/' \
  --exclude='publish/' \
  --exclude='.env' \
  ./ \
  "$REMOTE:$APP_DIR/"

rsync -az .env "$REMOTE:$APP_DIR/.env"

echo "==> [2/2] docker compose up --build"
ssh "$REMOTE" "cd '$APP_DIR' && docker compose up -d --build"

echo "==> done"
