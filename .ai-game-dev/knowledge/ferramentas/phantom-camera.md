# Phantom Camera

Instalação e funcionamento da Phantom Camera (ramokz/phantom-camera) no projeto, versão **v0.11.0.2**.

## Por que a extensão MCP sozinha não basta

A extensão `com.IvanMurzak.Godot.MCP.PhantomCamera` adiciona a fonte `phantomcamera` ao RQL, mas ela só funciona com o addon de terceiro instalado. A fonte reporta `installed = false` até o addon estar no disco **e** compilado.

O `PackageReference` no `.csproj` foi adicionado automaticamente, mas o restore só acontece num build — sem build a extensão não aparece.

## Passos que não são óbvios

1. **O addon v0.11.0.2 traz fontes C# junto dos `.gd`** (13 `.cs` em `addons/phantom_camera/`). Como estão em `res://`, o Godot.NET.Sdk compila todos. Por isso o hint da extensão termina em "then rebuild": sem `dotnet build` as classes não existem e a extensão continua `installed = false`.
2. **O autoload `PhantomCameraManager` é obrigatório.** O plugin registra ele no `_enable_plugin`, mas habilitar por `project.godot` não roda esse hook. Se for feito por fora, adicione o autoload na mão:

   ```ini
   [autoload]
   PhantomCameraManager="*res://addons/phantom_camera/scripts/managers/phantom_camera_manager.gd"
   ```

3. **O `_enable_plugin` do plugin chama `EditorInterface.restart_editor()`** — habilitar pela UI reinicia o editor. Vale saber antes de clicar.
4. **Todo câmera virtual precisa de um `PhantomCameraHost` filho da `Camera3D`** que ela vai dirigir. Sem host, a PCam não faz nada.

## Uso

A câmera de maior `priority` dirige o host. `follow_offset` é o offset em relação ao alvo.

```
root | call AddChild("PhantomCamera3D", "PCam_Main")
phantomcamera.cameras | where name == "PCam_Main" | call SetFollow("Simple", "Player")
phantomcamera.cameras | where name == "PCam_Main" | call SetLookAt("Simple", "Player")
phantomcamera.cameras | where name == "PCam_Main" | get node | set props["priority"] = 10, props["follow_offset"] = "(4, 2.5, 5)"
```

Os scripts são `@tool`: a câmera já é posicionada **no editor**, sem rodar o jogo — dá para conferir com `screenshot-camera`.

## Avisos em runtime

- "Using both Look At and Follow Mode on the same PCam3D has not been fully tested yet" — combinação é suportada mas não totalmente testada pelo autor.
- "Physics Interpolation is disabled" — ligar em `physics/common/physics_interpolation` suaviza o movimento. **Não foi ligado** (decisão pendente).

## Sem colisão de câmera — não serve para câmera de gameplay

Medido no jogo com um script que imprime a posição da câmera a cada segundo, player no canto da sala em (6, 0.1, 4):

| Configuração | Posição da câmera | Dentro da sala? |
|---|---|---|
| Follow `Simple`, offset (4, 2.5, 5) | (10.1, 2.6, 9.0) | ❌ atravessa a parede |
| Follow `ThirdPerson`, `spring_length` 4.5 | (10.1, 2.6, 13.5) | ❌ pior ainda |
| `SpringArm3D` nativo do Player | (5.81, 2.0, 5.99) | ✅ parou na parede |

O modo `ThirdPerson` instancia um `SpringArm3D` interno (`_follow_spring_arm`), mas na prática não segurou a câmera dentro do cômodo. Conclusão: **a PCam não substitui o SpringArm3D para câmera de gameplay em espaço fechado.**

Uso recomendado: PCam para câmera **cinematográfica/roteirizada** (intro, cutscene, troca de plano por `priority`), e o `SpringArm3D` do Player para a câmera de jogo. Hoje a PCam está na cena em `follow_mode = None`, inerte.
