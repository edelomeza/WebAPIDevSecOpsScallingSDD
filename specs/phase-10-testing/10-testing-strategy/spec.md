# 10-testing-strategy — Estrategia de testing

## Contexto
Estrategia consolidada: qué suite cubre qué, reglas de flakiness y paralelismo. **Depende de**: todas anteriores.

## Requisitos
1. Tabla suite ↔ proyecto/ruta ↔ cobertura.
2. Flakiness rules (`.Trim()`, seeds deterministas).
3. `xunit.runner.json` con `maxParallelThreads: 4`; `--blame-crash` en CI.

## Diseño
- xUnit + FluentAssertions + Moq + FsCheck; `WebApplicationFactory` para Integration/Security; Testcontainers con puerto fijo.

## Contratos
- Umbrales por suite (ver `00-03` NFR).

## Tests
- N/A (meta-spec de estrategia).

## Criterios
- Cada tipo de test tiene suite y umbral definido.

## Límites
- N/A.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
