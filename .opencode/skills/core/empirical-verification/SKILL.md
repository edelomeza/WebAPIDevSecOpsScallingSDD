---
name: empirical-verification
description: Medir antes de fijar umbrales, nombres, timeouts y formatos
---

## Propósito

Medir antes de fijar umbrales, nombres, timeouts y formatos.

## Cuándo usarla

Al definir thresholds CI, nombres de métricas, timeouts, contratos JSON.

## Precondiciones

Acceso a logs/reportes reales.

## Pasos

1. Ejecutar y capturar.
2. Inspeccionar formato real.
3. Fijar umbral con margen.
4. Documentar evidencia.
5. Precedentes fase 04 (medir antes de fijar): `04-01` literal `"role"` vs `ClaimTypes.Role` + CA1865/CA1845 + CS8601 indexer; `04-02` S101 `Argon2Id` + S1135 `todo` minúsculas + `AsSpan(2)` que compensa `"m="→""` + `catch when` fail-closed + `test-case-filter FullyQualifiedName~UnitTest.` (5 vs 75min) + timing anti-enumeración <10s; `04-03` `UseHsts(Action)` inexistente + S3358 ternaria + namespace `HttpsPolicy` + HSTS wire no verificable → assert opciones; `04-04` `EnableRateLimiting` action>clase verificado por 429 + `RetryAfter` ausente → best-effort + eager vs lazy config; `04-05` parciales honestos V2/V3/V7/V9 como ejemplo de deuda medida.

## Checklist

Cobertura real medida; mutation score real; runtime real; nombres con `curl /metrics`.

## Criterios de done

Todo número en docs cita su fuente/fecha.

## Límites/trampas

No asumir `mutationScore` en JSON raíz; no asumir wget/curl en imágenes; PowerShell sin `-UseBasicParsing`.
