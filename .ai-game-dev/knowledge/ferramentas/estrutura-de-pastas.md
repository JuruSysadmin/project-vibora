# Estrutura de pastas

Convenção do projeto, inspirada na organização de uma engine de estúdio grande.

```
res://
├── sistemas/     código reaproveitável (equivale a "Common")
├── gameplay/     regras do jogo (equivale a "Game")
├── art/          conteúdo por categoria (equivale a "DvdRoot/Data")
│   ├── ambientes/  cenário
│   ├── props/      objetos
│   ├── personagens/
│   └── materiais/  materiais simples compartilhados
├── scenes/       cenas montadas
└── addons/       plugins de terceiros
```

## Regras

- **`sistemas/` nunca referencia `gameplay/`.** É o que permite extrair os sistemas como addon depois. `DoorTransition` é sistema; `Player` e `InimigoIA` são gameplay.
- **`art/` agrupa por categoria de asset, não por extensão.** Um pack (modelo + texturas + material) fica junto na sua pasta, porque as referências internas são relativas. Materiais simples e compartilhados ficam em `art/materiais/`.
- **Crescer por feature, não por arquivo-monólito.** Um script por arquétipo (ex.: uma IA por comportamento), como o original fazia com um diretório por chefe.

## Mover arquivos sem quebrar a cena

Os recursos são referenciados por **UID**, então mover arquivos não quebra a cena — mas há pegadinhas reais, aprendidas na prática:

1. **Feche o editor antes de mover.** Com ele aberto existe a janela em que a cena salva um caminho que já não existe.
2. **`git mv` com vários arquivos aborta tudo se um não estiver versionado.** Arquivos novos (não commitados ainda) precisam de `mv` simples.
3. **Arquivo movido por `mv` pode não remapear o UID.** Foi o caso do `InimigoIA.cs`: o nó continuou apontando para o caminho antigo. Reanexe com `script-attach-to-node`.
4. **`.tres` movido pode manter o caminho velho na cena** mesmo com o editor enxergando o arquivo novo. Reatribua a referência (`MaterialOverride` etc.) apontando para o novo caminho; é o que reescreve o `ext_resource`.

Verificação depois de qualquer mudança de caminho: `files | where missingDeps.count() > 0` deve voltar vazio, e a cena não deve ter `res://` apontando para pastas que não existem mais.
