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
- Sprint 0 retro in `docs/sprints/sprint-0.md`.

### Changed

- Project named **Noche Cero**; stack defined as Godot 4 (.NET) with a deterministic, headless simulation core.
- Solution file uses the classic `.sln` format; the locked .NET 8 SDK cannot load `.slnx`.
- README documents the real project status per milestone (see the status table).

## [0.0.0] - 2026-11-11

### Added

- Initial repository: process documentation, project skills and CI skeleton.
