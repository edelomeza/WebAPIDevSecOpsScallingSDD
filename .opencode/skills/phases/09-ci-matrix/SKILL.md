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
3. Fijar timeouts reales (Stryker nightly 180min detached; Integration serie ~5min; `blame-hang-timeout 10m`).
4. Pinear actions a SHA inmutable + comentario `# vX` (Dependabot los sigue actualizando; SAST `mutable-tag` bloquea si no).
5. Semgrep: `semgrep scan --config=auto --config=.semgrep/semgrep.yaml --error` (SIN `--metrics=off`: `scan` lo rechaza, solo vale para `ci` con token).
6. Reproducir NU1100 pristino en local: `dotnet restore --force --no-cache /p:RestorePackagesPath=<temp-vacia>` (la caché tibia oculta el fallo); mapear IDs exactos + transitivos (`Testcontainers` sin `.*`, `Pipelines.Sockets.Unofficial`, `SSH.NET`, `SharpZipLib`, `BouncyCastle.Cryptography`…).
7. Jobs agregadores tolerantes + artifacts en pareja de majors.
8. Job `critic` (Fase 1): paralelo sin `needs`, mismo SHA checkout, `shell: pwsh`, `exit 0/1`; PR template `3→8` checks; sub-agentes `slice-scaffolder`/`security-reviewer` (ver `operations/critic-guardrails`).

## Checklist

Matriz job×evento; `if: always()` + `continue-on-error`; timeout medido.

## Criterios de done

CI verde en main; jobs con artifacts esperados.

## Límites/trampas

No renombrar steps sin parametrizar; no hardcodear thresholds.

## Referencias

`09-06`, `.github/workflows/ci-cd.yml`, `operations/critic-guardrails`.
