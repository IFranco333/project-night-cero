# Setup notes — Noche Cero

Internal, machine-specific notes. Not part of the product documentation shown in the README.

## Developer machines

| Machine | Notes |
|---|---|
| Dell Latitude 5490 (16 GB RAM, Intel integrated graphics) | Primary start. Godot 4 must run with the **Compatibility** (OpenGL) renderer; expect slow editor performance and no heavy scenes. Good enough for Core work and light scenes. |
| Desktop with NVIDIA GTX 1060 | Full Godot editing and play-testing of scenes with lighting. Primary machine once available. |

## Installing the toolchain (Windows, winget)

```powershell
winget install -e --id Microsoft.DotNet.SDK.8 --source winget
winget install -e --id GodotEngine.GodotEngine.Mono --source winget
```

## First run

1. `dotnet restore Project-NightCero.sln` then `dotnet test Project-NightCero.sln`.
2. The first S0 story generates `src/Game/project.godot`; open it in the Godot .NET editor after that.
3. In Project Settings → Rendering → Renderer, choose **Compatibility** on the Dell.

## Local secrets

Local tool configuration is kept out of the repository. Never commit credentials or machine-specific settings files.
