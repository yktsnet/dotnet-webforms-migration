#!/usr/bin/env bash
# デモ切替スクリプト
# 使用例:
#   ./infrastructure/switch-demo.sh winforms   # WinForms 起動・WebForms 停止
#   ./infrastructure/switch-demo.sh webforms   # WebForms 起動・WinForms 停止
set -euo pipefail

TARGET="${1:-}"
WINFORMS="winforms-migration.service"
WEBFORMS="webforms-migration.service"

if [[ "$TARGET" != "winforms" && "$TARGET" != "webforms" ]]; then
  echo "使用方法: $0 [winforms|webforms]"
  exit 1
fi

echo "==> 切替: $TARGET"

if [[ "$TARGET" == "winforms" ]]; then
  echo "  停止: $WEBFORMS"
  sudo systemctl stop "$WEBFORMS" || true
  echo "  起動: $WINFORMS"
  sudo systemctl start "$WINFORMS"
else
  echo "  停止: $WINFORMS"
  sudo systemctl stop "$WINFORMS" || true
  echo "  起動: $WEBFORMS"
  sudo systemctl start "$WEBFORMS"
fi

echo ""
echo "==> 状態確認"
systemctl is-active "$WINFORMS" && echo "  $WINFORMS: active" || echo "  $WINFORMS: inactive"
systemctl is-active "$WEBFORMS" && echo "  $WEBFORMS: active" || echo "  $WEBFORMS: inactive"
echo "==> done"
