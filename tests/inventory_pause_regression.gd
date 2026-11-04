extends SceneTree
## Regressão: abrir o inventário congela o mundo (inventory pause) sem matar a câmera.
##
## O que este teste protege:
##   - Abrir() liga GetTree().Paused (o "mundo" para de processar)
##   - a whitelist da câmera fica PROCESS_MODE_ALWAYS antes do pause
##   - Fechar() despausa e devolve os ProcessMode originais
##   - a órbita da PhantomCamera continua viva depois de abrir/fechar
##
## Rodar: ./tests/run_inventory_pause.sh
## Ou:    godot --headless --path . -s tests/inventory_pause_regression.gd

const LEVEL := "res://scenes/level_01.tscn"
const FRAMES_BIND := 20
const FRAMES_PAUSED := 30
const FRAMES_FOLLOW := 45
const MAX_DIST := 8.0

const MODE_ALWAYS := 3


## Contra-prova do congelamento: fora da whitelist, este nó deve parar no pause.
class Probe extends Node:
	var ticks := 0
	var physics_ticks := 0

	func _process(_delta: float) -> void:
		ticks += 1

	func _physics_process(_delta: float) -> void:
		physics_ticks += 1


var _failed := false


func _init() -> void:
	call_deferred("_run")


func _fail(msg: String) -> void:
	push_error("[inventory_pause_regression] FAIL: %s" % msg)
	print("[inventory_pause_regression] FAIL: %s" % msg)
	_failed = true


func _ok(msg: String) -> void:
	print("[inventory_pause_regression] OK: %s" % msg)


func _run() -> void:
	# Viewport real: em headless o padrão é 64×64, e o painel (720 px de mínimo)
	# nunca caberia — o teste de layout viraria falso positivo.
	root.size = Vector2i(1152, 648)
	await process_frame

	var packed: PackedScene = load(LEVEL) as PackedScene
	if packed == null:
		_fail("não carregou %s" % LEVEL)
		quit(1)
		return

	var level: Node = packed.instantiate()
	root.add_child(level)

	var inv := level.get_node_or_null("InventarioTela")
	if inv == null:
		_fail("InventarioTela não encontrada na raiz do level")
		quit(1)
		return

	if not inv.has_method("Abrir") or not inv.has_method("Fechar"):
		_fail("InventarioTela sem Abrir/Fechar (script C# não bindou?)")
		quit(1)
		return

	var cam := level.get_node_or_null("MainCamera") as Camera3D
	var host := level.get_node_or_null("MainCamera/PhantomCameraHost")
	var pcam := level.get_node_or_null("PCam_Main")
	if cam == null or host == null or pcam == null:
		_fail("câmera incompleta: MainCamera=%s PhantomCameraHost=%s PCam_Main=%s" % [cam, host, pcam])
		quit(1)
		return

	for _i in FRAMES_BIND:
		await process_frame

	# Baseline antes de qualquer pause.
	if paused:
		_fail("a árvore já começa pausada")
		quit(1)
		return

	# --- Layout: a raiz da UI precisa preencher a tela e o painel caber nela ---
	# Regressão real: âncoras setadas no inicializador do objeto deixavam a Raiz
	# 0×0, e o painel era centralizado num pai sem tamanho — aparecia cortado no
	# canto superior esquerdo do inventário.
	var raiz := inv.get_node_or_null("Raiz") as Control
	if raiz == null:
		_fail("InventarioTela sem nó 'Raiz'")
		quit(1)
		return

	var viewport := root.get_visible_rect()
	if not raiz.size.is_equal_approx(viewport.size):
		_fail("Raiz não preenche a tela: size=%s viewport=%s (âncoras não aplicadas?)" % [raiz.size, viewport.size])
		quit(1)
		return

	_ok("Raiz preenche a tela (%s)" % raiz.size)

	var painel := raiz.get_node_or_null("Painel") as Control
	if painel == null:
		_fail("InventarioTela sem nó 'Painel'")
		quit(1)
		return

	var rec := painel.get_global_rect()
	if rec.position.x < -0.5 or rec.position.y < -0.5 \
			or rec.end.x > viewport.size.x + 0.5 or rec.end.y > viewport.size.y + 0.5:
		_fail("Painel fora da tela: rect=%s viewport=%s" % [rec, viewport.size])
		quit(1)
		return

	var folga_esquerda := rec.position.x
	var folga_direita := viewport.size.x - rec.end.x
	if absf(folga_esquerda - folga_direita) > 2.0:
		_fail("Painel não está centralizado: folga esquerda=%.1f direita=%.1f" % [folga_esquerda, folga_direita])
		quit(1)
		return

	_ok("Painel cabe e está centralizado: rect=%s" % rec)

	var probe := Probe.new()
	probe.name = "PauseProbe"
	root.add_child(probe)

	for _i in 10:
		await process_frame

	if probe.ticks == 0:
		_fail("probe não processou nem despausado; teste inválido")
		quit(1)
		return

	_ok("probe processa com o jogo rodando (ticks=%d)" % probe.ticks)

	var modo_cam_antes: int = cam.process_mode
	var modo_host_antes: int = host.process_mode
	var modo_pcam_antes: int = pcam.process_mode

	# --- Abrir ---
	inv.call("Abrir")
	await process_frame

	if not paused:
		_fail("Abrir() não pausou a árvore (GetTree().Paused continua false)")
		quit(1)
		return

	_ok("Abrir() pausou a árvore")

	if inv.get("Aberto") != true:
		_fail("Aberto != true depois de Abrir()")
		quit(1)
		return

	for par in [["MainCamera", cam], ["MainCamera/PhantomCameraHost", host], ["PCam_Main", pcam]]:
		if int(par[1].process_mode) != MODE_ALWAYS:
			_fail("%s com process_mode=%d durante o pause (esperado Always=3)" % [par[0], int(par[1].process_mode)])
			quit(1)
			return

	_ok("whitelist da câmera em PROCESS_MODE_ALWAYS durante o pause")

	# --- Mundo congelado ---
	var ticks_antes: int = probe.ticks
	var phys_antes: int = probe.physics_ticks
	var inimigo := level.get_node_or_null("ZumbiLento") as Node3D
	var pos_antes := inimigo.global_position if inimigo != null else Vector3.ZERO

	for _i in FRAMES_PAUSED:
		await process_frame

	if probe.ticks != ticks_antes:
		_fail("mundo não congelou: probe ticks %d -> %d com o inventário aberto" % [ticks_antes, probe.ticks])
		quit(1)
		return

	if probe.physics_ticks != phys_antes:
		_fail("mundo não congelou: probe physics_ticks %d -> %d com o inventário aberto" % [phys_antes, probe.physics_ticks])
		quit(1)
		return

	if inimigo != null and not inimigo.global_position.is_equal_approx(pos_antes):
		_fail("inimigo andou durante o pause (%.2f -> %.2f)" % [pos_antes, inimigo.global_position])
		quit(1)
		return

	_ok("mundo congelado: nada processou em %d frames" % FRAMES_PAUSED)

	# MainCamera é uma Camera3D sem script (não tem _process próprio); quem processa
	# de fato é o host e a PCam — é neles que a órbita morre se o pause os pegar.
	if not host.is_processing():
		_fail("PhantomCameraHost parou de processar durante o pause (órbita morta)")
		quit(1)
		return

	if not pcam.is_processing():
		_fail("PCam_Main parou de processar durante o pause (órbita morta)")
		quit(1)
		return

	_ok("PhantomCameraHost e PCam_Main continuam processando durante o pause")

	# Prova real: com o mundo congelado, mover o Player (set direto) ainda move a câmera.
	var player_pausa := level.get_node_or_null("Player") as Node3D
	if player_pausa == null:
		_fail("Player não encontrado")
		quit(1)
		return

	player_pausa.global_position = Vector3(-6.0, 0.1, -3.0)
	for _i in FRAMES_FOLLOW:
		await process_frame
		await physics_frame

	var dist_pausa := cam.global_position.distance_to(player_pausa.global_position)
	if dist_pausa > MAX_DIST:
		_fail("câmera não seguiu dentro do pause (dist=%.2f > %.2f) — órbita morta com o inventário aberto" % [dist_pausa, MAX_DIST])
		quit(1)
		return

	_ok("câmera seguiu o Player durante o pause (dist=%.2f)" % dist_pausa)

	# --- Fechar ---
	inv.call("Fechar")
	await process_frame

	if paused:
		_fail("Fechar() não despausou a árvore")
		quit(1)
		return

	_ok("Fechar() despausou a árvore")

	if int(cam.process_mode) != modo_cam_antes or int(host.process_mode) != modo_host_antes or int(pcam.process_mode) != modo_pcam_antes:
		_fail("ProcessMode não restaurado (cam %d->%d, host %d->%d, pcam %d->%d)" % [
			modo_cam_antes, int(cam.process_mode),
			modo_host_antes, int(host.process_mode),
			modo_pcam_antes, int(pcam.process_mode)])
		quit(1)
		return

	_ok("ProcessMode originais restaurados")

	for _i in 10:
		await process_frame

	if probe.ticks == ticks_antes:
		_fail("mundo não voltou a processar depois de Fechar()")
		quit(1)
		return

	_ok("mundo retomou depois de Fechar()")

	# --- Órbita viva depois do ciclo abrir/fechar ---
	var player := level.get_node_or_null("Player") as Node3D
	if player == null:
		_fail("Player não encontrado")
		quit(1)
		return

	player.global_position = Vector3(10.0, 0.1, 4.0)
	for _i in FRAMES_FOLLOW:
		await process_frame

	var dist := cam.global_position.distance_to(player.global_position)
	if dist > MAX_DIST:
		_fail("câmera não acompanhou o Player após abrir/fechar (dist=%.2f > %.2f)" % [dist, MAX_DIST])
		quit(1)
		return

	_ok("câmera acompanhou Player após o ciclo (dist=%.2f)" % dist)

	# --- Clicar para abrir/fechar de novo não pode deixar resíduo ---
	inv.call("Abrir")
	await process_frame
	inv.call("Fechar")
	await process_frame

	if paused:
		_fail("segundo ciclo deixou a árvore pausada")
		quit(1)
		return

	_ok("segundo ciclo abrir/fechar limpo")

	print("[inventory_pause_regression] PASS")
	quit(0 if not _failed else 1)
