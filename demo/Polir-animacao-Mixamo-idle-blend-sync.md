---
title: Polir animacao Mixamo idle blend sync
type: chore
created: "2026-11-04T17:59:18Z"
modified: "2026-11-04T18:06:32Z"
author: JuruSysadmin
status: accepted
started: "2026-11-04T17:59:18Z"
finished: "2026-11-04T18:06:32Z"
delivered: "2026-11-04T18:06:32Z"
accepted: "2026-11-04T18:06:32Z"
---

## What

Polir Mixamo: idle + crossfade + walk in-place + SpeedScale sync com MoveSpeed.

## Done when

- Idle não cai em Stop/T-pose seco ao parar
- Walk sem root motion XZ (sem “deslizar sozinho”)
- Blend idle↔walk (~0.2s)
- SpeedScale acompanha velocidade horizontal

## Notes

Idle atual = `Take 001` do FBX Walking (hips estáticos). Para idle “respirando” de verdade, baixar Idle no Mixamo (Without Skin) e somar na library.
