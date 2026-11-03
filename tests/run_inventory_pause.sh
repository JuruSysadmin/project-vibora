#!/usr/bin/env bash
# Regressão headless: inventory pause congela o mundo sem matar a PhantomCamera.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

GODOT="${GODOT:-}"
if [[ -z "$GODOT" ]]; then
  if command -v godot >/dev/null 2>&1; then
    GODOT="$(command -v godot)"
  elif [[ -x "$HOME/.local/opt/godot/4.7.2-mono/Godot_v4.7.2-stable_mono_linux.x86_64" ]]; then
    GODOT="$HOME/.local/opt/godot/4.7.2-mono/Godot_v4.7.2-stable_mono_linux.x86_64"
  else
    echo "godot não encontrado; defina GODOT=/caminho/do/binario" >&2
    exit 127
  fi
fi

echo "[run_inventory_pause] using: $GODOT"
echo "[run_inventory_pause] building C#..."
dotnet build --nologo -v q

# Filtra ruído do addon em modo -s; falha se a linha PASS não aparecer ou exit != 0
set +e
OUT="$("$GODOT" --headless --path "$ROOT" -s "res://tests/inventory_pause_regression.gd" 2>&1)"
CODE=$?
set -e

echo "$OUT" | grep -E '\[inventory_pause_regression\]|PASS|FAIL' || true

if echo "$OUT" | grep -q '\[inventory_pause_regression\] PASS'; then
  echo "[run_inventory_pause] OK"
  exit 0
fi

echo "[run_inventory_pause] FAILED (godot exit=$CODE)" >&2
exit 1
