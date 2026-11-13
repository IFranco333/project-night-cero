# 0001. Godot 4 + C# como stack del juego, con el core fuera del motor

- **Fecha:** 2026-11-12
- **Estado:** aceptada

## Contexto
El proyecto es un RPG de supervivencia 3D nativo (descargable, sin navegador) con mundo, IA de zombis y humanos, armas, lesiones, vehículos y guardado. Se quiere un historial de Git limpio, código correcto y de calidad, y trade-offs bien razonados. El hardware de desarrollo inicial es modesto (Dell Latitude 5490, 16 GB; GPU dedicada GTX 1060 disponible más adelante).

## Decisión
Usar **Godot 4.x (.NET) + C#**. Toda la lógica de juego vive en un proyecto de **clases C# puras** (`Core/`, `Ai/`, `Content/`, `Save/`) sin referencias a Godot, determinista y testeada con **xUnit**. Godot se usa solo como **capa de presentación y conexión** (`Game/`: escenas, nodos, cámara, input, UI).

## Alternativas consideradas
| Alternativa | Pros | Contras | Por qué no |
|---|---|---|---|
| TypeScript + Three.js (navegador) | 100% testeable en Node; repo legible; sin instalación | no nativo; render 3D propio manual; GPU modesta sufre | usuario exige juego nativo descargable |
| Unreal 5 (C++) | gráficos y herramientas de mundo enorme | compila lento, C++ complejo, editor pesado, assets binarios (poco evaluable en Git) | riesgo de plazo y de "repo legible" |
| Godot + C++ (GDExtension) para todo el juego | rendimiento | bindings propios, compilaciones constantes, complejidad innecesaria | se reserva C++ solo para extensiones puntuales (ADR futuro) |
| Godot + GDScript | iteración rápida | sin tipado fuerte ni tests serios | C# es más profesional y testeable |

## Consecuencias
- **Positivas:** motor libre (MIT), exportación a .exe, física/navegación/animación importadas, repo legible (escenas y `.cs` son texto), juego testable sin abrir el editor.
- **Negativas / costes:** la ruta C# de Godot está algo por detrás de GDScript; requiere .NET SDK y la edición "Godot .NET".
- **Riesgos:** que la lógica se filtre hacia escenas/gdscript. Mitigación: verificación en CI de que `Core/` no referencia ensamblados de Godot, y la regla "el core no conoce el motor".

## Revisión
Se revisará si el rendimiento de un sistema crítico no cumple presupuesto (se evaluará GDExtension en C++) o si se requiere un mundo continuo mucho mayor que 3 km².
