---
title: 'Player: contrato de input e dependencias sem fallback'
type: bug
created: "2026-11-04T18:42:33Z"
modified: "2026-11-04T18:56:27Z"
author: JuruSysadmin
status: accepted
started: "2026-11-04T18:42:33Z"
finished: "2026-11-04T18:50:12Z"
delivered: "2026-11-04T18:50:12Z"
accepted: "2026-11-04T18:56:27Z"
---

## Problem statement

## Possible solution

## Comments
@JuruSysadmin 2026-11-04
Correcoes do review. VERIFICADO: com [Export] public, player.set("InputLocked", true) + get devolve true — o teste inventory_pause_regression cobre o round-trip. Sem o [Export] a propriedade nao era alcancavel e DoorTransition/InventarioTela travavam nada, em silencio.

CORRECAO DO MEU PROPRIO REVIEW: eu recomendei remover _pcam.Call("set_follow_target", this) por ser 'segunda fonte de verdade'. Errado: o setter arma _should_follow, conecta tree_exiting, registra o alvo no spring arm e detecta corpo fisico. Removido, o teste falhou na hora ('camera nao seguiu dentro do pause, dist=11.23'). Revertido e comentado no codigo.

Demais: SequenciaMorte e PhantomCamera com caminho unico e erro explicito (sem ?? e sem FindChild); Free() no lugar de QueueFree() em no fora da arvore; erro explicito em cada saida silenciosa (clip, lib, root motion); LimiarAndando e TempoBlendAnimacao exportados.

Build 0 erros; run_inventory_pause PASS (14 checks); run_pcam_follow PASS.

## Attachments
