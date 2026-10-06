# 10-02 — Performance testing

## Contexto
Escenarios NBomber y umbrales de rendimiento. **Depende de**: `00-03-nfr`, `10-testing-strategy`.

## Requisitos
1. Escenarios Login/Producto/Venta/Mixto en `PerformanceTest/`.
2. Umbrales P95 según NFR; reportes en `perf-reports/` (separado de `reports/`).
3. `PERF_LOGIN_USER` solo en env de perf.

## Diseño
- NBomber con usuario perf dedicado; rate limits relajados solo vía env `PERF_*`.

## Contratos
- N/A.

## Tests
- `PerformanceTest/` (nightly).

## Criterios
- NBomber ejecuta al menos un escenario por suite.

## Límites
- Nightly por duración; sin relajar límites en prod.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
