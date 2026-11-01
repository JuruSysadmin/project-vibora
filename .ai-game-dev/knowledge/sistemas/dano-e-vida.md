# Dano e vida

Como o dano funciona hoje: contato direto, componente compartilhado, i-frames.

## O componente `Saude`

`sistemas/Saude.cs` — um componente para jogador **e** inimigos. O sistema de vida não sabe o que é um jogador (a mesma separação do "Common" do original).

Vida, i-frames, sinais `Ferido` / `Morreu` e o piscar do corpo. Quem morre decide o que fazer no sinal.

### Ordem de `_Ready` morde

Filhos ficam prontos **antes** do pai. O `_Ready` da `Saude` roda antes do `_Ready` do `InimigoIA`, então quando o arquétipo define a vida o componente já teria se inicializado com o padrão. Por isso existe `Configurar(maximo)`, que define máximo e atual juntos.

## Camadas de colisão

Antes tudo estava na camada 1, sem nome — a área de dano não distinguia jogador de parede.

| Camada | Nome |
|---|---|
| 1 | `mundo` |
| 2 | `jogador` |
| 3 | `inimigo` |
| 4 | `dano_jogador` |
| 5 | `dano_inimigo` |

Player: layer `jogador`, mask `mundo`+`inimigo`. Inimigos: layer `inimigo`, mask `mundo`+`jogador`. As camadas 4 e 5 estão reservadas para hitboxes com `Area3D` (ainda não usadas).

## Dano por contato

Com cápsulas, **distância é equivalente a sobreposição** e não custa física. O inimigo já calcula a distância para a IA e reaproveita:

```csharp
if (distancia <= AlcanceDeAtaque && _cooldownAtaque <= 0f)
    if (_saudeJogador.AplicarDano(DanoPorContato))
        _cooldownAtaque = IntervaloDeAtaque;
```

Padrões: `DanoPorContato` 10, `AlcanceDeAtaque` 2,2 m, `IntervaloDeAtaque` 1,0 s, `Invulnerabilidade` 0,5 s.

**Limitação conhecida:** ignora paredes. Na prática não acontece porque a malha de navegação impede o inimigo de chegar perto através de parede — mas é uma dependência implícita. A solução correta é hitbox com `Area3D` (camadas 4/5).

## Medido em jogo

```
t=0,5 vida 90   primeiro dano
t=1,0 vida 90   i-frame segurando
t=1,5 vida 80   novo dano
...
t=5,0 vida 10   [Player] morreu — recarregando a cena
```

## Pendências

- a rota de patrulha do `rota_sala1.tres` (retângulo −5..5 × −3..3) **contém o ponto de nascimento** do jogador em (0,0,0): ele é atacado assim que a cena abre. Mover a rota ou o spawn.
- morte do jogador ainda não tem HUD: recarrega a cena após 2,5 s
- falta ataque com telegrafia (hitbox com `Area3D`) — é o próximo passo do combate
