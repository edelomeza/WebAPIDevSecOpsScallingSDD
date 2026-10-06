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

1. Entidad + DbContext.
2. DTOs Create/Update/Delete.
3. FluentValidation.
4. Servicio con cache/eventos.
5. Controller con auth/rate limit.
6. Tests unit/integration/security.

## Checklist

Los 6 componentes presentes y conectados.

## Criterios de done

`dotnet build` Release 0 errores; 3 suites verdes con `--no-build`.

## Límites/trampas

No dividir en PRs por capa; no commitear sin tests.
