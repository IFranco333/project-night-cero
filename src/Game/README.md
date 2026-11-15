# Game layer (Godot)

This directory will contain the Godot 4 (.NET) project: scenes, the camera rig, input mapping, UI, meshes and audio.

**Status:** the project file (`NightCero.Game.csproj`) exists so the Godot build is wired into the solution layout. The scene, input map and `project.godot` are generated with the Godot 4 **.NET** editor; until then there is nothing to open here. `NightCero.Game` is intentionally kept out of `Project-NightCero.sln` so the engine-free test build does not require the Godot SDK.

## Rules for this layer

- This is the **only** project that may reference Godot. `Core`, `Ai`, `Content`, `Save`, `Camera` and `Runner` must stay engine-free (CI enforces this).
- The game layer reads simulation state and emits **intents**. It never mutates the simulation directly.
- No gameplay rules live here: balance values, loot tables and behaviors belong to the core and `Content`.

See the [architecture document](../../docs/architecture.md) and [ADR 0001](../../docs/adr/0001-godot-csharp.md).
