---
title: Inventario pause quebra PhantomCamera
type: bug
created: "2026-11-03T20:44:52Z"
modified: "2026-11-04T17:58:02Z"
author: JuruSysadmin
status: accepted
started: "2026-11-03T20:45:06Z"
finished: "2026-11-03T20:45:43Z"
delivered: "2026-11-03T20:45:43Z"
project: demo
accepted: "2026-11-04T17:41:50Z"
---

## Problem statement

Abrir/fechar o inventário (`GetTree().Paused = true`) congelava MainCamera + PhantomCameraHost + PCam. Ao despausar, o third-person ficava “câmera fixa” / órbita estranha.

## Possible solution

Antes do pause, forçar `ProcessMode = Always` nesses nós e restaurar ao fechar. Corrigir NodePaths do inventário para `../Player` (antes apontavam filho inexistente).

## Comments
