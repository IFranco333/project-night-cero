# Setup — Noche Cero

How to build, test and run the project on any development machine.

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 | Build and test the engine-free simulation |
| [Godot](https://godotengine.org/download) | 4.x, **.NET edition** | Open and run the game layer |

## Install (Windows, winget)

```powershell
winget install -e --id Microsoft.DotNet.SDK.8 --source winget
winget install -e --id GodotEngine.GodotEngine.Mono --source winget
```

## Build and test

```bash
dotnet restore Project-NightCero.sln
dotnet test Project-NightCero.sln --configuration Release
dotnet format Project-NightCero.sln --verify-no-changes
```

## Run the headless simulation

```bash
dotnet run --project src/Runner -- --seed 2026 --ticks 250 --bodies 8
```

The command prints a state hash that is identical on every machine for the same seed — the
determinism guarantee can be checked from a shell.

## Game layer

The Godot project lives in `src/Game/`. Once `project.godot` exists, open it with the Godot 4
**.NET** editor. On limited graphics hardware, switch Project Settings → Rendering → Renderer to
**Compatibility (OpenGL)**.

## Local secrets

Machine-specific settings and credentials stay out of the repository.
