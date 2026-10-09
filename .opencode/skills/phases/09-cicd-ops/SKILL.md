---
name: 09-cicd-ops
description: Pipeline CI, nightly, Docker, AWS, chaos, Dependabot
---

## Propósito

Pipeline, nightly, Docker, AWS, chaos, Dependabot.

## Cuándo usarla

Fase 11.

## Pasos

CI orden restore→build→test×3; nightly Stryker 180min y chaos; Dockerfile; CloudFormation; Dependabot.
Sincronizado con `09-ci-matrix` (fuente de detalle): SHA pineado + `# vX`, Semgrep `scan --config=auto --config=.semgrep/semgrep.yaml --error` (sin `--metrics=off`), NU1100 pristino (`--force --no-cache` + carpeta vacía + transitivos), job `critic` paralelo (`shell: pwsh`, `exit 0/1`), job `endpoints` (`check_endpoints.ps1`), job `contract` (`ContractTest`, InMemory sin Docker), PR template 8 checks, sub-agentes Fase 1 (ver `operations/critic-guardrails`).

## Checklist

Jobs agregadores tolerantes; timeouts medidos.

## Criterios de done

Pipeline verde en main; nightly chaos ejecutado.

## Límites/trampas

Majors de artifacts en pareja; `ASPNETCORE_HTTP_PORTS` en .NET 10.

## Referencias

`09-01`…`09-03`, `phases/09-ci-matrix`, `operations/critic-guardrails`, `operations/drift-guards`.
