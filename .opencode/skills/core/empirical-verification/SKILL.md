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

## Checklist

Cobertura real medida; mutation score real; runtime real; nombres con `curl /metrics`.

## Criterios de done

Todo número en docs cita su fuente/fecha.

## Límites/trampas

No asumir `mutationScore` en JSON raíz; no asumir wget/curl en imágenes; PowerShell sin `-UseBasicParsing`.
