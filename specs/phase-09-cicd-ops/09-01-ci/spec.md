# 09-01 — CI

## Contexto
Pipeline global de integración continua. **Depende de**: todas.

## Requisitos
1. Orden: restore → build → unit → integration → security → database → contract → mutation (nightly) → performance (nightly opcional) → chaos (nightly).
2. Tests siempre con `--no-build`.

## Diseño
- Workflows en `.github/workflows/`; matriz de jobs en `09-06`.

## Contratos
- N/A.

## Tests
- N/A (el pipeline ejecuta las suites).

## Criterios
- Orden documentado; nightly verde.

## Límites
- Mutation/perf/chaos solo nightly (coste y duración).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
