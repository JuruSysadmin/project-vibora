# IA do inimigo

O segundo cilindro (vermelho) é controlado por `scripts/InimigoIA.cs`. É a base para aprender IA: uma máquina de estados de dois estados com pathfinding real.

## Estrutura

```
Inimigo (CharacterBody3D)  ← InimigoIA.cs
├── Collider (CollisionShape3D)   cápsula 0,4 × 1,8
├── Body (MeshInstance3D)         cápsula vermelha
└── NavigationAgent3D             caminho pela malha de navegação
```

E a navegação fica em `Navegacao (NavigationRegion3D)`, com a malha baked.

## A armadilha do bake: 0 polígonos

O primeiro `Bake()` devolveu **0**. O `NavigationRegion3D` só analisa **os próprios filhos** por padrão (`GeometrySourceGeometryMode = RootNodeChildren`), e toda a geometria do nível é *irmã* dele, não filha.

Correção sem reparentar nada: usar o modo por grupo.

```
nodes | where class == "StaticBody3D" | call AddToGroup("navegavel")
nodes | where name == "Navegacao" | set NavigationMesh.GeometrySourceGeometryMode = 1, NavigationMesh.GeometrySourceGroupName = "navegavel"
nodes | where name == "Navegacao" | call Bake()
```

Resultado: **43 polígonos**. Qualquer parede nova precisa estar no grupo `navegavel` e a malha precisa ser re-assada (`Bake()`), senão a IA não enxerga o caminho.

## A máquina de estados

| Estado | Quando entra | O que faz |
|---|---|---|
| `Ocioso` | jogador além de `RaioDeVisao` | freia e para |
| `Perseguindo` | jogador dentro de `RaioDeVisao` | segue o caminho do `NavigationAgent3D` |

`FolgaParaPerder` (1,2×) evita piscar entre estados quando o jogador fica no limite do raio.

Parâmetros exportados: `Velocidade` (3,5), `RaioDeVisao` (14), `DistanciaDeParada` (1,8), `GiroSuave` (10), `FolgaParaPerder` (1,2).

## Como estender

Para um novo estado (patrulhar, atacar, fugir): adicione no `enum Estado`, trate a transição em `AtualizarEstado()` e o comportamento no `_PhysicsProcess`. O agente de navegação não muda — ele só recebe `TargetPosition` e devolve `GetNextPathPosition()`.

## Detalhe de rotação

`alvoYaw = Atan2(-direcao.X, -direcao.Z)` porque a frente de um `Node3D` é **-Z**. É o mesmo cálculo do `Player.cs`.

## Medido em jogo

```
t=1  Perseguindo  dist=3,89   partiu de (6, 4)
t=2  Perseguindo  dist=1,63   alcançou e parou em DistanciaDeParada
t=2..8            dist=1,63   parado, sem empurrar o jogador
t=9+ Perseguindo  dist≈1,7-2,7 seguindo o jogador em movimento
```
