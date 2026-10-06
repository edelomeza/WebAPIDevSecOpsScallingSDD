# 00-04 — Lessons Learned

## Contexto
Consolida las lecciones aprendidas del proyecto legado y de fases futuras como reglas operativas constitucionales. **Depende de**: `00-01`.

## Requisitos
1. 13 reglas, cada una con evidencia (fase/proyecto/fecha) no vacía.
2. Cada regla referenciada desde su spec aplicable (cross-link).
3. Sin duplicidad: AGENTS.md §10 apunta a esta spec como fuente canónica.

## Diseño

| # | Regla de la Lección Aprendida | Evidencia (Fase / Proyecto / Fecha) | Spec / Contexto Aplicable |
|---|---|---|---|
| 1 | Verificar empíricamente antes de escribir | [Sistema Legado - Histórico de Errores, Oct 2025] | `specs/_template.md`, skill `empirical-verification` |
| 2 | Medir runtimes reales antes de fijar timeouts/umbrales | [Sistema Legado - Histórico de Errores, Oct 2025] | `specs/phase-00-constitution/00-03-nfr/spec.md` |
| 3 | Resetear estado estático y resolver eager singletons de providers globales | [Sistema Legado - Histórico de Errores, Oct 2025] | `specs/phase-00-constitution/00-02-sdd-governance/spec.md` |
| 4 | Chequear release status de paquetes y documentar excepciones | [Sistema Legado - Histórico de Errores, Oct 2025] | `specs/phase-00-constitution/00-01-principles-stack/spec.md` |
| 5 | Documentar límites conocidos en vez de hacks | [Sistema Legado - Histórico de Errores, Oct 2025] | `Memoria.md`, 00-01 §Límites |
| 6 | Tests de frontera exactos (`>=`/`<`, mensajes, ambas ramas RNG) | [Sistema Legado - Histórico de Errores, Nov 2025] | skill `testing-strategy` |
| 7 | Herramientas con socket real → proceso real + puerto libre + cleanup `finally` | [Fase 4-6 Objetivo / Por Validar] | skill `testing-pact`, contrato `ContractTest/` |
| 8 | Scripts con fallback explícito, atomicidad (`mktemp`/`mv`), `LC_NUMERIC=C` | [Fase 4-6 Objetivo / Por Validar] | fase 09-cicd-ops |
| 9 | `.gitignore` defensivo de artefactos locales (`reports/`, `StrykerOutput/`) | [Fase 4-6 Objetivo / Por Validar] | `specs/phase-01-foundation/01-05-repo-layout/spec.md` |
| 10 | Diagnóstico de `verify FAIL status 0` con grupo control, logs y procesos apilados antes de tocar código | [Fase 4-6 Objetivo / Por Validar] | skill `chaos-verification`, fase 09 |
| 11 | Versionar majors de actions de artifacts en pareja | [Fase 4-6 Objetivo / Por Validar] | fase 09-cicd-ops, Dependabot |
| 12 | Jobs agregadores tolerantes (`if: always()`, `continue-on-error`) | [Fase 4-6 Objetivo / Por Validar] | fase 09-cicd-ops |
| 13 | Parsers de reportes defensivos y YAML CI validado localmente | [Fase 4-6 Objetivo / Por Validar] | fase 09, `scripts/` (pendiente) |

## Contratos
N/A — spec constitucional. Referencia normativa desde `AGENTS.md` §10.

## Tests
N/A — verificación manual: 13 filas, sin celdas vacías, 0 reglas duplicadas.

## Criterios
- 13 reglas presentes, sin duplicadas.
- Cada regla cita evidencia y cross-link no vacíos.
- `AGENTS.md` §10 y `Memoria.md` referencian esta spec.

## Límites
Las evidencias `[Fase 4-6 Objetivo / Por Validar]` se promoverán a fecha real al ejecutar cada fase. Las evidencias legadas son aproximadas (Oct/Nov 2025) por falta de registro exacto.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrado a `specs/_template.md`; evidencia clasificada Legado vs. Fase 4-6 Objetivo; duplicidad con AGENTS.md resuelta.
