# Sprint 1 — World, navigation, opening

**Goal:** a seeded town, a content catalog and a navigation graph, all engine-free and testable.
Status: in progress — world generation, content and navigation are done; visibility and the Godot
opening scene remain.

## Delivered

### S1-1 — Ground-plane geometry
- `Rect2`: axis-aligned rectangles on the XZ plane with `Contains`, `Intersects`, `Expand` and
  Liang–Barsky segment clipping (`IntersectsSegment`), which is what line-of-sight and path
  smoothing build on.
- `Hashing.Fnv1a64` extracted from the state hasher and shared with the town hash.

### S1-2 — Town generation
- `TownSettings` (block pitch, road width, margin), `TownPlan`, `Building`, `Room`, `Prop`,
  `RoadSegment`.
- `TownGenerator`: road grid, 2 × 2 building cells per block, weighted building kinds, parks and
  empty lots, entrances on the street side, interiors split into ~9 m rooms, streetlights, cars and
  trees.
- Named streams (`town/blocks`, `town/buildings`, `town/props`) and row-major block order make the
  plan reproducible; `TownPlan.Hash()` pins seed 2026.
- `TownSpawner` turns the plan into ECS entities (`Transform` + `BuildingTag`/`PropTag`).

### S1-3 — Content catalog
- `BuildingCatalog` in `NightCero.Content`: one definition per `BuildingKind`, with size and floor
  ranges, weighted selection and two unique landmarks (church and hospital).
- `BuildingCatalog.Validate` runs at boot and in tests; duplicate, missing or inverted definitions
  fail with a typed exception.

### S1-4 — Navigation
- `NavGraph`: ordered nodes, bidirectional weighted edges, `NearestNode`, `IndicesOfKind`.
- `NavGraphBuilder.FromTown`: crossings, road points every ~16 m, one node per door and per room.
- `AStar.FindPath`: Euclidean heuristic, deterministic tie-breaking by node id.
- `PathSmoother`: string pulling with a caller-supplied clear-line test; in the game it is
  `TownGeometry.IsSegmentClear`.
- Runner `--town` and `--nav` for inspecting generation and routing headlessly.

## Not delivered
- Camera visibility and explored-map fog.
- The "Noche Cero" opening scene, which needs the Godot 4 .NET editor.

## Lessons
- **Struct defaults are not parameter defaults.** `TownSettings.Default => new()` zero-initialised
  the struct and ignored the optional values; the runner caught it on the first run. The
  constructor is now written out explicitly.
- **The namespace shadows `System.Math`.** `NightCero.Core.Math` made bare `Math.Min` resolve to
  the namespace; the code now says `System.Math` where both are in scope.
- **Hashing must cover primitives.** Adding `BuildingTag` exposed that `StateHasher` ignored enums
  and integers; two towns differing only in building kind hashed the same. Fixed with a primitive
  branch and regression tests.
- **Assets should be data, not constants.** Passing the catalog into the generator keeps the core
  free of `NightCero.Content` and lets tests generate towns with a hand-written catalog.
