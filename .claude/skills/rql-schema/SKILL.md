---
name: rql-schema
description: List RQL sources, installed extensions and the node classes in the edited scene; pass an extension name for its guide or a type name for its members and Godot properties.
metadata:
  generated-by: godot-mcp
  ai-game-dev: exclude
---

# RQL / Schema

Without a type: RQL sources, functions, the installed extensions (Particles, Tilemap …), and the node classes present in the edited scene (with counts), plus the Node members. With an extension name: its guide with examples. With a type name (e.g. "Node3D", "OmniLight3D", "StandardMaterial3D", "Vector3", "editor"): that type's members (name type; ro = read-only), methods, and its Godot properties (props["name"]). Use it before querying an unfamiliar class.

Without a type: RQL sources, functions, the installed extensions (Particles, Tilemap …), and the node classes present in the edited scene (with counts), plus the Node members. With an extension name: its guide with examples. With a type name (e.g. "Node3D", "OmniLight3D", "StandardMaterial3D", "Vector3", "editor"): that type's members (name type; ro = read-only), methods, and its Godot properties (props["name"]). Use it before querying an unfamiliar class.

## How to Call

Run from the project's root directory:

```bash
godot-cli run-tool rql-schema --input '{
  "type": "string_value"
}'
```

For complex input, create a JSON file instead:
```bash
printf '%s\n' '{
  "type": "string_value"
}' > args.json
godot-cli run-tool rql-schema --input-file args.json
```

> CLI installation and usage: see the `godot-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `type` | `string` | No | Optional type name, e.g. "Node3D", "OmniLight3D", "StandardMaterial3D", "Vector3", "editor" |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "type": {
      "type": "string",
      "description": "Optional type name, e.g. \"Node3D\", \"OmniLight3D\", \"StandardMaterial3D\", \"Vector3\", \"editor\""
    }
  }
}
```

## Output

This tool does not return structured output.

