# 0002. Simulación determinista headless separada de la presentación

- **Fecha:** 2026-11-12
- **Estado:** aceptada

## Contexto
Queremos bugs reproducibles, pruebas automáticas de la IA y del combate, replays, y una futura cámara FPS/TPS que no exija reescribir el juego. La IA humana avanzada (trampas, flancos, memoria compartida) es difícil de depurar sin reproducibilidad.

## Decisión
Toda la simulación (mundo 3D, entidades, sistemas, IA, combate, loot, lesiones) corre en un **core sin dependencias del motor**, en **ticks fijos de 100 ms**, con RNG sembrado por el mundo y streams con nombre (`world.Rng.Fork("loot")`, `"zombie-ai"`, ...). La presentación (Godot) **lee** el estado y **emite intenciones**; nunca muta la simulación.

## Alternativas consideradas
| Alternativa | Pros | Contras |
|---|---|---|
| Lógica dentro de nodos Godot | simple al principio | intesteable sin editor, acoplamiento total |
| Simulación por frames variables | menos código | resultados no reproducibles |
| Ticks fijos + interpolación (elegida) | determinismo, tests, replays | un poco más de código en render |

## Consecuencias
- **Positivas:** tests de IA/combate en milisegundos, `git bisect` útil, simulación de balance por lotes, cámara intercambiable.
- **Negativas:** hay que interpolar en el render y mantener separadas intención/estado.
- **Riesgos:** flotantes de precisión distinta entre plataformas; se cuantiza el hash de estado (1e-4) y los tests comparan hashes/invariantes, no bits.

## Revisión
Se revisará si se decide multijugador (requiere determinismo entre máquinas, posible cambio a aritmética de punto fijo).
