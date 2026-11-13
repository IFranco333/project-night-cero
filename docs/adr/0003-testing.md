# 0003. Estrategia de test: xUnit para el core + CI por capas

- **Fecha:** 2026-11-12
- **Estado:** aceptada

## Contexto
La corrección y la calidad del código son requisitos del proyecto. El core determinista (ADR 0002) permite pruebas rápidas y herméticas.

## Decisión
- **xUnit** para tests de `Core/`, `Ai/`, `Content/` y `Save/` (proyecto `tests/Project-NightCero.Tests`).
- Categorías: unitarios, integración con mundos de fixture, replays con seed fija (hash del estado), property-based (FsCheck) para invariantes, fuzz de guardados.
- **CI (GitHub Actions):** `dotnet test`, análisis de código (`dotnet format --verify-no-changes`), y build del proyecto Godot con Godot headless en la misma job cuando haya escenas.
- Guard: `Core`/`Ai`/`Content`/`Save` **no pueden referenciar** `GodotSharp` (se verifica en CI por listado de referencias).

## Alternativas consideradas
| Alternativa | Pros | Contras |
|---|---|---|
| GUT (Godot Unit Test) | corre dentro del motor | requiere Godot para cada test, lento |
| xUnit + tests manuales en el editor | sin dependencias | no automatizable |
| NUnit | madurez | xUnit es el estándar actual de .NET |

## Consecuencias
- **Positivas:** CI rápido (segundos), cobertura medible (`coverlet`), determinismo comprobable.
- **Negativas:** hay que mantener el proyecto de tests aparte.
- **Riesgos:** que el código de test se acople a Godot por accidente; mismo guard de referencias.

## Revisión
Se revisará cuando se añada la capa de escenas y haga falta un smoke test de juego (launch headless + simular input).
