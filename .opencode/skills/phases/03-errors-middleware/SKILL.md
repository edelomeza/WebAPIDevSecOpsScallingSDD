---
name: 03-errors-middleware
description: ErrorResponse uniforme + middleware único + sondas probe + factories Staging
---

## Propósito

Canon post-`03-16`: un solo try/catch en middleware, `ErrorResponse` uniforme, sondas de verificación, factories en `Staging`.

## Cuándo usarla

En cada controller nuevo; al mapear una excepción de dominio; al escribir `IntegrationTest` de errores.

## Pasos

1. `ExceptionHandlingMiddleware` (ÚNICO try/catch de dominio del repo) + `ErrorResponse` (`Error/Status/TraceId`, `Detail` solo no-prod, omitido en prod) + tipos `NotFoundException`/`ForbiddenAccessException`. Pipeline post-`04-03`: lo primero es `SecurityHeadersMiddleware` outermost absoluta (antes de `DeveloperExceptionPage`, cubre 403/500), ExceptionHandling va después; fuera `UseExceptionHandler` no-Dev (orden `01-02` aditivo, ver `phases/04-security-core`). `OnRejected` 429 de rate-limit reutiliza el mismo `ErrorResponse` sin Detail + `Retry-After` best-effort.
2. Purga en controllers: CERO try/catch de dominio (8 purgados en `03-16`); `21 NotFound() → throw NotFoundException`; ownership `EnsureOwner → throw ForbiddenAccessException → 403` (no `return Forbid/NotFound`). Único outlier: `EmpEmpleado` FK servicio `400 → 422`.
3. Sondas `GET /api/v1/probe/{timeout,error,forbidden}` gateadas por `EnableProviderStates` + no-prod (no existen en prod).
4. Tests: `10` factories de integración a `Staging` (en `Dev` la página de excepciones devolvería HTML); `Production` exige override `UseInMemoryDatabase=true`. `IntegrationTest/Errors 8/8` (+37/37 resto; `55/56` solo por Redis-Docker ambiental).
5. Stryker `stryker-0316.json`: **100%** (`break 80`, 1 run) + `dotnet build` restaurativo. Critic `PASS` (1 aviso GET-only legítimo).

## Checklist

- Sin try/catch en controllers; `throw` (no `return NotFound()`); `Detail` ausente en prod.
- Sondas gateadas; factories en `Staging`; spec en `🚧 Borrador con evidencia` hasta firma.

## Criterios de done

`Unit 289/289 (+14)`, `Security 62/62`, `Integration Errors 8/8`; Stryker `100%`.

## Límites/trampas

- No reintroducir try/catch ad-hoc (lo bloquea `operations/critic-guardrails` B4).
- No exponer `Detail` en prod; no dejar sondas habilitadas en prod.

## Referencias

`specs/phase-03-api-catalog/03-16-*`, `stryker-0316.json`, `operations/critic-guardrails`, `testing/testing-webappfactory` (Staging), `Memoria.md` (`03-16`).
