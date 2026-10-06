---
name: 09-chaos-experiments
description: Definir experimentos de caos con verify robusto
---

## Propósito

Definir experimentos de caos con verify robusto.

## Cuándo usarla

Al ejecutar o agregar experimentos en `ChaosTest`.

## Precondiciones

`09-03`, `09-05` definidos.

## Pasos

1. Definir `redis-kill.json`, `sql-kill.json`, `redis-latency.json`.
2. Runner `run-chaos.ps1` con exit 0/1/2.
3. Verify con retry 5×5s y grupo control.
4. Carga con `PERF_*_USERS` relajada.
5. Recolectar JSON reports en `reports/` (no compartir con NBomber).

## Checklist

PowerShell `-UseBasicParsing`; `tc netem` solo Linux; thresholds de carga relajados.

## Criterios de done

Redis caído → fallback; SQL caído → circuit breaker; exit 0.

## Límites/trampas

Imágenes aspnet sin curl; NBomber borra su carpeta de reportes.

## Referencias

`09-05`, `ChaosTest/Experiments/README.md`.
