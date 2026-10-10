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
Baseline verde post-`04-04`: Unit 330/330, Security 81/81, Integration 99/99, Contract 4/4, Database 2/2 + `critic PASS` + `endpoints OK (56)`; Stryker local con `test-case-filter FullyQualifiedName~UnitTest.`; relajación rate-limit solo vía `PERF_RATELIMIT_MULTIPLIER`; canónicos `docs/rate-limit-matrix.md` + `docs/asvs-l2-checklist.md` (ver `operations/drift-guards`).

## Checklist

Jobs agregadores tolerantes; timeouts medidos.

## Criterios de done

Pipeline verde en main; nightly chaos ejecutado.

## Límites/trampas

Majors de artifacts en pareja; `ASPNETCORE_HTTP_PORTS` en .NET 10.

## Referencias

`09-01`…`09-03`, `phases/09-ci-matrix`, `operations/critic-guardrails`, `operations/drift-guards`.
