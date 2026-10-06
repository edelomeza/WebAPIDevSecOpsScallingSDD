# 09-08 — Kill-tree

## Contexto
Terminación de árboles de proceso sin dejar huérfanos tras chaos/perf. **Depende de**: `09-03-ops`.

## Requisitos
1. Windows: `taskkill /PID /T /F`; Linux: `pkill -TERM -P`.
2. Sin procesos huérfanos tras chaos/perf.

## Diseño
- Helper reutilizable por runners (chaos, perf, Pact).

## Contratos
- N/A.

## Tests
- N/A (verificación post-ejecución).

## Criterios
- Sin procesos huérfanos tras chaos/perf.

## Límites
- No matar procesos `dotnet`/MSBuild persistentes salvo limpieza real.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
