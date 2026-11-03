---
title: Camera third-person ainda quebrada apos fix path
type: bug
created: "2026-11-03T21:02:49Z"
modified: "2026-11-03T21:06:24Z"
author: JuruSysadmin
status: delivered
started: "2026-11-03T21:02:49Z"
finished: "2026-11-03T21:06:24Z"
delivered: "2026-11-03T21:06:24Z"
project: demo
---

## Problem statement

Depois do extract, `PCam_Main.follow_target` ficava `null` (NodePath externo não sobrevive no PackedScene). Câmera parecia fixa. Timing C# vs GDScript SpringArm também quebrava `../PCam_Main`.

## Possible solution

`Player.BindPhantomCamera` deferred: encontra PCam (path / % / FindChild) e `set_follow_target(this)`. Override `follow_target` no instance do level_01.

## Comments
