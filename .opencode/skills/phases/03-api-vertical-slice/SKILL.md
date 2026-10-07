---
name: 03-api-vertical-slice
description: Patrón por endpoint (entidad→DTO→validation→controller→service→tests)
---

## Propósito

Patrón por endpoint (entidad→DTO→validation→controller→service→tests).

## Cuándo usarla

Cada spec `03-0x` de catálogo/entidad.

## Pasos

Seguir plantilla de vertical slice; auth `AdminPolicy` (`[Authorize(Policy = "AdminPolicy")]`; Bearer sin policy → `User→200`, documentar desviación).
DTOs: `required` en valores input (S6964), `RowVersion` base64 (`byte[]{1}` default), colecciones `IReadOnlyList` con setter (CA2227/CA1002).
Servicio: triple-check FKs → `ValidationException→422`, stock/concurrencia → `ConcurrencyConflictException→409`, Tx explícita solo `IsRelational` (InMemory eleva `TransactionIgnoredWarning` a error), `Guid` cliente → único `SaveChanges`; bucles validación+ cómputo fusionados con `TryGetValue` (S3267); `ConfigureAwait(false)` (no en cuerpos `[Fact]`, xUnit1030).
Controller: helper `ValidateAsync` (`DistinctBy`), `CreatedAtAction`, `GET {id}` auxiliar, `400 { error }` inline (atributo `[StringLength]` daría `ValidationProblem`).
Fakes + `NOTE (XX-YY)` para alcance diferido (ver `core/deferred-scope-fakes`); `try/catch` manual hasta `03-16`.

## Checklist

CRUD completo, PagedResult, FluentValidation, cache-aside, tests 3 suites + Stryker ≥80% + `dotnet build` restaurativo.

## Checklist

CRUD completo, PagedResult, FluentValidation, cache-aside, tests 3 suites.

## Criterios de done

Endpoint responde con códigos correctos; rate limit aplicado.

## Límites/trampas

No exponer entity directo; no cachear password.

## Referencias

`03-01`…`03-05`, `03-00`, `03-16`.
