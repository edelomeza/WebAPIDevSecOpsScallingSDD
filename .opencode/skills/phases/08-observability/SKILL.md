---
name: 08-observability
description: OTel, Prometheus, Grafana, quality metrics, audit hash chain
---

## Propósito

OTel, Prometheus, Grafana, quality metrics, audit hash chain.

## Cuándo usarla

Fase 10.

## Pasos

1. OpenTelemetry metrics/tracing.
2. Prometheus exporter beta pin 1.17.0-beta.1.
3. `QualityMetricsService` con 4 gauges.
4. Audit hash chain.
5. Verificar nombre real de métricas con `curl /metrics`.
6. Documentar sufijos del exporter Prometheus en `docs/metrics.md`.

## Checklist

- Meter resuelto eager.
- Nombres con sufijos.
- `curl /metrics` verifica.
- `docs/metrics.md` actualizado.

## Criterios de done

`/metrics` expone 4 gauges; audit hash chain íntegro; `docs/metrics.md` documenta nombres reales.

## Límites/trampas

Singleton lazy no registra meter; exporter Console rompe testhost.

## Referencias

`08-01`, `08-02`.
