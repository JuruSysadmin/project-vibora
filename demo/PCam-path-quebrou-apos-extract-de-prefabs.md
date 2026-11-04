---
title: PCam path quebrou apos extract de prefabs
type: bug
created: "2026-11-03T20:59:27Z"
modified: "2026-11-04T17:57:52Z"
author: JuruSysadmin
status: accepted
started: "2026-11-03T20:59:27Z"
finished: "2026-11-03T20:59:53Z"
delivered: "2026-11-03T20:59:53Z"
project: demo
accepted: "2026-11-04T17:44:50Z"
---

## Problem statement

Após extrair Player/PCam em PackedScenes, `Player` usava `%PCam_Main`. Unique-name não atravessa o owner da instância → órbita third-person morta (câmera parece fixa).

## Possible solution

`PhantomCameraPath = ../PCam_Main`. Unique name no instance do level. Mesmo para `SequenciaMorte` (`../SequenciaMorte`).

## Comments
