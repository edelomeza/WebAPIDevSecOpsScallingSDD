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
5. Controller con auth explícita + helper `ValidateAsync`.
6. Tests unit/integration/security + Stryker ≥80% + build restaurativo.
7. Conciliar `spec/plan/task.md` + `Memoria.md` (`Borrador con evidencia`; firma solo con revisor indicado + PR).

## Checklist

Los 6 componentes presentes y conectados.

## Criterios de done

`dotnet build` Release 0 errores; 3 suites verdes con `--no-build`.

## Límites/trampas

No dividir en PRs por capa; no commitear sin tests.
