# Game layer (Godot)

The Godot 4 (.NET) project: scenes, the camera rig, input mapping, UI, meshes and audio.

**Status:** the project opens on a title screen over the drifting town; any key starts the game. It
generates the town from the seed, draws it with instanced boxes and lets you walk around in third
or first person, under a procedural night sky with clouds, stars and a moon. Art is grey-box on
purpose — this layer exists to see and tune the world, not to be final. Every colour lives in
`Rendering/Palette.cs` and every lighting preset in `Rendering/Mood.cs`, so the look is retuned in
one place.

## Run it

```bash
# Build the assembly first; the editor is not required for this step.
dotnet build src/Game/NightCero.Game.csproj

# From the repository root, with the Godot 4.3 .NET editor:
Godot_v4.3-stable_mono_win64.exe --path src/Game
```

On a weak GPU (integrated graphics, old laptop), add `-- --no-shadows`; shadows are the dominant
cost. The renderer defaults to the Compatibility (OpenGL 3.3) backend for the same reason.

## Controls

| Input | Action |
|---|---|
| any key / click | leave the title screen and start |
| W A S D | move (relative to the camera) |
| Shift | sprint (6 m/s vs 3.5 m/s) |
| Right mouse | aim (third person: shoulder camera; first person: tighter FOV) |
| V | switch third / first person, at any time |
| M | cycle the lighting preset (night → overcast → dusk) |
| H | hide the debug line |
| Esc | release the mouse (click to capture it again) |

## Command line

Arguments after `--` go to the game and not to the engine:

| Argument | Effect |
|---|---|
| `--seed=1234` | generate a different town |
| `--mood=night\|overcast\|dusk` | start under a lighting preset (default `night`) |
| `--no-shadows` | disable shadows (much faster on weak GPUs) |
| `--screenshot=path.png` | render a few frames, save a PNG and quit — used to check the town headlessly; starts in the town |
| `--title` | keep the title screen up (with `--screenshot`, captures the title instead of the town) |
| `--yaw=45` `--pitch=35` `--distance=30` | start the camera at a fixed orbit, for inspection shots |

## Rules for this layer

- This is the **only** project that may reference Godot. `Core`, `Ai`, `Content`, `Save`, `Camera` and
  `Runner` must stay engine-free (CI enforces this).
- The game layer reads simulation state and emits **intents**. It never mutates the simulation
  directly.
- No gameplay rules live here: balance values, loot tables and behaviors belong to the core and
  `Content`.

## Layout

```
project.godot            engine config, input map, renderer selection
Scenes/Main.tscn         single root node with the GameRoot script
Scenes/GameRoot.cs       generates the town, owns the Simulation, drives the fixed tick
Scenes/TitleScreen.cs    the title over the drifting town; any key starts the game
Rendering/WorldRenderer.cs  TownPlan -> MultiMesh instances (ground, roads, buildings, props)
Rendering/Palette.cs     every colour in the scene, in one place
Rendering/Mood.cs        lighting presets: ambient, fog, key light, moon direction
Rendering/SkyLook.cs     sky parameters -> Sky resource
Rendering/NightSky.gdshader  procedural sky: gradient, cloud, stars, moon
Player/PlayerController.cs  input -> MoveIntent, and the third/first person camera
```

See the [architecture document](../../docs/architecture.md) and [ADR 0001](../../docs/adr/0001-godot-csharp.md).
