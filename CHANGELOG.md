# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Project documentation: vision, vertical-slice design, architecture, roadmap and ADRs.
- Engineering process: evaluation-facing README, contributing guide.
- .NET solution skeleton with engine-free `Core`, `Ai`, `Content` and `Save` projects and an xUnit test project.
- CI: Godot-free guard on the core, formatting check and tests on Linux and Windows.
- Deterministic RNG (`RngStream`, SplitMix64 with named streams) and golden-value tests.
- Entity–component world with deferred structural changes, ordered iteration and an event bus.
- Fixed-tick `Simulation`, canonical `StateHasher` and a headless `NightCero.Runner` CLI.
- Player movement system (walk 3.5 m/s, sprint 6.0 m/s) and engine-free third-person camera math
  (`NightCero.Camera`).
- Deterministic replay: `InputLog` records one intent per tick and `Simulation.Run(InputLog)`
  reproduces the session hash-for-hash.
- `--bench` mode in the runner for tick-throughput measurements.
- CI reports line coverage in the job summary; the README shows a coverage badge.
- Sprint 0 retro in `docs/sprints/sprint-0.md`.
- Town generation (`NightCero.Core.WorldGen`): seeded road grid, buildings with entrances and
  interiors, and static props. `TownSpawner` adds them to the ECS, so the town is part of the state
  hash; `TownPlan.Hash()` pins a whole town per seed.
- Building catalog in `NightCero.Content`, validated at boot.
- Navigation (`NightCero.Core.Navigation`): graph builder, A* with node-id tie-breaking and
  string-pulled path smoothing against building footprints.
- `Rect2` ground-plane geometry and a shared `Hashing.Fnv1a64`.
- `StateHasher` now covers enums, booleans, strings and integer components.
- Runner `--town` and `--nav` modes for inspecting a generated town and a route headlessly.
- Godot project (`src/Game`): `project.godot`, `Main.tscn`, input map and a `GameRoot` that
  generates the town, owns the fixed-tick `Simulation` and drives the player.
- `WorldRenderer` draws a `TownPlan` with one `MultiMeshInstance3D` per material, so a 724-building
  town costs a handful of draw calls.
- `PlayerController` with two fully playable camera modes (third person and first person, each with
  its own aim behaviour) and a `V` toggle at any time.
- `--seed`, `--no-shadows`, `--screenshot`, `--yaw`, `--pitch` and `--distance` arguments for
  inspecting the town without a human watching.
- Town blocks (`Block`, `BlockKind`): the plan now records whether each block is built, a park or an
  empty lot, and `CanonicalText` covers them. The renderer draws grass, bare ground, a 2 m sidewalk
  ring with a curb, park paths and building doors from that data.

### Changed

- Project named **Noche Cero**; stack defined as Godot 4 (.NET) with a deterministic, headless simulation core.
- Solution file uses the classic `.sln` format; the locked .NET 8 SDK cannot load `.slnx`.
- README documents the real project status per milestone (see the status table).
- `InternalsVisibleTo` names the real test assembly, so tests can build small graphs and worlds.

## [0.0.0] - 2026-11-11

### Added

- Initial repository: process documentation, project skills and CI skeleton.
