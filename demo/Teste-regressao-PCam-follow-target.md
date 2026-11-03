---
title: Teste regressao PCam follow_target
type: chore
created: "2026-11-03T21:08:08Z"
modified: "2026-11-03T21:09:50Z"
author: JuruSysadmin
status: accepted
started: "2026-11-03T21:08:08Z"
finished: "2026-11-03T21:08:52Z"
delivered: "2026-11-03T21:08:52Z"
accepted: "2026-11-03T21:08:52Z"
project: demo
---

## What

Teste headless que falha se `PCam_Main.follow_target` voltar a ser null após extract de prefabs.

## Done when

- `./tests/run_pcam_follow.sh` passa (exit 0)
- Checa follow_target == Player, follow_mode THIRD_PERSON, câmera acompanha movimento

## Notes

`tests/pcam_follow_regression.gd` + runner. Rodar antes de mexer em Player/PCam/level_01.
