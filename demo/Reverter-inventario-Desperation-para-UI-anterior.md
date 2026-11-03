---
title: Reverter inventario Desperation para UI anterior
type: bug
created: "2026-11-03T20:46:52Z"
modified: "2026-11-03T20:47:21Z"
author: JuruSysadmin
status: delivered
started: "2026-11-03T20:46:52Z"
finished: "2026-11-03T20:47:21Z"
delivered: "2026-11-03T20:47:21Z"
project: demo
---

## Problem statement

Inventário Desperation (letterbox/arte) ficou ruim; após abrir/fechar a câmera third-person não voltava.

## Possible solution

Restaurar `InventarioTela` commitado (UI antiga). Remover `GetTree().Paused` (só InputLocked + mouse). Paths `../Player`.

## Comments
