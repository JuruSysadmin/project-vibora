# Formatos de modelo 3D

Quais formatos o Godot importa neste projeto e o que cada um preserva.

## `.obj` — funciona nativo, testado

Importa sem depender do Blender instalado. Verificado no Godot 4.7: um cubo `.obj` entrou como recurso tipo `Mesh`, renderizou e o `.mtl` foi aplicado — o Godot gerou um `StandardMaterial3D` embutido (`res://cubo.obj::StandardMaterial3D_…`) com o `Kd` do material.

Limitações: é **mesh estático**. Não traz skinning, armature, animação, câmeras nem luzes. O material vem só do que o `.mtl` carrega (difusa/ambiente/especular), não é PBR.

## `.blend` — funciona, mas exige configuração

O Godot não lê `.blend` sozinho: chama o Blender em headless para converter em glTF. Depende de `Editor Settings → FileSystem → Import → Blender → Blender Path`.

O campo `filesystem/import/blender/blender_path` está **vazio** neste projeto (`~/.config/godot/editor_settings-4.7.tres`), mas isso **não impediu** o import: o `.blend` do pack Poly Haven em `materials/Tiles101_1K-JPG/` entrou como `PackedScene`. O Godot encontra o Blender instalado mesmo com o campo vazio (provavelmente via PATH) — na prática a configuração só é necessária quando a detecção automática falha ou há mais de uma versão. A setting é do editor, não do projeto: não vai pro git e o time não herda.

Blender instalado na máquina: 5.2.2 LTS (`/usr/bin/blender`).

## `.glb` / `.gltf` — o recomendado

Importa nativo, sem depender de nada instalado, e é o único que preserva hierarquia de nós, skinning e animação. Também é o mais previsível em time: o arquivo vai pro git e todo mundo vê igual.

Escolher `.blend` só vale quando se está iterando sozinho e quer editar → salvar → ver no jogo na hora.

## `.fbx` — importa nativo, mas sem as texturas

Testado com o armário elétrico da Poly Haven em `materials/armario/`: o `.fbx` entrou como `PackedScene` sem precisar do `fbx2gltf` (o Godot 4.3+ tem importador FBX embutido via ufbx).

**Pegadinha:** vem **sem material**. O `.fbx` de packs assim referencia texturas externas que não são embutidas, então o modelo aparece branco até alguém ligar as imagens na mão.

Padrão que funcionou: criar um `StandardMaterial3D` com

| Slot Godot | Textura do pack |
|---|---|
| `albedo_texture` | `_BaseColor` |
| `normal_texture` + `normal_enabled` | `_Normal` |
| `roughness_texture` + canal **Grayscale (4)** | `_Roughness` |
| `ao_texture` + `ao_enabled` + canal **Grayscale (4)** | `_AO` |

Ficam de fora `_Bump`, `_Cavity`, `_Gloss` e `_Specular`: o `StandardMaterial3D` não tem slot de specular, e `_Gloss` é o inverso da roughness.

O modelo também não traz colisão — é preciso um `StaticBody3D` + `CollisionShape3D` (box casando com o AABB do mesh resolve bem para props).
