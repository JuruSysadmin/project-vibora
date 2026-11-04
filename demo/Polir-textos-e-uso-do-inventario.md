---
title: Polir textos e uso do inventario
type: bug
created: "2026-11-04T00:04:57Z"
modified: "2026-11-04T00:38:25Z"
author: JuruSysadmin
status: accepted
started: "2026-11-04T00:05:01Z"
finished: "2026-11-04T00:28:45Z"
delivered: "2026-11-04T00:28:45Z"
accepted: "2026-11-04T00:38:25Z"
---

## Problem statement

## Possible solution

## Comments
@JuruSysadmin 2026-11-04
- Header duplicado: fallback de AtualizarStatus escrevia 'STATUS' de novo; agora so '—'.
- Usar com vida cheia nao consome mais o frasco; mostra aviso amarelo no painel de descricao ate trocar a selecao.
- Fechar() guarda e devolve o MouseMode anterior em vez de forcar Captured.
- Textos em PT: CONDIÇÃO BEM/CUIDADO/PERIGO, INVENTÁRIO, 'Frasco de cura... Usar', Vazio, '[Tab] Fechar [Setas] Mover'.
- Codigo morto: campo _paineis removido (era preenchido e nunca lido); Modulate=White redundante removido.
Build 0 erros; run_inventory_pause e run_pcam_follow PASS.

@JuruSysadmin 2026-11-04
Causa do painel no canto: AnchorsPreset no inicializador de objeto nao pega (Raiz ficava 0x0), entao o PanelContainer centralizado num pai sem tamanho caia em (-429,-210) e sobrava so o canto na tela. Medido headless antes/depois: Raiz 0x0 -> 1152x648; Painel (-429,-210,859x420) -> (216,114,720x420). Corrigido com SetAnchorsAndOffsetsPreset depois do AddChild; slot 88->72 para caber em 720. Teste agora falha se a Raiz nao preencher a tela ou o painel sair do viewport.

## Attachments

## Rejection notes

- 2026-11-04: Playtest: painel do inventario aparece no canto esquerdo, cortado e com texto ilegivel. Nao e regressao dos textos (mexi em string/mouse/dead code) - e layout de base nunca verificado visualmente.
