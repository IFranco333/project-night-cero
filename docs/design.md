# Design — Noche Cero (vertical slice)

Living design document. Gameplay constants live in the engine-free core (for example `src/Core/Player/MoveIntent.cs`) and are mirrored here in the same commit. Full-game north star: `docs/vision.md`.

## One-liner
*Noche Cero*: you wake up to the screams of your city on the night the apocalypse begins. Survive the infected, the darkness underneath, and the people who locked themselves in.

## Pillars
1. **Information tension** — you don't see the whole town; exploring is risky (walls cut sight, fog, noise, night).
2. **Every action costs** — sprinting, shooting, looting and turning on a light consume time, stamina, noise and visibility.
3. **Emergent simulation** — gunshots attract hordes, a raided community returns with reinforcements, darkness wakes the ghost, a car is fast and loud.
4. **Deterministic and legible** — same seed + same inputs ⇒ same outcome (reproducible bugs, honest tests).

## Opening — "Noche Cero"
Black screen → white text "NOCHE CERO" → low suspense tone → fade into a dark bedroom. The player wakes in bed. Outside: screams, sirens, a flickering streetlight. Within the first in-game minutes, most of the city is infected; only the hostile community's district remains organized.

This is a **scripted intro that hands control back quickly**: pick up the flashlight, open the door, witness the chaos, lock the door. No long cinematic.

## Camera
Third-person by default (behind-the-shoulder, character visible). Hold aim → smooth transition to first-person aim with weapon model. Free orbit on mouse, zoom wheel. Aim in first person is the only time the player sees their weapon up close.

The orbit math is implemented engine-free in `src/Camera/ThirdPersonRig.cs`: yaw 0 faces north (−Z), yaw grows clockwise seen from above, pitch is clamped to [−30°, 70°], and the orbit radius is measured from the head point so pitch never changes the distance. The Godot layer only applies the resulting pose to a `Camera3D`.

## Core loop (slice)
Wake up in your house → gear up (flashlight, weapon) → explore the town and loot (food, meds, ammo, arrows) → manage hunger/thirst/stamina/wounds → avoid or fight hordes alerted by noise → recover medical supplies at the hospital → optionally raid the hostile community district → find the catacomb entrance → recover the objective → reach the safehouse and survive the night.

## Day counter
"**Noche 0**" is the first night. The counter increases each dawn: "Día 1", "Noche 1"… Difficulty from day count grows through spawn pressure and community readiness, not hidden scaling.

## Time model
1 tick = 100 ms simulation; day/night cycle of 20 real minutes. Night lowers visibility and activates the ghost and community aggression.

## Balance table (initial — tune via the balance simulation)
| Parameter | Value | Unit | Reason |
|---|---|---|---|
| Player walk / sprint | 3.5 / 6.0 | m/s | sprint ≈7 s |
| Sprint stamina drain | 15 | /s | |
| Player health | 100 | hp | |
| Food heal | 10 hp per item (20 hot) | hp | eating restores health | 
| Zombie speed (shamble / alert) | 0.8 / 2.2 | m/s | alert is a straight race lost by standing still |
| Zombie bite damage | 30 | hp | ~4 bites kill |
| Zombie sight / hearing | 14 m cone / 18 m gunshot | | |
| Human guard sight / hearing | 22 m cone / 20 m gunshot, FOV 120° | | humans see better |
| Human guard pistol damage / fire rate | 25 / 1.2 shots/s | | |
| Human trap (tripwire / noise can) | 15 dmg + slow / alerts squad | | |
| Ghost detect radius (darkness) | 8 | m | light protects |
| Pistol (low tier) damage / mag / reload | 25 / 8 / 2.0 | | |
| Rifle (high tier) damage / mag / reload | 55 / 5 / 2.8, very rare | | |
| Bow damage / arrow recovery | 50 / 60% | | |
| Melee: axe / bat / pipe | 45 / 30 / 25 damage, reach 1.6–2.2 m | | |
| Bleed rate | 0.4 hp/s until bandaged | | |
| Fracture: movement penalty | 50% speed until splinted | | |
| Carried weight capacity | 25 | kg | |

## Weapons — tiers and spawn rarity
| Tier | Examples | Rarity (containers) |
|---|---|---|
| Common | pipe, bat, kitchen knife, low-tier pistol, bow | 40% of weapons found |
| Uncommon | axe, sword, mid pistol | 25% |
| Rare | rifle, shotgun | 8% |
| Legendary | unique named firearms | <1%, only in hospital vault / strongholds |

Arrows and ammo are common; high-tier firearms are the scarcity that drives risk. Loot tables encode this and are data in `src/content/` (`Core/Content/`).

## Injuries and healing
- **Cut**: bleeding until bandaged; deep cuts need **sutures** and a hospital supply.
- **Fracture**: movement speed 50% until **splinted** (board); broken legs matter in chases.
- Items: bandage (common), board (tools), suture kit (rare), painkillers.
- Hospital: the concentrated medical loot site.

## Hostile community (integrated, no separate mode)
- One district of the town is controlled by a hostile group. There is **no mode switch**: crossing into it is normal play in a place where organized humans dominate.
- Behaviors: patrol routes, perception (vision cone, hearing), cover usage, flanking, **traps** (tripwires, alarm cans), **ambushes at chokepoints**, retreat when hurt, reinforcements and escalation after the player kills a guard.
- Shared memory: the squad knows the player's last known position and shares it.
- Choice: avoid the district, negotiate (post-slice), or assault it for high-tier loot.

## Zombies
Idle → wander → investigate noise → chase (LOS) → attack; horde alerting.

## Stealth
Crouch to reduce noise and detection; cover (walls, cars), darkness to hide; stealth skill slows the noise growth.

## Skills (slice)
| Skill | Raised by | Effect |
|---|---|---|
| Aim | ranged hits | tighter spread |
| Stealth | time unseen while crouching | slower noise growth |
| Survival | looting, eating, healing | better loot tier odds, better healing |
| Strength | melee and carrying | melee damage, capacity +1 kg/level |

## Loot
- Containers seeded per chunk; tiers: common, uncommon, rare, broken.
- Category generators (weapon, ammo, food, med, tool, clothing, junk) with stat rolls, durability, rarity.

## Fun criteria (headless-testable)
- A maxed guard squad takes cover, flanks and kills a straight-line rushing player in < 20 s in 90% of seeds.
- A trigger wire on the main road kills or slows the player in ≥ 70% of first attempts.
- A patient player who avoids gunfights and stays hidden survives the night in ≥ 60% of seeds.
- A player who takes the hospital in the first 10 minutes is dead or wounded in ≥ 50% of seeds.
