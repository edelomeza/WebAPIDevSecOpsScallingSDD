# 09-03 — Ops

## Contexto
Operación: caos nocturno, dependencias y limpieza de procesos. **Depende de**: `09-02`.

## Requisitos
1. `chaos-nightly.yml` en `.github/workflows/`.
2. Dependabot activo.
3. Kill-tree para limpieza (ver `09-08`).

## Diseño
- Chaos con grupos control y prueba de vida en logs.

## Contratos
- N/A.

## Tests
- `ChaosTest/` (runner + experimentos en `09-05`).

## Criterios
- Chaos nightly verde; Dependabot activo.

## Límites
- Sin matar procesos `dotnet`/MSBuild persistentes salvo limpieza real.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
