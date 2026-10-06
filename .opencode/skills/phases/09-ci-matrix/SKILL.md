---
name: 09-ci-matrix
description: Matriz de jobs por evento con timeouts y artifacts
---

## Propósito

Matriz de jobs por evento con timeouts y artifacts.

## Cuándo usarla

Al definir o ajustar `ci-cd.yml` y workflows nightly.

## Precondiciones

`09-01`, `09-06` definidos.

## Pasos

1. Mapear jobs (build-and-test, docker-build, dockle, sonarcloud, database-test, mutation-test, contract-test, semgrep, zap, hardening-report) a eventos (push, PR, nightly, dispatch).
2. Definir `needs` y `if` por job.
3. Fijar timeouts reales (Stryker 180min, Pact continue-on-error).
4. Artifacts en pareja de majors.
5. Jobs agregadores tolerantes.

## Checklist

Matriz job×evento; `if: always()` + `continue-on-error`; timeout medido.

## Criterios de done

CI verde en main; jobs con artifacts esperados.

## Límites/trampas

No renombrar steps sin parametrizar; no hardcodear thresholds.

## Referencias

`09-06`, `.github/workflows/ci-cd.yml`.
