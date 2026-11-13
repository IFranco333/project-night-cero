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
- `Core`, `Ai`, `Content`, `Save` are **plain .NET projects** (target `net8.0`, no Godot references).
- `Game` is the only project referencing GodotSharp; it depends on Core.
- CI verifies the golden rule above.

## Solution layout
```
Project-NightCero.sln
src/
  Core/       Core.csproj        deterministic simulation (ECS, chunks, RNG, systems, balance)
  Ai/         Ai.csproj          perception and planning shared by every agent
  Content/    Content.csproj     data-driven definitions (items, loot, weapons, factions, injuries)
  Save/       Save.csproj        serialization, versioning, migrations, validation
  Game/       NightCero.Game.csproj  Godot project glue (C#), scenes under Game/
tests/
  Project-NightCero.Tests/       xUnit tests for Core/Ai/Content/Save
```

## World and coordinates
- Right-handed 3D: X east, Y up, Z south (Godot convention). Units: metres.
- World split into **chunks** (32×32 m). Entity position world-space; chunk index = floor(x/32), floor(z/32).
- Navigation on a 2.5D height field with per-level nav graphs; catacomb levels connected by portals (stairs).
- Static geometry generated from content + seed; Godot renders with MultiMesh instancing.

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
- Third-person camera by default; first-person aim mode. Camera smoothing; collision-aware spring arm.
- MultiMesh for props and zombies; merged static meshes per building; visibility ranges for LOD.
- One directional light with shadow range limited around the camera; night lighting presets.

## Headless mode
A console entry point in `Core` (or a `dotnet run` harness) boots the simulation without Godot for tests, replay and balance statistics.
