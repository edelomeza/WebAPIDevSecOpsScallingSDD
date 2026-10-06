# 07-02 — Calidad de tests

## Contexto
Mutation testing y property-based testing. **Depende de**: `02-01`, `10-testing-strategy`.

## Requisitos
1. Stryker.NET con umbral break ≥80% (config `stryker-config.json` en raíz para nightly).
2. FsCheck determinista en `UnitTest/PropertyBased/*` (p. ej. `.Trim()` y redondeos).
3. Cobertura ≥45% (`coverage.runsettings`, `scripts/check_coverage.py`).

## Diseño
- Mutation por slice con `ignore-mutations` documentado solo para mutantes equivalentes.

## Contratos
- N/A.

## Tests
- `MutationTest/` (Stryker); `UnitTest/PropertyBased/`.

## Criterios
- Stryker ≥80% (break 60); FsCheck determinista.

## Límites
- Gate real es mutation score, no cobertura de línea.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
