#!/usr/bin/env bash
set -euo pipefail

if [[ -f .env ]]; then
  set -a; source .env; set +a
fi

REMOTE="${DEPLOY_HOST:-sv6}"
REMOTE_USER="${DEPLOY_USER:-sv6}"
APP_PATH="/home/${REMOTE_USER}/github-public/attendance-system-migration"

echo "==> [1/2] .env 転送"
rsync -az .env "$REMOTE:$APP_PATH/.env"

echo "==> [2/2] docker compose up --build"
ssh "$REMOTE" "cd $APP_PATH && docker compose up -d --build"

echo "==> done"
