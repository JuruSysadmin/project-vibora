# Inventory pause (mundo congelado)

Abrir o inventário com Tab congela o mundo, como no Resident Evil clássico: inimigos, dano e timers param. A tela do inventário e a câmera continuam vivas, para a órbita da third-person não morrer atrás do menu.

Implementado em `ui/InventarioTela.cs` com `GetTree().Paused` + whitelist de `ProcessMode`.

## A crença errada que existia antes

O código tinha um comentário explícito — "Não usa `GetTree().Paused` — isso quebrava o PhantomCamera (órbita fixa)" — e o bug `Inventario-pause-quebra-PhantomCamera` documentava isso como fato. **Estava errado.** O que quebrava a câmera era pausar a árvore sem colocar os nós da câmera em `ProcessMode.Always`: eles pausavam junto, o host parava de atualizar a transform e a câmera ficava plantada.

Com a whitelist, a PCam segue normalmente durante o pause. O teste `./tests/run_inventory_pause.sh` prova isso: move o Player com o mundo pausado e confere que a câmera acompanha.

**Regra:** num pause global, quem precisa continuar reagindo (câmera, UI, áudio de interface) tem que estar `Always` — isso não é motivo para abandonar o pause global.

## A ordem importa

```
PausarMundo():
  1. ManterVivo(no) em cada nó da whitelist   (salva ProcessMode original, seta Always)
  2. GetTree().Paused = true

DespausarMundo():
  1. GetTree().Paused = false
  2. restaurar o ProcessMode original de cada nó
```

Marcar `Always` depois do pause não adianta: o nó já parou de processar. E ao fechar, restaurar o modo antes de despausar deixa o nó com o modo errado por um frame.

## Whitelist

| Nó | Por quê |
|---|---|
| `InventarioTela` | recebe `_UnhandledInput` e chama `Abrir`/`Fechar` |
| `MainCamera` | raiz da pilha de câmera |
| `MainCamera/PhantomCameraHost` | é ele que aplica a transform na câmera |
| `PCam_Main` | a lógica de follow/órbita |

`MainCamera` sozinha não basta: é uma `Camera3D` sem script, não tem `_process` próprio. Quem processa de fato é o host (`addons/phantom_camera/scripts/phantom_camera_host/phantom_camera_host.gd:850` e `:856`) e a PCam (`addons/phantom_camera/scripts/phantom_camera/phantom_camera_3d.gd:955`). Daí `cam.is_processing()` ser `false` mesmo com o jogo rodando — o teste confere o host e a PCam, não a câmera.

Os caminhos caem num fallback relativo (`../MainCamera`, `../PCam_Main`) porque `InventarioTela` é irmã delas no root do `level_01`. Os três são `[Export]` para o caso de a tela migrar para outro prefab.

## Armadilhas

### Não fazer early-return por inimigo

A alternativa tentadora é pausar a IA dos inimigos um por um. Vira lista infinita: depois dos inimigos vêm timers, portas, partículas, áudio. O pause de árvore congela o mundo de uma vez; a whitelist é curta e finita.

### Sair da árvore com o mundo pausado congela o jogo

A pausa pertence ao `SceneTree`, não ao nó. Se a `InventarioTela` for removida da cena com o inventário aberto, nada despausa. `_ExitTree()` chama `DespausarMundo()` por isso.

### O host da PCam pode sobrescrever o modo

`PhantomCameraHost` reseta o próprio `process_mode` para `DISABLED`/`INHERIT` quando `interpolation_mode == MANUAL` (`phantom_camera_host.gd:1438`). Se um dia alguém ligar o modo MANUAL, a whitelist é desfeita. Fora desse modo, não há conflito.

### Chamada direta atravessa o pause, `_Process` não

`InventarioTela` é `Always`, então consumir um frasco durante o pause funciona: `TentarConsumirFrasco()` e `RestaurarVida()` são chamadas diretas de método. Já o `_Process` dos nós `Inventario`/`Saude` está congelado — efeito colateral desejado (regeneração, invulnerabilidade e timers param junto).

## Decisões de escopo

- **Whitelist mínima:** áudio ambiente (`Ambiente` no root do level) e HUD de vida pausam junto. Escolha consciente — o RE clássico também cala o ambiente no inventário. Se um dia entrar música de inventário, esse nó precisa ser `Always` ou tocar num player próprio.
- **`Abrir()` continua com early-return** quando o actor está com `InputLocked` — inventário não abre durante a sequência de morte.

## Teste

`./tests/run_inventory_pause.sh` (headless, roda `dotnet build` antes). O teste fixa o viewport em 1152×648 — em headless o padrão é 64×64 e o painel de 720 px nunca caberia, o que tornaria as checagens de layout um falso positivo.

1. a `Raiz` preenche a tela e o painel cabe nela, centralizado
2. probe fora da whitelist processa com o jogo rodando e **para** durante o pause
3. `Abrir()` pausa a árvore e deixa host + PCam em `Always`
4. câmera **segue** o Player com o mundo pausado
5. `Fechar()` despausa, restaura os `ProcessMode` originais e o mundo volta a processar
6. segundo ciclo abrir/fechar não deixa resíduo

## Layout: âncora no inicializador de objeto não pega

Aconteceu de verdade: o painel abria cortado no canto superior esquerdo, com texto ilegível sobreposto.

```csharp
// ERRADO — a âncora não é aplicada: a Raiz fica 0×0
_raiz = new Control { AnchorsPreset = (int)Control.LayoutPreset.FullRect };
AddChild(_raiz);

// CERTO — entra na árvore primeiro, depois aplica
AddChild(_raiz);
_raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
```

Com a `Raiz` 0×0, o `PanelContainer` centralizado por `SetAnchorsAndOffsetsPreset(Center)` calcula o centro num pai sem tamanho e cai em coordenadas negativas — sobra na tela só o quadrante inferior direito dele.

**Regra:** qualquer preset de âncora/offset só vale depois do `AddChild`. Vale para `AnchorsPreset`, `SetAnchorsAndOffsetsPreset` e para o preset de posição.

Cuidado correlato: `InventarioTela` é `CanvasLayer`, e o primeiro filho dela é o `AudioStreamPlayer`, não a UI. Busque a `Raiz` por nome (`get_node_or_null("Raiz")`), não por índice.

## Ver também

- [Transição de porta](/sistemas/transicao-porta.md) — outro sistema que mexe em câmera e teleporte
- [Dano e vida](/sistemas/dano-e-vida.md) — o que o pause congela
