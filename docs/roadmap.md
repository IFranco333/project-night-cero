# Roadmap — Noche Cero (vertical slice)

Sprint role: each sprint produces a demonstrable milestone and a retro in `docs/sprints/sprint-N.md`.
Definition of Done: tests green + CI green + demo runs + retro written.

## S0 — Foundations
Goal: solution builds, deterministic core skeleton tested, Godot opens a scene.
- [x] Solution: `Core`, `Ai`, `Content`, `Save`, `Camera`, `Runner`, `tests` + xUnit + CI (dotnet test, format)
- [x] ECS (entities, components, systems, deferred commands, event bus)
- [x] Deterministic RNG with named streams + golden tests
- [x] 3D world model: chunks, transforms
- [x] Headless sim runner + state hash
- [x] Player movement system (intent → transform) and third-person camera math
- [ ] Godot scene: ground, instanced boxes, third-person camera, WASD movement demo
- [ ] "Noche Cero" title screen
- [ ] Godot headless build job in CI
- [x] ADRs 0001–0003 accepted

Status detail and lessons: [`docs/sprints/sprint-0.md`](sprints/sprint-0.md).

## S1 — World, navigation, opening
- [x] Town generator (roads, buildings with interiors, props) from seed
- [x] Content: typed building catalog validated at boot (JSON modding later)
- [x] Nav graph + A* with string-pulled smoothing
- [x] Invariant tests: buildings never overlap, every door reachable, golden town hash per seed
- [ ] Camera visibility, explored-map fog
- [ ] Noche Cero opening scene (wake in bed) — needs the Godot editor

Status detail and lessons: [`docs/sprints/sprint-1.md`](sprints/sprint-1.md).

## S2 — Player, equipment, combat
- Player controller: third-person movement, first-person aim toggle
- Survival stats: hunger, thirst, stamina, wounds (cuts/fractures), bleeding, splints
- Inventory (weight, stacking, equip), loot containers, food heals
- Weapons: melee (axe/sword/bat/pipe), bow with arrow recovery, firearms by tier
- HUD and minimal menus

## S3 — Threats and stealth
- Zombie AI: perception, investigate/chase/attack, horde alerting
- Hostile community AI: patrols, cover, flanking, traps, ambush, retreat, reinforcements, shared memory
- Stealth: crouch, noise, cover, darkness
- Catacomb (3 levels) with ghost and darkness
- Balance simulation reports

## S4 — Vehicles, progression, atmosphere
- One drivable car (fuel, battery, collision, damage)
- XP/skills (aim, stealth, survival, strength); day counter
- Day/night cycle and ambience
- Hospital interior + medical loot; hostile district layout
- Win/lose flow and first full night

## S5 — Persistence & hardening
- Save/load versioned + migrations + checksum
- Save fuzzing, schema validation, threat model
- Determinism replay harness, performance budgets
- README with GIF, CHANGELOG, release `v1.0.0`

## Post-slice (vision backlog)
2–3 km² regions, several communities and reputation, vampires, more vehicles, crafting, full skill tree, FPS-only mode. See `docs/vision.md`.
