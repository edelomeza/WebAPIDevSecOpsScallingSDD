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
Servicio: triple-check FKs → `ValidationException→422`, stock/concurrencia → `ConcurrencyConflictException→409` (matar `catch` con `ThrowingContext : AppDbContext` + flag `ThrowOnSave`, probado `03-13`), Tx explícita solo `IsRelational` (InMemory eleva `TransactionIgnoredWarning` a error), `Guid` cliente → único `SaveChanges`; bucles validación+ cómputo fusionados con `TryGetValue` (S3267); `ConfigureAwait(false)` (no en cuerpos `[Fact]`, xUnit1030; tampoco `new` inline en `await using` en `[Fact]` → helper, CA2007).
Controller: helper `ValidateAsync` (`DistinctBy`), `CreatedAtAction`, `GET {id}` auxiliar, `400 { error }` inline (atributo `[StringLength]` daría `ValidationProblem`); slice de solo lectura válido sin `POST` ni `try/catch` de dominio (`GET {id}` 200/404, probado `03-14`).
Fakes + `NOTE (XX-YY)` para alcance diferido (ver `core/deferred-scope-fakes`); post-`03-16`: CERO try/catch en controllers (canon en `phases/03-errors-middleware`: `throw NotFound/Forbidden`, `EnsureOwner→403`, `EmpEmpleado` FK `400→422`, sondas `probe` gateadas, factories en `Staging`).
Seeding en Integration vía `AppDbContext` scoped + `Guid` único por test cuando no hay `POST` (ver `testing/testing-webappfactory`).
Fase 1: esqueleto vía `slice-scaffolder` (Fase A compilable + test que aserta cada diferido + snippet DI + fila `03-17`); gate vía `security-reviewer` (`edit: deny`, diff-scoped) y `operations/critic-guardrails` (4 bloqueantes).

## Checklist

CRUD completo o slice acotado con `NOTE`s (`03-13` sin bus/consumer; `03-14` solo lectura sin `POST`/folio/consumer), PagedResult, FluentValidation, cache-aside, tests 3 suites + Stryker ≥80% + `dotnet build` restaurativo, sin try/catch en controllers (canon `03-16`), Fase A + critic verdes.

## Criterios de done

Endpoint responde con códigos correctos; rate limit aplicado.

## Límites/trampas

No exponer entity directo; no cachear password.

## Referencias

`03-01`…`03-05`, `03-00`, `03-16`, `03-07` (precedente scoped), `03-13`, `03-14`, `phases/03-errors-middleware`, `operations/critic-guardrails`.
