---
title: Player com BASEmodel animado
type: feature
created: "2026-11-03T17:21:56Z"
modified: "2026-11-03T17:21:56Z"
author: JuruSysadmin
status: unstarted
estimate: "5"
project: demo
---

## What

Trocar o mesh placeholder do player (cápsula/cilindro em `scenes/actors/player.tscn`) pelo `BASEmodel.blend`, usando as animações que o arquivo já traz.

## Done when

- O blend está versionado sob `art/` (não solto na raiz)
- O player na cena usa o modelo em vez da CapsuleMesh
- Pelo menos idle + walk (ou o equivalente que o blend tiver) tocam no movimento
- Colisão/câmera continuam jogáveis

## Notes

Asset já existe na raiz do repo (`BASEmodel.blend`) com animações. Prioridade acima do build exportado — a demo mostrável deve parecer personagem, não primitiva.
