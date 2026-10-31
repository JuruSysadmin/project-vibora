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
