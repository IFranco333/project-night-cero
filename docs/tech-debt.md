# Tech debt register

| ID | Date | Area | Description | Why deferred | Estimated fix | Trigger |
|---|---|---|---|---|---|---|
| TD-1 | 2026-11-15 | WorldGen | Interiors are a grid of rooms without doors, corridors or wall segments; the nav edge from a door to a room ignores walls. | Interiors are not walkable until the Godot layer renders them (S2). | Split rooms with a corridor graph and add doorway nodes. | First interior movement or loot container. |
| TD-2 | 2026-11-15 | Navigation | The graph has no spatial index: `NearestNode` and the door-to-road link scan every node, so building the full town is O(n·m). | The 1 km² town builds in well under a second; it is not on the tick path. | Bucket nodes into chunks and query the 3 × 3 neighbourhood. | Agents query the graph every tick, or the town grows past ~2 km². |

Rules: one item per accepted shortcut. Each sprint retro picks at most one to pay down. Never delete rows — mark as `paid`.
