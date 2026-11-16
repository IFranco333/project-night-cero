<div align="center">

[![CI](https://github.com/IFranco333/project-night-cero/actions/workflows/ci.yml/badge.svg)](https://github.com/IFranco333/project-night-cero/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Godot](https://img.shields.io/badge/Godot-4.x%20(.NET)-478CBF)
![License: MIT](https://img.shields.io/badge/license-MIT-green)
![coverage](https://img.shields.io/badge/coverage-88%25-brightgreen)

# Noche Cero

**A post-apocalyptic survival RPG — you wake up on the night the city falls.**

Survive the infected, the darkness below, and the people who locked themselves in.

[Vision](docs/vision.md) · [Design](docs/design.md) · [Architecture](docs/architecture.md) · [Roadmap](docs/roadmap.md) · [Decisions](docs/adr/)

</div>

---

## Table of contents

- [Overview](#overview)
- [The game](#the-game)
  - [Premise](#premise)
  - [Gameplay pillars](#gameplay-pillars)
  - [Systems](#systems)
- [Current status](#current-status)
- [Engineering](#engineering)
  - [Why this architecture](#why-this-architecture)
  - [Architecture overview](#architecture-overview)
  - [Project layout](#project-layout)
  - [Determinism](#determinism)
  - [Testing strategy](#testing-strategy)
  - [Development process](#development-process)
- [Getting started](#getting-started)
  - [Quick start](#quick-start)
  - [Prerequisites](#prerequisites)
  - [Build and test](#build-and-test)
  - [Run the game](#run-the-game)
- [Project status](#project-status)
- [Documentation](#documentation)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

**Noche Cero** ("Night Zero") is a single-player survival RPG set in a small city on the first night of a zombie apocalypse. The game is built around three ideas: **information is power**, **every action costs something**, and **the world is a simulation** — foes, resources, and consequences emerge from interacting systems rather than scripted set pieces.

The project is also a software engineering exercise in building a real-time 3D game whose simulation is fully deterministic and testable without a graphics engine.

> **Prototype notice — the art is placeholder.** Every mesh in the current build is an untextured box. There are no models, textures, animations, characters or audio yet, and the town is generated to exercise the simulation, not to look like the final game. The final game will have full 3D art; nothing you can run today is the intended look.

## The game

### Premise

The screen fades in from black. You are in your bed. Outside, the city is screaming.

Within minutes, most of the city is infected. A few people have barricaded themselves into a district and will shoot anyone who comes too close. Beneath the church, something older waits in the dark.

Every night begins the same way and ends differently. Your job is to make the next morning happen.

### Gameplay pillars

1. **Information tension.** You cannot see the whole town. Walls cut sight, darkness hides threats, and every sound tells someone where you are.
2. **Every action costs.** Sprinting burns stamina and makes noise. Shooting attracts hordes. Turning on a light keeps the ghost away but reveals you. There are no free moves.
3. **Emergent simulation.** Gunshots pull zombies toward the sound, a wounded guard calls reinforcements, a car is fast and loud. Systems interact; outcomes are not scripted.
4. **Deterministic and legible.** The same seed and the same inputs always produce the same game, so bugs can be reproduced exactly and behavior can be tested.

### Systems

The table describes the full vertical slice as designed. Only the world generation and the
presentation layer exist today; the rest is scheduled — see [Project status](#project-status).

| System | Description |
|---|---|
| **Cameras** | Third-person by default; smooth first-person aim for precise shooting |
| **World** | A procedurally generated town (~1 km²) of houses, mansions, shops, warehouses, garages, a church, a police station and a hospital, all with interiors, plus parks, woods and empty lots. Houses and mansions can have cellars, and one house hides the entrance to a 3-level catacomb |
| **Zombies** | Individual behavior plus horde alerting: vision cones, line of sight, hearing, investigation, chase, attack |
| **Hostile community** | A human district with advanced AI: patrol routes, cover, flanking, traps, ambushes, retreat, reinforcements and shared memory. There is no separate "combat mode" — entering their territory in force is simply where organized humans dominate |
| **Stealth** | Crouching, cover, noise generation and darkness interact with all agents' perception |
| **Melee and ranged combat** | Axes, swords, bats, pipes and poles with reach, damage, durability and bleeding; bows with recoverable arrows; firearms in tiers, from common low-tier pistols to very rare high-tier rifles |
| **Injuries** | Cuts (bleeding, bandages, sutures) and fractures (splints); food restores health; hospitals are the main source of medical supplies |
| **Progression** | XP and skills (aim, stealth, survival, strength), a day counter starting at "Night 0" |
| **Items and loot** | Data-driven, tiered loot with rarity and durability, generated per container from the world seed |
| **Vehicles** | A drivable car with fuel, battery and damage |
| **Atmosphere** | Night sky with drifting cloud, stars and a moon; selectable lighting moods (night, overcast, dusk); day/night cycle, stealth-relevant lighting and darkness-driven threats |

The full long-term scope — larger regions, more communities, new threats, crafting, a complete skill tree — is described in the [vision document](docs/vision.md). The [design document](docs/design.md) describes the vertical slice under construction.

## Current status

> **World stage (S1).** The engine-free core is in place: a deterministic fixed-tick loop, an entity–component world with deferred structural changes, seeded RNG with golden tests, a state hash, a headless CLI runner, player movement and third-person camera math. World generation produces a seeded town — a road grid, blocks that are built, park, wood or empty, buildings with interiors and landmarks, cellars and a catacomb entrance, and static props — plus a navigation graph with A* and path smoothing. The Godot layer opens on the title screen, then draws that town with instanced meshes under a procedural night sky and lets you walk it in third or first person. CI also builds the Godot project on Linux without the editor. **There is no gameplay yet**: no threats, loot or survival stats. The title screen is a placeholder — a main menu with settings and character creation is planned — and *Noche Cero* / *Night Zero* is a working title that may change.

The project is developed in the open, with every feature shipped together with its tests and documentation.

## Engineering

### Why this architecture

A survival game is a system of systems: movement, perception, combat, injury, inventory, vehicles and a world. Keeping that logic inside a game engine's scene graph makes it hard to test, hard to reproduce bugs, and hard to change the camera or rendering later.

Noche Cero separates the two:

- **The simulation is a plain C# library.** No engine types, no wall clock, no global randomness. It runs headless in tests and in batch simulations.
- **The engine only presents.** The Godot layer reads the simulation state and turns input into intents. It never mutates the simulation.

This means gameplay bugs are reproduced with a seed and a list of inputs — not by clicking around in the editor.

### Architecture overview

```
┌───────────────────────────────────────────────┐
│ Game layer — Godot 4 (.NET)                    │
│ scenes · camera · input · UI · meshes · audio │
└───────────────┬───────────────────▲───────────┘
                │ intents           │ state / events
┌───────────────▼───────────────────┴───────────┐
│ Core — deterministic simulation (no Godot)    │
│ chunks · ECS · systems · 3D transforms · RNG  │
└────────┬─────────────────┬────────────────┬───┘
         │                 │                │
   ┌─────▼─────┐    ┌──────▼──────┐  ┌──────▼──────┐
   │    Ai     │    │   Content   │  │    Save     │
   │ perception│    │ weapons,    │  │ versioning, │
   │ planning  │    │ loot, rules │  │ migrations  │
   └───────────┘    └─────────────┘  └─────────────┘
```

- A single perception and planning system serves every agent. Zombies, hostile humans and the player share perception primitives; what differs is the parameters, the available behaviors and whether memory is shared with a squad.
- The world is split into 32 m chunks, with navigation graphs per level and portals (stairs) linking them.
- Gameplay runs on fixed 100 ms ticks; rendering interpolates between them.

More detail, including tick order and module rules, is in the [architecture document](docs/architecture.md).

### Project layout

```
Project-NightCero.sln
├── src/
│   ├── Core/       Deterministic 3D simulation — no Godot
│   ├── Ai/         Shared perception and planning — no Godot
│   ├── Content/    Data-driven game definitions — no Godot
│   ├── Save/       Versioned serialization and migrations — no Godot
│   ├── Camera/     Third-person camera math — no Godot
│   ├── Runner/     Headless CLI: run a seed, print the state hash
│   └── Game/       Godot project: scenes, camera, input, UI, rendering
├── tests/
│   └── Project-NightCero.Tests/   xUnit test suite for the simulation
└── docs/           Architecture decisions, design, roadmap, setup
```

The core projects **must not reference Godot**. CI enforces this rule on every change.

### Determinism

- All randomness comes from named RNG streams derived from the save seed (`"loot"`, `"zombie-ai"`, …), so adding a system never shifts another system's random sequence.
- Iteration over entities follows entity id order; there is no reliance on hash-map or insertion order.
- State hashes quantize floats to a fixed precision, and tests assert on hashes and invariants rather than raw bits.
- Replays are first-class: a seed plus a recorded input log reproduces a session exactly.

See [ADR 0002](docs/adr/0002-deterministic-core.md).

### Testing strategy

```
            e2e (game boots, input, no errors)
          integration (systems together, fixture worlds)
        unit · property-based · replay hashes · save fuzzing
```

- **Unit and integration tests** cover perception, navigation, combat, injuries, inventory, loot and save round-trips.
- **Property-based tests** assert invariants (paths are contiguous and unobstructed, inventories never exceed capacity, save→load→save is lossless).
- **Replay tests** compare the state hash of a seeded session against a golden value.
- **Golden world tests** pin the generated town for a seed, and invariant tests assert that buildings never overlap, every entrance sits on its wall and every room is reachable from the road network.
- **Save fuzzing** ensures corrupted or malicious save files fail cleanly with typed errors.
- **Balance simulations** run batches of seeded games headless to compare survival statistics between versions.

On every push and pull request, CI runs the Godot-free guard on the core projects and, on Linux and Windows, the formatting check and the test suite. The Godot build job is added in the first milestone that includes the Godot project.

### Development process

- Short-lived branches (`feature/…`, `fix/…`, `chore/…`) merged into `main` through a pull request; `main` always stays green and milestones are marked with tags.
- **Conventional Commits**, atomic and small, each with its tests and documentation.
- **Test-driven development** for simulation logic: a failing test first, the minimal implementation second, a refactor third.
- **Architecture Decision Records** in [`docs/adr/`](docs/adr/) capture every decision that is expensive to reverse.
- The [design document](docs/design.md) and the code are updated in the same change.

## Getting started

### Quick start

```bash
git clone https://github.com/IFranco333/project-night-cero.git
cd project-night-cero
dotnet test Project-NightCero.sln    # build the simulation and run its test suite
```

Run the deterministic headless simulation (no engine required):

```bash
dotnet run --project src/Runner -- --seed 2026 --ticks 250 --bodies 8
# seed     2026
# ticks    250
# entities 8
# hash     696c8fbb980683a3
```

The same seed and tick count always print the same hash — that is the determinism guarantee in action.

Generate the town for a seed and route between two building entrances:

```bash
dotnet run --project src/Runner -- --seed 2026 --ticks 0 --town --nav
# town     654 buildings, 1659 props, 34 roads
# blocks   Built 191, Park 24, Forest 21, Empty 20
# kinds    House 335, Mansion 51, Shop 128, Warehouse 48, Garage 89, Church 1, Hospital 1, Police 1
# features 1193 trees in 21 woods, 124 basements, catacomb entrance under Mansion #303
# townhash 77ed45815b40cd5d
# nav      3453 nodes, 1921 on roads
# path     109 nodes, 18 waypoints, 1720.3 m
```

Measure tick throughput (timing lives in the runner, not the deterministic core):

```bash
dotnet run --project src/Runner -- --seed 2026 --ticks 20000 --bodies 1000 --bench
# ticks/s ~439, ms/tick ~2.28 on the development machine
```

To run the game, open `src/Game/project.godot` with the Godot 4 **.NET** editor. The editor is only needed to run scenes: the C# layer builds with `dotnet build src/Game/NightCero.Game.csproj`, which is what CI does. Expect boxes — see the prototype notice above.

### Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 | Build and test the simulation |
| [Godot](https://godotengine.org/download) | 4.x, **.NET edition** | Run and edit the game |

### Build and test

```bash
dotnet restore Project-NightCero.sln
dotnet build Project-NightCero.sln
dotnet test Project-NightCero.sln
```

Formatting:

```bash
dotnet format Project-NightCero.sln --verify-no-changes
```

### Run the game

The Godot project lives in `src/Game/`. Open `src/Game/project.godot` with the Godot 4 **.NET**
editor (the editor is only needed to run scenes; `dotnet build src/Game/NightCero.Game.csproj`
compiles the C# layer without it). The game opens on the title screen and starts the town on any
key. Current status of the game layer: [`src/Game/README.md`](src/Game/README.md).

## Project status

| Milestone | Goal | Status |
|---|---|---|
| **S0 — Foundations** | Solution, ECS, deterministic RNG, headless runner, player movement, camera math, Godot scene shell, title screen | ✅ done |
| **S1 — World, navigation, opening** | Town generator, nav graph, visibility, opening scene | 🟡 in progress (town generation, content catalog, navigation and the rendered town are done; camera visibility and the wake-in-bed opening remain) |
| **S2 — Player, equipment, combat** | Movement, cameras, survival stats, injuries, weapons, inventory, HUD | ⬜ planned |
| **S3 — Threats and stealth** | Zombie AI, hostile community AI, stealth, catacomb | ⬜ planned |
| **S4 — Vehicles, progression, atmosphere** | Car, XP/skills, day counter, hospital, first full night | ⬜ planned |
| **S5 — Persistence and hardening** | Versioned saves, fuzzing, replays, performance, `v1.0.0` | ⬜ planned |

Details and acceptance criteria for each milestone: [`docs/roadmap.md`](docs/roadmap.md).

## Documentation

| Document | Description |
|---|---|
| [Vision](docs/vision.md) | The long-term game and its features |
| [Design](docs/design.md) | The vertical slice: pillars, loop, balance, systems |
| [Architecture](docs/architecture.md) | Modules, tick order, determinism and world rules |
| [Art direction](docs/art-direction.md) | Palette, lighting presets and the procedural sky |
| [Roadmap](docs/roadmap.md) | Milestones from foundation to release |
| [ADRs](docs/adr/) | Architecture decision records |
| [Contributing](CONTRIBUTING.md) | Workflow, standards and definition of done |
| [Changelog](CHANGELOG.md) | Release history |
| [Security](docs/SECURITY.md) | Threat model and vulnerability reporting |
| [Tech debt](docs/tech-debt.md) | Known debt and planned paydowns |
| [Setup](docs/setup.md) | Local environment setup |

## Roadmap

The milestone roadmap is in [`docs/roadmap.md`](docs/roadmap.md). It is organized as serial milestones — foundations, world and navigation, player and combat, threats and stealth, vehicles and progression, persistence and hardening — each ending in a playable, tested build.

A player-facing roadmap with dates and release notes will be published as the playable slice comes together.

## Contributing

The project is currently maintained by a single developer. The workflow, coding standards and definition of done are described in [`CONTRIBUTING.md`](CONTRIBUTING.md). Issue reports that reproduce a bug via seed and input log are especially welcome.

## License

Released under the MIT License. See [LICENSE](LICENSE) for details.
