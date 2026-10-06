# 08-02 — Calidad y auditoría

## Contexto
Métricas de calidad y cadena de auditoría con hash. **Depende de**: `08-01`.

## Requisitos
1. Exponer 4 gauges de calidad.
2. Implementar audit hash chain SHA-256 (`scripts/collect-quality-metrics.sh`).

## Diseño
- Cadena de hashes encadenados por registro de auditoría; verificación de integridad extremo a extremo.

## Contratos
- 4 gauges en `/metrics`.

## Tests
- N/A (verificación empírica de cadena íntegra).

## Criterios
- 4 gauges; audit chain íntegro.

## Límites
- Sin PII en la cadena de auditoría.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
