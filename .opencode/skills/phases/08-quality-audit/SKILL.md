---
name: 08-quality-audit
description: Métricas de calidad y audit hash chain con script de métricas
---

## Propósito

Métricas de calidad y audit hash chain.

## Cuándo usarla

Fase 10.

## Precondiciones

`08-01` definido.

## Pasos

1. Exponer 4 gauges en `/metrics`: coverage, mutation, ASVS, chaos.
2. Implementar `Middleware/AuditHashChain.cs` con SHA-256.
3. Script `scripts/collect-quality-metrics.sh` atómico (`mktemp`+`mv`), `LC_NUMERIC=C`.
4. Validar con `curl /metrics` y `quality-metrics.env`.

## Checklist

4 gauges presentes; audit chain íntegro; script produce `quality-metrics.env`.

## Criterios de done

`UnitTest/Audit/` verde; script ejecutable y produce archivo.

## Límites/trampas

No registrar Meter perezoso; no loggear secrets.

## Referencias

`08-02`, `scripts/collect-quality-metrics.sh`.
