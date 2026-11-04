---
title: Inventario com arte Desperation
type: feature
created: "2026-11-03T20:25:41Z"
modified: "2026-11-04T17:58:02Z"
author: JuruSysadmin
status: accepted
estimate: "3"
started: "2026-11-03T20:38:13Z"
finished: "2026-11-03T20:39:05Z"
delivered: "2026-11-03T20:39:05Z"
project: demo
accepted: "2026-11-04T17:41:50Z"
---

## What

Usar `res://art/telas/hud/desperation_inventory_2d.png` como layout fixo 1920×1080 (letterbox), com hitboxes nos 8 slots e faixa inferior dinâmica.

## Done when

- Inventário abre em letterbox 16:9 (sem cropar a arte)
- Seleção/ícone do frasco alinham aos slots #1–#8 da arte
- Descrição dinâmica na faixa de baixo (cobre o texto baked)
- Chrome da PNG (header, retrato, abas) não é redesenhado por cima

## Notes

Coords dos slots em `SpecSlotsDesign` — afinar se ainda driftar 1–2 px.

## Comments

@JuruSysadmin 2026-11-03
Revertido a pedido do PM — UI Desperation desfeita; voltou inventário anterior sem pause global.
