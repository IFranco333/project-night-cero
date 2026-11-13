# Vision — Noche Cero (long-term north star)

This is the full game we *want* to build. It is not a delivery promise. The deliverable is the **vertical slice** described at the bottom; the vision guides architecture so the slice can grow into it without rewrites.

## Product statement
*Noche Cero* — a post-apocalyptic survival RPG. The apocalypse begins the night you wake up to the screams of your city: by morning, most people are infected and the survivors have closed ranks into hostile communities. Explore a living world of towns, hospitals, forests and underground catacombs; survive zombies, ghosts and people; fight with melee weapons, bows and firearms; scavenge, heal wounds, drive vehicles, and decide which side of the last night you are on.

## Desired features (full vision)
| Area | Features |
|---|---|
| Cameras | Third-person (default) with first-person aim; free camera |
| World | Large open world (target 2–3 km² per region, chunk-streamed): towns, villages, forests, roads, multi-level catacombs |
| Opening | **Noche Cero**: black screen, title, suspense sound; you wake in bed at night to screams and chaos; within minutes the city is infected |
| Threats | Zombies (individual + horde), ghosts in dark catacombs, possibly vampires, hostile human communities with advanced AI |
| Human communities | Occupied zones of the map, strategic and hostile: stealth, cover, traps, ambushes, retreat and reinforcements. Entering their territory turns the fight into an organized gun battle |
| Combat | Melee (axes, swords, bats, pipes, poles), bows and arrows, firearms with tiers (low-tier common, high-tier very rare, legendary at strongholds) |
| Stealth | Crouch, noise, cover, sight cones, light and darkness |
| Injuries | Cuts (bleed, bandage, sutures), fractures (splints, boards), infection; food restores health; hospital as the main medical loot site |
| Progression | XP, 4 skills in the slice growing to a full tree; days survived ("Noche 0" onward) |
| Items | Tiered loot tables, rarity, durability, crafting (post-slice), containers |
| Vehicles | Several car types, fuel, battery, damage, repair |
| Atmosphere | Day/night, weather, fear/morale, sound-driven tension |

## Architecture implications (decisions that must hold from day one)
- **Simulation is 3D and headless.** All gameplay logic lives in a deterministic, tick-based core independent of renderer and camera.
- **One perception/planning system for every agent.** Player, zombies and humans share perception primitives; behavior sets, parameters and shared memory differ. There is no separate "shooter mode": a hostile community is a location where organized human agents dominate.
- **Content is data-driven.** Items, loot tables, factions, skills, weapons, injuries and maps are declared as data with schemas.
- **Chunked world streaming from day one.** Entity ids and save format are chunk- and world-coordinate aware from the first save.
- **Save format is versioned from the first save.**

## Vertical slice (the actual deliverable)
One town (~1 km²) with outskirts forest, a hospital, a hostile community district, and one 3-level catacomb underneath.

| Vision feature | Slice version |
|---|---|
| Opening | Noche Cero intro scene: wake in bed, screams, chaos, tutorial disguised as first minutes |
| TPS + FPS | Third-person camera with first-person aim toggle |
| Large world | 1 km² hand-authored + generated town (architecture supports 2–3 km² in a later phase) |
| Hospital | Full hospital building as medical loot site |
| Hostile community | One occupied district: patrols, perception, traps, ambush, retreat, reinforcements |
| Catacombs with ghosts | One 3-level catacomb with one ghost type |
| Zombies | Full zombie AI (perception, noise, hordes) |
| Vampires | Deferred |
| Melee | Axes, swords, bats, pipes, poles with range/damage/durability/bleed |
| Bows | Bow with recoverable arrows |
| Firearms | 3–4 weapons across tiers: low-tier common, mid uncommon, high-tier very rare |
| Stealth | Crouch, noise, cover, vision cones, light/darkness |
| Injuries | Cuts, fractures, bleeding; bandages, splints/boards, sutures; food heals |
| XP & skills | 4 skills (aim, stealth, survival, strength) |
| Infinite loot | Tiered loot tables, rarity, durability, per-container seeded distribution |
| Vehicles | One drivable car with fuel and battery |

## Explicitly out of scope for the slice
Networking/multiplayer, 3 km² map with full hand-authored content, full animation rigging, dialogue trees, crafting trees, vampire faction, more than one vehicle type, more than one hostile community.
