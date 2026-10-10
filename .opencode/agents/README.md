# Sub-agentes (hibrido agentes -> skills)

Convencion hibrida Fase 1: los agentes de este directorio definen rol,
permisos y criterios de done. Las reglas tecnicas viven en
`.opencode/skills/` (SKILL.md). Los agentes **solo enlazan skills,
nunca copian su contenido**. Si una skill cambia, el agente no se toca
(solo su `last-synced` al verificar que sigue vigente).

Invocacion: manual en dev con `@agente` (p. ej. `@slice-scaffolder`,
`@security-reviewer`); en CI solo corre el job `critic` ligero
(`scripts/critic-guardrails.ps1`), no los sub-agentes.

| Agente | Rol | Permisos |
|---|---|---|
| `slice-scaffolder` | Fase A: esqueleto compilable vertical-slice intra-PR (+ variante swap fake→real fase 04 + variante saga/consumer fase 06: eventos, consumers, MassTransit, diagrama) | edit+bash allow |
| `security-reviewer` | Gate pre-push: critica sin editar ni ejecutar Stryker (+ checks fase 04: JWT, hash, rate-limit, headers, secretos en logs; + checks fase 06 WARN→FAIL: secretos en eventos, SQS, idempotencia/retry/DLQ, compensación, auth del bus, schemas, estados) | edit deny |
| `traceability-clerk` | Matrices vivas 04-04/04-05 + saga 06-01…06-04 (diagrama, schemas, transiciones, NOTEs) + addenda: reporta drift, no edita | edit deny |

Convención y skills nuevas enlazadas: `operations/drift-guards`, `operations/critic-guardrails`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`, `phases/06-events-saga`, `phases/06-saga-state-machine`.
