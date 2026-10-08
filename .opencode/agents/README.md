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
| `slice-scaffolder` | Fase A: esqueleto compilable vertical-slice intra-PR | edit+bash allow |
| `security-reviewer` | Gate pre-push: critica sin editar ni ejecutar Stryker | edit deny |

`traceability-clerk` queda diferido a Fase 2.
