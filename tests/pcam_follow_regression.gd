extends SceneTree
## Regressão: PCam_Main.follow_target deve apontar para o Player após o load.
## Introduzido quando PCam virou PackedScene (NodePath externo virava null).
##
## Rodar: ./tests/run_pcam_follow.sh
## Ou:    godot --headless --path . -s tests/pcam_follow_regression.gd

const LEVEL := "res://scenes/level_01.tscn"
const FRAMES_BIND := 20
const FRAMES_FOLLOW := 45
const MAX_DIST := 8.0

var _failed := false


func _init() -> void:
	call_deferred("_run")


func _fail(msg: String) -> void:
	push_error("[pcam_follow_regression] FAIL: %s" % msg)
	print("[pcam_follow_regression] FAIL: %s" % msg)
	_failed = true


func _ok(msg: String) -> void:
	print("[pcam_follow_regression] OK: %s" % msg)


func _run() -> void:
	var packed: PackedScene = load(LEVEL) as PackedScene
	if packed == null:
		_fail("não carregou %s" % LEVEL)
		quit(1)
		return

	var level: Node = packed.instantiate()
	root.add_child(level)

	for _i in FRAMES_BIND:
		await process_frame

	var player := level.get_node_or_null("Player") as Node3D
	if player == null:
		_fail("Player não encontrado na raiz do level")
		quit(1)
		return

	var pcam := level.find_child("PCam_Main", true, false) as Node
	if pcam == null:
		_fail("PCam_Main não encontrado (nem sob SpringArm3D)")
		quit(1)
		return

	if not pcam.has_method("get_follow_target"):
		_fail("PCam_Main sem get_follow_target (script PhantomCamera?)")
		quit(1)
		return

	var target: Variant = pcam.call("get_follow_target")
	if target == null:
		_fail("follow_target é null — regressão do extract de prefab")
		quit(1)
		return

	if target != player:
		_fail("follow_target=%s esperado Player=%s" % [target, player])
		quit(1)
		return

	_ok("follow_target == Player")

	var mode: int = int(pcam.get("follow_mode"))
	if mode != 6:
		_fail("follow_mode=%s esperado 6 (THIRD_PERSON)" % mode)
		quit(1)
		return

	_ok("follow_mode == THIRD_PERSON (6)")

	var cam := level.get_node_or_null("MainCamera") as Camera3D
	if cam == null:
		_fail("MainCamera não encontrado")
		quit(1)
		return

	player.global_position = Vector3(10.0, 0.1, 4.0)
	for _i in FRAMES_FOLLOW:
		await process_frame

	var dist := cam.global_position.distance_to(player.global_position)
	if dist > MAX_DIST:
		_fail("câmera não acompanhou o Player (dist=%.2f > %.2f)" % [dist, MAX_DIST])
		quit(1)
		return

	_ok("câmera acompanhou Player (dist=%.2f)" % dist)
	print("[pcam_follow_regression] PASS")
	quit(0 if not _failed else 1)
