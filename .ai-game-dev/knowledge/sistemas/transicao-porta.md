# Transição de porta (estilo RE2)

Perto da porta aparece o aviso, a tecla de interagir escurece a tela e o jogador surge do outro lado, já virado para dentro do cômodo. Sem animar a folha da porta.

## Por que tela escura em vez de animação

O modelo da porta é um rip de PS1 com `AnimationPlayer` vazio — não existe abrir/fechar pronto (ver [Modelo ripado que chega explodido](/ferramentas/modelo-ripado.md)). A tela preta é a solução que o próprio RE2 usava e resolve dois problemas de uma vez: esconde o teleporte e esconde a câmera atravessando a parede para alcançar o jogador.

## Como está montado

`scripts/DoorTransition.cs`, anexado ao nó `PortaTransicao`:

```
PortaTransicao (Node)          ← script
├── Fade (CanvasLayer)
│   └── Black (ColorRect)      tela cheia, preto, alpha 0
├── GatilhoFrente (Area3D)     sala principal → manda para o fundo
├── GatilhoFundo (Area3D)      sala do fundo → manda para a frente
└── Pontos
    ├── Frente (3.5, 0.1, -4.6)
    └── Fundo  (3.5, 0.1, -8.0)
```

A porta também tem colisão própria (`PortaColisor`, box 1,15 × 2,10 × 0,10), então é sólida — o jogador encosta nela, não atravessa.

Sequência: fade out 0,35 s → teleporte → 0,55 s de tela preta (esconde a câmera alcançando) → fade in 0,45 s.

## Botão de ação

Não é gatilho automático: os `Area3D` só marcam presença e mostram o aviso no HUD (`[E]  Abrir porta`). A ação `interact` (E / botão Y) é que dispara. Isso elimina o efeito sanfona de vez — andar contra a porta não abre nada.

## Sistema de chave-cartão (ponto de extensão)

`RequiredKeyId` vazio = destrancada. Para trancar:

1. Preencha `RequiredKeyId` no Inspector (ex.: `cartao_seguranca`).
2. Coloque um nó no grupo `inventory` com o método `has_key(id) -> bool`.

`KeyHolderHasCard()` procura esse nó via `GetFirstNodeInGroup("inventory")`. **Sem inventário na cena, porta trancada não abre** — falha fechada, que é o comportamento seguro.

Consequência de design: o aviso mostra "Trancada — precisa de cartão" em vez de sumir, então o jogador sabe que existe algo faltando.

## As duas armadilhas

### 1. Chegar virado para a porta causa ping-pong infinito

Primeira versão teleportava o jogador e pronto. Resultado, medido no log: ciclo infinito indo e voltando.

```
DISPARO Fundo  | player em (3.5, 0, -4.97) | vel (0, 0, -6)
DISPARO Frente | player em (3.5, 0, -7.53) | vel (0, 0, +6)
```

`vel 6` é o próprio `MoveSpeed`: o jogador continuava andando, porque o movimento é **relativo à câmera** e a câmera apontava de volta para a porta. Corrigido em `FaceIntoRoom()`: ao chegar, a rotação da câmera virtual é ajustada para o jogador encarar o interior do cômodo (yaw 0 indo para o fundo, 180 vindo para a frente).

**Regra:** qualquer teleporte num jogo com movimento relativo à câmera precisa reorientar a câmera, senão o jogador volta pelo mesmo caminho.

### 2. O gatilho precisa desarmar até o jogador sair

`body_entered` dispara uma vez por entrada, mas se o jogador for reposicionado dentro de outro gatilho ele volta a disparar. O script usa `_armed`: desarma ao disparar e só rearma quando `GetOverlappingBodies()` dos dois gatilhos volta a zero.

## Detalhe do material

O azulejo usa projeção triplanar, e `heightmap` (displacement) **não funciona com triplanar** — o Godot ignora e avisa no log a cada execução. `heightmap_enabled` foi desligado no `Tiles101_1K-JPG.tres` para calar o aviso.
