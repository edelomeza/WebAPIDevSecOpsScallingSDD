# 09-05 — Experimentos de caos

## Contexto
Definición de experimentos de caos con verificación robusta. **Depende de**: `09-03`.

## Requisitos
1. Experimentos en JSON (`ChaosTest/Experiments/`) con schema válido.
2. Runner `ChaosTest/run-chaos.ps1` con verify retry 5×5s.
3. Exit codes: 0 PASS / 1 FAIL / 2 error de suite.

## Diseño
- Escenarios `redis-kill`, `sql-kill`, `redis-latency` con grupos control.

## Contratos
- N/A.

## Tests
- `ChaosTest/` (nightly).

## Criterios
- JSON schema válido; verify retry; exit codes.

## Límites
- `reports/` (caos) separado de `perf-reports/` (NBomber borra su carpeta al arrancar).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
