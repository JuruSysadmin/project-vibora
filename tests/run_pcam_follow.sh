#!/usr/bin/env bash
# Regressão headless: PCam follow_target ↔ Player (evita câmera “fixa” pós-extract).
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

echo "[run_pcam_follow] using: $GODOT"
echo "[run_pcam_follow] building C# (BindPhantomCamera)..."
dotnet build --nologo -v q

# Filtra ruído do addon em modo -s; falha se a linha PASS não aparecer ou exit != 0
set +e
OUT="$("$GODOT" --headless --path "$ROOT" -s "res://tests/pcam_follow_regression.gd" 2>&1)"
CODE=$?
set -e

echo "$OUT" | grep -E '\[pcam_follow_regression\]|PASS|FAIL' || true

if echo "$OUT" | grep -q '\[pcam_follow_regression\] PASS'; then
  echo "[run_pcam_follow] OK"
  exit 0
fi

echo "[run_pcam_follow] FAILED (godot exit=$CODE)" >&2
exit 1
