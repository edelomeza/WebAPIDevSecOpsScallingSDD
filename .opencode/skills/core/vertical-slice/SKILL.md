---
name: vertical-slice
description: Entregar entidad+DTO+validación+controller+servicio+tests en un solo slice
---

## Propósito

Entregar entidad+DTO+validación+controller+servicio+tests en un solo slice.

## Cuándo usarla

En cualquier sub-spec de endpoint (`03-*`).

## Precondiciones

Spec del endpoint aprobada.

## Pasos

1. Entidad + DbContext (`IsRowVersion()` en auditables; mitigación InMemory `byte[]{1}` en `SaveChanges`).
2. DTOs Create/Update/Delete (`required`, `RowVersion`, `IReadOnlyList`).
3. FluentValidation (`>0`, `NotEmpty`, `RuleForEach`).
4. Servicio con cache-aside + eventos fake si aplica (`NOTE`).
5. Controller con auth explícita + `[EnableRateLimiting]` explícito (policies `Admin/Global/ConcurrentWrites/Login/Login2faVerify`; action prevalece sobre clase; fila en `docs/rate-limit-matrix.md`) + helper `ValidateAsync`. Post-`03-16`: sin try/catch de dominio (canon en `phases/03-errors-middleware`: `throw NotFound/Forbidden`, `EnsureOwner→403`); sondas `probe` gateadas; factories en `Staging`. Post-`04-03`: respetar orden `SecurityHeadersMiddleware` outermost → ExceptionHandling. Post-`04-01`: claims literales (`"role"`) + flag `Authentication:UseJwtBearer`.
6. Tests unit/integration/security + Stryker ≥80% + build restaurativo.
7. Conciliar `spec/plan/task.md` + `Memoria.md` (`Borrador con evidencia`; firma solo con revisor indicado + PR).
8. Fase 1: Fase A vía `slice-scaffolder` + gate `security-reviewer`/`operations/critic-guardrails` (nunca copiar reglas: solo enlazar `SKILL.md`).

## Checklist

Los 6 componentes presentes y conectados.

## Criterios de done

`dotnet build` Release 0 errores; 3 suites verdes con `--no-build`.

## Límites/trampas

No dividir en PRs por capa; no commitear sin tests.
