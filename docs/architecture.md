# Architecture — Noche Cero

## Principle: deterministic 3D simulation, separate presentation
The game state is a **3D world simulated headlessly**. Godot renders it and turns input into intents; it never mutates the simulation. All gameplay logic is in plain C# assemblies with no reference to Godot.

```
        +---------------------------------------+
        |  Game (Godot project, C# glue)        |
        |  scenes · nodes · Camera3D · input   |
        |  UI · audio · meshes · animation     |
        +----------+----------------+-----------+
                   | intents        | observes state/events
        +----------v----------------v-----------+
        |  Core (C# class library, no Godot)    |
        |  world chunks · ECS · systems · RNG   |
        |  3D transforms · balance · clock      |
        +----+-------------+-------------+------+
             |             |             |
       +-----v----+  +-----v-----+  +----v-----+
       |   Ai     |  |  Content  |  |   Save   |
       +----------+  +-----------+  +----------+
```
- `Core`, `Ai`, `Content`, `Save`, `Camera` and `Runner` are **plain .NET projects** (target `net8.0`, no Godot references).
- `Game` is the only project referencing GodotSharp; it depends on Core.
- CI verifies the golden rule above.

## Solution layout
```
Project-NightCero.sln
src/
  Core/       NightCero.Core.csproj    deterministic simulation (ECS, chunks, RNG, systems, balance)
  Ai/         NightCero.Ai.csproj      perception and planning shared by every agent
  Content/    NightCero.Content.csproj data-driven definitions (items, loot, weapons, factions, injuries)
  Save/       NightCero.Save.csproj    serialization, versioning, migrations, validation
  Camera/     NightCero.Camera.csproj  third-person camera math (pure, engine-free)
  Runner/     NightCero.Runner.csproj  headless CLI: run a seed, print the state hash
  Game/       NightCero.Game.csproj    Godot project glue (C#), scenes under Game/ — not in the .sln
tests/
  Project-NightCero.Tests/             xUnit tests for every engine-free project
```

`NightCero.Game` is deliberately excluded from the solution: it targets the Godot SDK, which
needs a Godot project directory to generate its glue, so keeping it out lets `dotnet build` and
`dotnet test` run on any machine with only the .NET SDK.

## World and coordinates
- Right-handed 3D: X east, Y up, Z south (Godot convention). Units: metres.
- World split into **chunks** (32×32 m). Entity position world-space; chunk index = floor(x/32), floor(z/32).
- Navigation on a 2.5D height field with per-level nav graphs; catacomb levels connected by portals (stairs).
- Static geometry generated from content + seed; Godot renders with MultiMesh instancing.

## World generation
`NightCero.Core.WorldGen` builds the static town from the seed, with no engine and no content dependency:
- `TownSettings` describes the grid (block pitch, road width, building margin); the defaults are a
  1024 × 1024 m town of 16 × 16 blocks.
- `TownGenerator` fills each block with up to four buildings drawn from the `BuildingDefinition`
  records it receives as a catalog. `NightCero.Content.BuildingCatalog` is the game's catalog; the
  core only knows the shape, so a test can pass a hand-written one.
- Every random draw comes from the named streams `town/blocks`, `town/buildings` and `town/props`,
  and blocks are visited in row-major order, so the plan is reproducible (ADR 0002).
- Each building gets an entrance on the wall facing the nearest street and an interior split into
  rooms of roughly 9 m.
- `TownPlan.CanonicalText()` and `Hash()` make a whole town comparable; the golden test pins the
  town for seed 2026.
- `TownSpawner` turns the plan into entities (`Transform` plus `BuildingTag` / `PropTag`), so the
  static world is part of the state hash.

## Navigation
`NightCero.Core.Navigation` turns a plan into a weighted graph:
- nodes at road crossings, points along the roads, one per building entrance and one per room;
- `AStar.FindPath` uses Euclidean distance and breaks ties by node id, so the route is reproducible;
- `PathSmoother` drops nodes that a clear straight line can skip, testing each shortcut with
  `TownGeometry.IsSegmentClear` against the building footprints.

Keeping routes as node ids rather than editor-placed markers is what makes chase and patrol
behaviour testable when the agents arrive in S3.

## Tick and time
- Fixed step 100 ms (10 Hz) for gameplay; render interpolates at display rate.
- Fixed system order; structural changes (spawn/despawn/components) deferred to end of tick via command buffer.
- RNG: named streams derived from the save seed; adding a system never shifts another system's sequence.

## Agent intelligence (one system)
- **Shared perception**: vision cone + line of sight + hearing radius; produces perceived facts.
- **Shared planning**: a planner chooses among behaviors available to the agent's profile.
- **Profiles** differ: zombie (individual, instinct), human (squad memory, cover, flanking, traps, retreat), player (input intents).
- There is **no "combat mode"**: a hostile community is a location where human agents with richer profiles dominate.

## Content
- `Content/` definitions in C# (typed records) validated at boot; later loaded from JSON for modding.
- Categories: weapons (tiers, rarity), loot tables, injuries/healing items, factions, map layouts, balance constants.

## Save format
- JSON `{ version, meta: { seed, tick, day, createdAt, checksum }, state }`.
- Checksum FNV-1a over canonical state; migrations are versioned functions; future versions rejected cleanly.

## Rendering (Godot)
The Godot project lives in `src/Game` and is deliberately thin: it reads a `TownPlan` and a
`Simulation`, and draws them. It contains no gameplay rules.

- `Scenes/GameRoot.cs` generates the town for the seed, spawns it into the simulation, owns the
  fixed-tick accumulator and drives the player. It is the only place that knows both layers.
- `Rendering/WorldRenderer.cs` turns the plan into instanced boxes: one `MultiMeshInstance3D` per
  material (ground, roads, road markings, block slabs, park paths, buildings, doors, windows,
  hatches, trees, cars, streetlights, benches, dumpsters). A 654-building town is a handful of draw
  calls, and swapping boxes for prefab scenes later does not touch the generator.
- Block slabs come from `TownPlan.Blocks`: a built block is a grass interior plus a 2 m sidewalk
  ring (the 15 cm step is the curb), a park is grass with a path across it, a wood is a darker floor
  under dense conifers, an empty lot is bare ground. Doors are drawn from `Building.Entrance`, which
  the generator already places on the wall that faces the street. Windows and cellar hatches are
  derived from the same footprints, so a building never has a window in open air.
- `Rendering/Palette.cs` is the only place with a literal colour; the renderer reads every colour
  from it. `Rendering/Mood.cs` holds the lighting presets (night, overcast, dusk): ambient, fog and
  a key light whose direction is also the moon direction. `Rendering/SkyLook.cs` plus
  `NightSky.gdshader` draw the sky procedurally — gradient, drifting fbm cloud, hashed stars and a
  moon — so the look costs no assets and stays tweakable in code. Colours and mood are chosen at
  runtime: `--mood=` on the command line, `M` in game.
- `Player/PlayerController.cs` converts input into a `MoveIntent` and applies the pose from
  `ThirdPersonRig` to a `Camera3D`. Both camera modes are fully playable: third person walks and
  aims in third person, first person walks and aims in first person, and `V` switches at any time.
- The renderer defaults to the **Compatibility** (OpenGL 3.3) backend so it runs on integrated and
  older GPUs; `forward_plus` is a one-line change in `project.godot` for machines with Vulkan.
- Shadows are the dominant cost on weak GPUs; `--no-shadows` disables them for inspection runs.

Still to come: camera collision, roof fading when the player is indoors, LOD and chunk streaming
for the 3 km² target, and prefab meshes replacing the grey boxes.

## Headless mode
`NightCero.Runner` is a console harness that boots the simulation without Godot:

```bash
dotnet run --project src/Runner -- --seed 2026 --ticks 250 --bodies 8 [--state]
```

It prints the seed, tick count, entity count and the `StateHasher` hash (and, with `--state`, the
canonical state text). The same seed and tick count always print the same hash — the determinism
guarantee made checkable from a shell and in CI.

`--town` generates the town for the seed and spawns it instead of demo bodies, printing the town
hash; `--nav` also builds the navigation graph and reports a route between two building entrances.
Both are headless and reproducible, so world generation is inspected without opening Godot.

`--bench` measures tick throughput. Timing lives in the runner, never in the deterministic core, so
the printed hash still proves the run is reproducible.

Replays use `InputLog` (Core): a recorded intent per tick. `Simulation.Run(InputLog)` replays it
tick by tick, and with the same seed it reaches the same state hash — the property `ReplayTests`
pins down.

## Presentation math
`NightCero.Camera` holds the third-person rig as pure math (`ThirdPersonRig.Evaluate` →
`CameraPose`), with no Godot types. The Godot layer feeds it the player transform and mouse yaw/pitch
and applies the resulting pose to a `Camera3D`, so camera behaviour is unit-tested like the sim.
