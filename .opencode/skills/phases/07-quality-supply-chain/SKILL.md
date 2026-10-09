---
name: 07-quality-supply-chain
description: Análisis estático, mutation, ZAP/Trivy/dockle/RESTler, hardening report
---

## Propósito

Análisis estático, mutation, ZAP/Trivy/dockle/RESTler, hardening report.

## Cuándo usarla

Fase 9.

## Pasos

Semgrep, SonarAnalyzer, SonarCloud gate (informativo; new code coverage ≥80%), Stryker `break 80/low 80/high 90` por slice (`stryker-XXXX.json` en raíz), Trivy/dockle (HIGH/CRITICAL falla), ZAP en PR y main, RESTler.
Scores medidos post-`03-15`: `03-15` 72%→100%, `03-09` 68.42%→`93.41%` (`break 80`, 4 runs, `Otp.*` en `nuget.config`), `03-16` 100% (`stryker-0316.json`); waiver `Secret` enrollment `03-09` registrado (ver `operations/critic-guardrails`).
`nuget.config` Package Source Mapping como control supply-chain: patrones exactos por ID (+ transitivos); verificar con restore pristino (ver `09-ci-matrix`).

## Checklist

Umbrales reales; job hardening-report tolerante.

## Criterios de done

CI con jobs de seguridad verdes o informativos documentados.

## Límites/trampas

Safe Mode de Stryker oculta métodos sin test; dockle accept-key por versión.

## Referencias

`07-01`…`07-03`, `testing/testing-mutation`, `operations/critic-guardrails`.
