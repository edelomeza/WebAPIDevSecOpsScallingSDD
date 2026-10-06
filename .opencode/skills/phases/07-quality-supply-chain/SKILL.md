---
name: 07-quality-supply-chain
description: Análisis estático, mutation, ZAP/Trivy/dockle/RESTler, hardening report
---

## Propósito

Análisis estático, mutation, ZAP/Trivy/dockle/RESTler, hardening report.

## Cuándo usarla

Fase 9.

## Pasos

Semgrep, SonarAnalyzer, SonarCloud gate, Stryker 80/70/60, Trivy/dockle, ZAP, RESTler.

## Checklist

Umbrales reales; job hardening-report tolerante.

## Criterios de done

CI con jobs de seguridad verdes o informativos documentados.

## Límites/trampas

Safe Mode de Stryker oculta métodos sin test; dockle accept-key por versión.

## Referencias

`07-01`…`07-03`.
