---
name: testing-pact
description: Contratos API con Pact contra proceso real
---

## Propósito

Contratos API.

## Pasos

Pacts JSON, `pactSpecification` 3.0.0 flat `{"match":"type"}`, proceso real + puerto libre + kill finally, `/provider-states` con IDENTITY_INSERT.

## Checklist

WebApplicationFactory no sirve para Pact; RowVersion base64 `"AQ=="`.

## Referencias

`ContractTest`.
