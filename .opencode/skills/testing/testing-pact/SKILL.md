---
name: testing-pact
description: Contratos API con Pact contra proceso real
---

## Propósito

Contratos API.

## Pasos

Pacts JSON, `pactSpecification` 3.0.0 flat `{"match":"type"}`, proceso real + puerto libre + kill finally, `/provider-states` con IDENTITY_INSERT.
Fase previa ejecutada `03-18` (sin PactNet): `ContractTest/Fixtures/*.json` (14, fuente única) generados por captura real del wire (`FixtureCaptureTests` con `WebApplicationFactory` `Staging` + auth `Test` + InMemory sin Docker; escribe solo con `CONTRACT_CAPTURE=1`, sin la variable valida status sin ensuciar git); refresh vía `IRefreshTokenService.CreateAsync` scoped; convención de nombres medida en `IsConventional` (prefijos legacy + `id`/sufijo + resto PascalCase, no PascalCase puro); `RowVersion` base64 `"AQ=="`; `xunit.runner.json` en serie. Catálogo de rutas verificado aparte en `docs/endpoints.md` (ver `operations/drift-guards`); job CI `contract` (ver `phases/09-ci-matrix`).

## Checklist

WebApplicationFactory no sirve para Pact; RowVersion base64 `"AQ=="`.

## Referencias

`ContractTest`, `specs/phase-03-api-catalog/03-18-json-contracts/spec.md`, `operations/drift-guards`, `phases/09-ci-matrix`.
