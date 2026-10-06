# 08-01 — Métricas

## Contexto
Métricas y trazas con OpenTelemetry y Prometheus. **Depende de**: `01-02`.

## Requisitos
1. Instrumentar OTel (métricas + trazas) con exporter Prometheus (pin 1.17.0-beta.1).
2. Exponer `/metrics`; verificar nombres reales con `curl /metrics` antes de dashboards.

## Diseño
- Meter dedicado para métricas de calidad; dashboard Grafana en `deploy/`.

## Contratos
- `GET /metrics` con gauges.

## Tests
- N/A (verificación empírica contra `/metrics`).

## Criterios
- /metrics expone gauges.

## Límites
- Sin exporter en prod sin gate `Observability:ConsoleExport` (ver 01-04).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
