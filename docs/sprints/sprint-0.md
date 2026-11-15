# Sprint 0 — Foundations (retro)

**Goal:** a solution that builds, a deterministic engine-free core covered by tests, and a Godot
scene that opens. The core half is done; the Godot half is pending a machine with the Godot .NET
editor installed.

## Delivered

### S0-1 — Deterministic RNG
- `RngStream`: SplitMix64 with named streams derived from the world seed; `Fork(name)` for
  sub-streams (`"loot"`, `"zombie-ai"`, …).
- Golden-value tests pin the first five `ulong`s for seed 2026 / stream `"golden"`, so a change to
  the mixing function cannot pass silently.

### S0-2 — Entity–component world
- `World`: ordered entity ids, per-type component columns, per-instance state (no static columns),
  liveness bitset, deferred structural changes through `TickCommands`.
- Systems run in registration order; direct `Spawn`/`Despawn` during a tick throws, forcing every
  structural change through the command buffer.
- Event bus with base-type and interface delivery; `Unsubscribe` matches on method + target.
- Components must be value types (validated on attach).
- Seven regression tests were added after a review found ordering, lifetime and identity bugs.

### S0-3 — Fixed-tick simulation, state hash, headless runner
- `Simulation` runs a fixed 100 ms tick; `SimulationClock` is the single source of truth for the
  timestep.
- `StateHasher` produces a canonical text form (floats quantized to 4 decimals, entities by id,
  components by name) and an FNV-1a 64 hash that also folds in liveness.
- `NightCero.Runner` CLI: `--seed N --ticks N --bodies N [--state]`.
  Reproducible: seed 2026, 250 ticks, 8 bodies → `696c8fbb980683a3`.

### S0-4 — Player movement and third-person camera
- `MoveIntent` (input → intent), `PlayerMoveState` (the simulation's copy), `PlayerMovementSystem`
  (normalizes direction, walks at 3.5 m/s, sprints at 6.0 m/s, faces the movement direction).
- `Simulation.BindPlayer` / `Submit(intent)` / `LastIntent`.
- `NightCero.Camera.ThirdPersonRig`: pure orbit math, pitch clamped to [−30°, 70°], radius measured
  from the head so pitch never changes the distance.

### S0-5 — Deterministic replay and benchmark
- `InputLog` records one intent per tick; `Simulation.Run(InputLog)` replays it. `ReplayTests`
  proves that a recorded session and its replay reach the same state hash, and that a different
  seed or a different log diverges.
- `--bench` in the runner measures tick throughput (timing stays out of the deterministic core).

## Not delivered (needs Godot)
- Godot scene shell: ground, instanced boxes, `Camera3D` driven by `ThirdPersonRig`, WASD demo.
- "Noche Cero" title screen.
- Godot headless build job in CI.

These are tracked in `docs/roadmap.md` (S0) and require the Godot 4 **.NET** editor.

## Lessons
- **Review pays for itself.** A code review of the ECS found ten real defects (shared static columns,
  Set-then-Despawn leaks, missing world identity, command-order aborts, …) before any gameplay used
  it. They are now regression tests.
- **One source of truth for time.** A duplicated `0.1f` in the movement system was replaced by
  `SimulationClock.TickSeconds` derived from `TickDuration`; the two can no longer drift.
- **Tests caught an incoherent camera spec.** The rig's yaw convention and its `height` parameter
  contradicted the "pitch never changes distance" invariant; the tests exposed it and the model was
  simplified (orbit around the head, no separate height).
- **Keep the Godot project out of the solution.** The Godot SDK needs a project directory at
  generation time; excluding `NightCero.Game` lets the engine-free build and tests run anywhere.
