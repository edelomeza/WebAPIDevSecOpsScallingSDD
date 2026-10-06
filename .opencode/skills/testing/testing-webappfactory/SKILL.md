---
name: testing-webappfactory
description: Integration/Security tests con WebApplicationFactory<Program>
---

## Propósito

Integration/Security con `WebApplicationFactory<Program>`.

## Pasos

`public partial class Program`; `UseSetting` para JWT y `UseInMemoryDatabase=true`; `Reset()` de estáticos.

## Checklist

Key ≥32B; InMemory; paralelismo limitado.

## Límites/trampas

TokenBlacklist estático cruza tests.

## Referencias

`IntegrationTest`, `SecurityTest`.
