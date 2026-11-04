---
title: Player com BASEmodel animado
type: feature
created: "2026-11-03T17:21:56Z"
modified: "2026-11-04T19:05:49Z"
author: JuruSysadmin
status: accepted
estimate: "5"
project: demo
started: "2026-11-04T17:52:58Z"
finished: "2026-11-04T17:55:34Z"
delivered: "2026-11-04T17:55:34Z"
accepted: "2026-11-04T19:05:49Z"
---

## What

Trocar o mesh placeholder do player pela personagem Mixamo (`art/personagens/mixamo/walk.fbx`), com animação de walk no movimento.

## Done when

- Modelo Mixamo versionado sob `art/personagens/mixamo/`
- `player.tscn` usa o modelo no lugar da CapsuleMesh
- Walk (`mixamo_com`) toca ao andar; para ao parar
- Colisão/câmera continuam jogáveis

## Notes

Originalmente BASEmodel.blend (apagado). Substituído por Mixamo Walking With Skin. Idle ainda é pose de rest (sem clip idle separado).
