# Art direction

How the town should read before any final asset exists, and where each decision lives in code.
The goal is that a grey-box does not look like a grey-box: shape, light and colour carry the mood
while the meshes are still boxes.

## Current state

The art is **placeholder**: every mesh is an untextured box, and there are no models, textures,
animations, characters or audio. This document describes the direction the final art will follow,
and the parts of it that already live in code (palette, lighting, sky); it is not a description of
the current build's fidelity. Do not judge the final look by a screenshot of this stage.

## Pillars

1. **Cold, quiet, empty.** The dominant colours are desaturated and cold. Warm light is scarce and
   therefore means something: a lit window, a fire, the objective.
2. **Readable silhouettes over detail.** Buildings are blocks with a door, windows and a roofline;
   what matters is that a player can tell a house from the hospital from a church at 60 m in the
   dark. Detail is added inside that silhouette, never instead of it.
3. **One accent, used sparingly.** A single cold violet-blue marks the night-side things — the
   catacomb hatch, and later the ghost's trace, interactable highlights and the objective. The
   references use a sickly green for this; we deliberately do not.
4. **Fog is composition.** Distance fog closes the horizon at roughly 250–400 m. It hides the edge
   of the generated town and makes the streets feel enclosed instead of gridded.

## Palette

All colours are constants in `src/Game/Rendering/Palette.cs`. Nothing else in the renderer holds a
literal colour, so a full retune is an edit to one file.

| Group | Family | Approximate value | Notes |
|---|---|---|---|
| Ground, roads, sidewalks | neutral dark grey | `#131415` – `#444` | Roads slightly bluer than the ground; markings a faded ochre. |
| Grass, woods, dirt | desaturated green-grey | `#182a18`, `#121b11`, `#282018` | Green enough to read as vegetation, dark enough not to glow at night. |
| House, mansion | warm grey-brown | `#554b41`, `#4d4540` | The only warm family; it is what makes homes feel lived in. |
| Shop, warehouse, garage | cold blue-grey | `#4b4e56`, `#414344` | Commercial buildings read colder than homes. |
| Church | pale stone | `#5f5a50` | The lightest wall in town except the hospital. |
| Hospital | clinical grey-white | `#6e7075` | Brightest building; the landmark you can find from across the map. |
| Police station | deep blue-grey | `#3c414d` | Institutional, darker than the hospital. |
| Windows | near-black with a few lit | `#0e0f13` / `#b8a89e` | Lit windows are a per-building hash, not random per frame. |
| **Accent** | **cold violet-blue** | **`#7a85f2`** | Catacomb hatch, later interaction and the ghost. Kept under emission energy 2 so it never clips to white. |

Rules:

- No saturated colours anywhere except the accent. Saturation is how the accent keeps its meaning.
- The palette is close to monochrome with a blue cast: the difference between two surfaces should
  be value first, hue second.
- Warm light (`#ffb86b`) is reserved for interiors and fire; it is the only counterweight to the blue.

## Lighting

Presets live in `src/Game/Rendering/Mood.cs`; `--mood=` picks one at launch and `M` cycles them.

| Preset | Use | Key light | Ambient | Fog |
|---|---|---|---|---|
| `night` | the default; the game is called Noche Cero | moon, cool blue, 0.85 | blue-grey 0.24/0.28/0.36 | `#12141f`, 0.0022 |
| `overcast` | flat grey daylight for judging geometry | white-grey, 0.55 | neutral grey | `#7a8087`, 0.0018 |
| `dusk` | the last warm light before dark | low orange, 0.75 | mauve | `#2e1f21`, 0.0026 |

The moon direction in `Mood` also drives the sky, so the brightest thing in the sky and the
shadow direction can never disagree. The key light is weak on purpose: night is lit mostly by
ambient, with a directional hint so boxes still have form.

## Sky

`src/Game/Rendering/NightSky.gdshader` draws the sky procedurally at any resolution, with no
texture assets: a horizon-to-zenith gradient, hashed stars with per-star twinkle, a domain-warped
fbm cloud deck drifting with time, and a moon disc with a halo. Parameters are in `SkyLook.cs`.

- Clouds must have soft, curled edges. A plain value-noise threshold produces square facets; the
  shader avoids this with five octaves, a rotation between them, and a wide smoothstep band.
- Stars fade out near the horizon, where fog takes over.
- Silver lining: the cloud picks up moon colour where it faces the moon, so the deck is lit rather
  than flat.

## Not decided yet

- Roof shape and materials; everything is a flat box top for now.
- Rain and snow (explicitly deferred; the sky and the mood system are built to host a weather pass).
- Interior dressing and the look of light through a doorway.
- Character and zombie silhouettes.

## How to change the look

1. Edit `Palette.cs` for colour, `Mood.cs` for lighting and fog, `SkyLook.cs` for the sky.
2. `dotnet build src/Game/NightCero.Game.csproj`.
3. Take screenshots from fixed viewpoints, one per mood:

   ```bash
   Godot_v4.3-stable_mono_win64_console.exe --path src/Game -- \
       --seed=2026 --mood=night --screenshot=.shots/night.png --yaw=45 --pitch=35 --distance=90
   ```

4. Compare against the previous shots. The same seed and the same camera arguments give the same
   frame, so the only variable is the change.
