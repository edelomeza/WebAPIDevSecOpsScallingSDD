---
name: testing-nbomber
description: Performance con escenarios Login/Producto/Venta/Mixto
---

## Propósito

Performance con escenarios Login/Producto/Venta/Mixto.

## Pasos

1. Crear escenarios Login/Producto/Venta/Mixto en `PerformanceTest/`.
2. Compartir JWT en `WithInit`.
3. Definir thresholds P95 y error rate.
4. Exit codes 0/1/2.
5. Configurar `PERF_*_USERS`.
6. Separar reportes en `perf-reports/` (no compartir con `reports/` de chaos).

## Checklist

- Rate limits relajados en perf.
- `WithMaxFailCount` en caos.
- `perf-reports/` contiene JSON por escenario.
- `perf-scripts/` referencia CSV/HTML.

## Criterios de done

Cada escenario ejecuta al menos una vez; reportes separados de caos.

## Límites/trampas

Argon2id colapsa 2 vCPU; NBomber borra su carpeta de reportes.

## Referencias

`PerformanceTest`, `10-02-performance-testing`.
