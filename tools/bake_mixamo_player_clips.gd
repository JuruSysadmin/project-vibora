extends SceneTree

const CLIP_SRC := "mixamo_com"
const OUT := "res://art/personagens/mixamo/player_clips.tres"

func _init() -> void:
	var lib := AnimationLibrary.new()
	if not _add_clip(lib, "res://art/personagens/mixamo/idle.fbx", "idle", false):
		quit(1)
		return
	if not _add_clip(lib, "res://art/personagens/mixamo/walk_anim.fbx", "walk", true):
		quit(1)
		return
	var err := ResourceSaver.save(lib, OUT)
	print("bake_mixamo: saved ", OUT, " err=", err)
	quit(0 if err == OK else 1)

func _add_clip(lib: AnimationLibrary, path: String, clip_name: String, strip_root: bool) -> bool:
	if not ResourceLoader.exists(path):
		push_error("bake_mixamo: missing " + path)
		return false
	var packed: PackedScene = load(path)
	var root: Node = packed.instantiate()
	var ap := root.find_child("AnimationPlayer", true, false) as AnimationPlayer
	if ap == null or not ap.has_animation(CLIP_SRC):
		push_error("bake_mixamo: no '%s' in %s" % [CLIP_SRC, path])
		root.free()
		return false
	var anim: Animation = ap.get_animation(CLIP_SRC).duplicate(true)
	if strip_root:
		_strip_hips_xz(anim)
	anim.loop_mode = Animation.LOOP_LINEAR
	if lib.has_animation(clip_name):
		lib.remove_animation(clip_name)
	lib.add_animation(clip_name, anim)
	root.free()
	return true

func _strip_hips_xz(anim: Animation) -> void:
	for i in anim.get_track_count():
		if anim.track_get_type(i) != Animation.TYPE_POSITION_3D:
			continue
		if not String(anim.track_get_path(i)).contains("Hips"):
			continue
		var base: Vector3 = anim.position_track_interpolate(i, 0.0)
		for k in anim.track_get_key_count(i):
			var p: Vector3 = anim.track_get_key_value(i, k)
			p.x = base.x
			p.z = base.z
			anim.track_set_key_value(i, k, p)
