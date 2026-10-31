# Modelo ripado que chega "explodido"

Sintoma e correção para `.fbx` de jogo antigo (PS1) que importa com as partes do corpo espalhadas pelo espaço.

## Sintoma

O `PL0A.fbx` (personagem do RE2) entrou no Godot com a malha **desmontada**: pernas, braços e sapatos flutuando separados, ocupando um AABB de 5,66 × 30,37 × 11,38 unidades.

Não é artefato de render isolado — foi confirmado na cena real, com a câmera de jogo.

## O que NÃO era o problema

Tudo parecia certo, e é isso que engana:

| Verificação | Resultado |
|---|---|
| `MeshInstance3D.skin` | ✅ definido (`Skin`, 15 binds) |
| `Skeleton3D` | ✅ 15 ossos, hierarquia coerente (quadril em Y=18, pé em Y=1,6) |
| `AnimationPlayer` | ✅ 10 animações, `Anim0` com 16 tracks e 0,97 s |

Skin ligado, esqueleto são, animação real — e mesmo assim o mesh renderiza solto.

## A correção: round-trip pelo Blender

O importador de FBX do Godot não reconstrói esse bind. O do Blender **reconstrói**:

```bash
blender --background --python fbx_to_glb.py
```

O script importa o FBX e exporta GLB:

```python
import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=".../PL0A.fbx")
bpy.ops.export_scene.gltf(filepath=".../PL0A.glb", export_format='GLB', export_apply=False)
```

No Blender o resultado vem íntegro: `ARMATURE` (15 bones) + `MESH` com modificador `ARMATURE` e 15 vertex groups. O `.glb` importa no Godot **montado**.

## Detalhes que mordem depois

- **As 10 animações sobrevivem**, mas são renomeadas para o padrão glTF: `Armature|Anim0|Base`, `Armature|Anim1|Base`, …
- **O mesh sai deslocado em Y**: AABB local começa em Y=15,7. Sem corrigir a posição do nó, o personagem fica flutuando acima do teto e parece que "não importou".
- **Escala**: o modelo tem ~30 unidades de altura. `scale = 0.058` dá ~1,75 m — coerente com a porta do mesmo pack (`scale = 0.053` → 2,10 m). Assets do mesmo rip compartilham a unidade arbitrária.
- O `.fbx` original pode ficar no projeto como fonte; o `.glb` é o que se usa.

Ver [Formatos de modelo 3D](/ferramentas/modelos-3d.md) para o comportamento normal de cada formato.

## As animações vêm corrompidas — no arquivo de origem

As 10 animações (`Anim0`–`Anim9`) sobrevivem à conversão, têm keyframes de verdade (`Anim0` frames 1-30, `Anim2` 1-113, etc.) e tocam. Mas **desmontam o personagem**.

**Não é problema do Godot nem da conversão.** Verificado renderizando os frames **dentro do Blender**, no FBX original:

| Frame renderizado no Blender | Resultado |
|---|---|
| repouso | perna direita desprendida, flutuando |
| `Anim6` @f12 | boneco desmontado e caído |
| `Anim0` @f15 | braços e pernas separados do tronco |

Também medido pelo bounding box da malha deformada: as ações variam entre 10 × 11 × 30 e 25 × 31 × 31 unidades, sem padrão coerente.

**Causa:** o formato de origem do RE2 guarda cada membro como objeto separado, com transform próprio. O `.fbx` foi exportado com as peças nos transform antigos e as animações no mesmo espaço, mas o esqueleto foi reconstruído em outro referencial. Ossos e keyframes existem, só não conversam.

**Não há conserto por configuração de importação.** As saídas são reautorar os tracks no esqueleto reconstruído, ou animar por código no Godot.

### O que funciona

- A **malha em repouso** no Godot, depois do round-trip pelo Blender, fica montada e serve como personagem estático.
- A pose de repouso não é neutra: é um frame capturado, com o tronco levemente torcido e um braço flexionado.
- Para um jogador animado de verdade, o caminho realista é um modelo com rig sadio (Mixamo ou similar), não este rip.
