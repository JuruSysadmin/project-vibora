---
title: Polir textos e uso do inventario
type: bug
created: "2026-11-04T00:04:57Z"
modified: "2026-11-04T00:14:22Z"
author: JuruSysadmin
status: started
started: "2026-11-04T00:05:01Z"
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

## Attachments

## Rejection notes

- 2026-11-04: Playtest: painel do inventario aparece no canto esquerdo, cortado e com texto ilegivel. Nao e regressao dos textos (mexi em string/mouse/dead code) - e layout de base nunca verificado visualmente.
