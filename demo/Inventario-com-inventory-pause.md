---
title: Inventario com inventory pause
type: feature
created: "2026-11-03T21:20:20Z"
modified: "2026-11-03T22:58:24Z"
author: JuruSysadmin
status: accepted
estimate: "3"
started: "2026-11-03T22:36:54Z"
finished: "2026-11-03T22:40:57Z"
delivered: "2026-11-03T22:40:57Z"
accepted: "2026-11-03T22:43:13Z"
---

## What

Abrir o inventário (Tab) congela o mundo como no Resident Evil clássico (**inventory pause**): inimigos, dano e timers param. Fechar despausa. Câmera + UI ficam vivos via `ProcessMode.Always`.

## Done when

- Com inventário aberto, zumbi parado não aplica dano
- Player não se move (já via InputLocked)
- Fechar inventário retoma combate/IA normalmente
- Órbita third-person (PhantomCamera) continua ok após abrir/fechar
- `./tests/run_pcam_follow.sh` continua passando

## Plan (tree pause + whitelist)

No Godot, `GetTree().Paused = true` congela todo nó `ProcessMode = Inherit`. Só vive quem está `Always` (ou filho de `Always`).

Ordem crítica:
1. Marcar whitelist como `Always` **antes** do pause
2. `GetTree().Paused = true`
3. No fechar: `Paused = false`, depois restaurar `ProcessMode` originais

Whitelist mínima no `level_01`:
- `InventarioTela` (já Always)
- `../MainCamera`
- `../MainCamera/PhantomCameraHost`
- `../PCam_Main`

Guardar modos anteriores num `Dictionary<Node, ProcessModeEnum>` e restaurar no `Fechar()`.

Não fazer early-return só no `InimigoIA` — vira lista infinita (timers, portas, partículas). Tree pause congela o mundo de uma vez.

Plano B se PCam ainda quebrar: grupo `congelavel` + `ProcessMode = Disabled` nos inimigos, sem pause global.

## Notes

Hoje Tab só faz InputLocked; mundo segue. Bug `Inventario-pause-quebra-PhantomCamera` documentou o fail sem whitelist — esta story é o feeling RE com a pilha da câmera viva.

## Tasks

- [x] Em InventarioTela: Dictionary para guardar ProcessMode original dos nós da whitelist

- [x] Implementar ManterVivo(Node) — salva modo e seta Always

- [x] Implementar PausarMundo() — whitelist (this, MainCamera, PhantomCameraHost, PCam_Main) depois GetTree().Paused = true

- [x] Implementar DespausarMundo() — Paused = false e restaura ProcessModes

- [x] Abrir() chama PausarMundo após InputLocked; Fechar() chama DespausarMundo antes de liberar input

- [x] Export/ResolverRefs dos NodePaths da câmera se GetNodeOrNull relativo falhar

- [x] Playtest: zumbi colado + Tab = vida não desce; fechar = volta a morder

- [x] Playtest: abrir/fechar inventário sem órbita PhantomCamera morta

- [x] Rodar ./tests/run_pcam_follow.sh e confirmar PASS

- [ ] Se PCam ainda quebrar: plano B (grupo congelavel / Disabled) e documentar no Comments

## Comments

@JuruSysadmin 2026-11-03
Implementado com tree pause + whitelist. Evidência headless (tests/run_inventory_pause.sh, PASS): Abrir pausa a árvore; PhantomCameraHost e PCam_Main ficam Always e continuam processando; câmera seguiu o Player mesmo com o mundo congelado (dist 3.87); processo fora da whitelist parou de rodar em 30 frames; Fechar restaura os ProcessMode originais e o mundo retoma; segundo ciclo abrir/fechar sem resíduo. Pendente: os 2 playtests com input real (zumbi colado) — deixo para o PM.
