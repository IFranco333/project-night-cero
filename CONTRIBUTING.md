# Contributing to Noche Cero

Thanks for your interest. This document describes how work is organized, how to build and test locally, and the standards every change must meet.

## Before you start

Read, in this order:

1. [`README.md`](README.md) — what the project is.
2. [`docs/architecture.md`](docs/architecture.md) — how the code is organized and the rules that protect determinism.
3. [`docs/design.md`](docs/design.md) — the gameplay rules of the current slice.
4. [`docs/roadmap.md`](docs/roadmap.md) — the current milestone.

## Development setup

### Prerequisites

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 |
| [Godot](https://godotengine.org/download) | 4.x, **.NET edition** |

### Build and test

```bash
dotnet restore Project-NightCero.sln
dotnet build Project-NightCero.sln
dotnet test Project-NightCero.sln
dotnet format Project-NightCero.sln --verify-no-changes
```

Opening the game layer: `src/Game/README.md` explains its current status.

## Workflow

### Branches

| Prefix | Use |
|---|---|
| `feature/<milestone>-<n>-<slug>` | new work (e.g. `feature/S0-2-ecs`) |
| `fix/<slug>` | bug fixes |
| `chore/<slug>` | tooling, CI, dependencies |
| `docs/<slug>` | documentation and decisions |

Branch from `main`, keep branches short-lived, and merge them back through a pull request. `main` always stays green; milestones are marked with tags.

### Commits

**Conventional Commits**, one logical change per commit:

```
<type>(<scope>): <imperative summary>

Why this change is needed.
```

- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`, `ci`.
- Scopes: `core`, `ai`, `content`, `save`, `game`, `ci`, `docs`.
- Every commit that changes behavior includes its tests.
- Never commit with a red build or failing tests.

### Pull requests

Each pull request must state:

- **What** changed and **why**.
- Which acceptance criteria it fulfills.
- How to verify it (`dotnet test …`, a scene to open, a screenshot/GIF for visual changes).
- Risks or trade-offs.
- The documentation updated alongside the code.

Keep pull requests small (a few hundred lines at most). Large changes are split.

## Standards

### Determinism (non-negotiable)

- No `Random`, `DateTime.Now`, `Stopwatch` or `Godot.*` in `Core`, `Ai`, `Content` or `Save`.
- Randomness comes from named RNG streams of the world seed.
- Iteration order is explicit (entity ids, ordered collections).
- Any change to serialized state bumps the save version and ships a migration.

### Testing

- Test-driven development for simulation logic: failing test → minimal implementation → refactor.
- One test per acceptance criterion.
- Every bug fix starts with a regression test.
- New public behavior in the core needs unit tests; system interactions need integration tests.

### Documentation

- Update `docs/design.md`, `docs/architecture.md` or an ADR in the same change as the code.
- Record decisions that are expensive to reverse as ADRs in `docs/adr/` (see [ADR 0001](docs/adr/0001-godot-csharp.md) for the format).
- New shortcuts are logged in [`docs/tech-debt.md`](docs/tech-debt.md).

### Definition of done

- [ ] Acceptance criteria covered by tests.
- [ ] `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass.
- [ ] Documentation and ADRs updated.
- [ ] No Godot reference in the core projects.
- [ ] Tech debt recorded.

## Reporting issues

When reporting a bug, include:

- The seed and the input sequence (when the simulation is involved).
- The state hash or save file produced by the debug recorder.
- Expected vs. observed behavior and the steps to reproduce.

Deterministic reproduction is what turns a bug report into a fix.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
