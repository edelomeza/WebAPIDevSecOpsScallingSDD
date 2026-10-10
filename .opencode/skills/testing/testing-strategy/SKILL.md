---
name: 10-testing-strategy
description: Qué suite cubre qué, flakiness rules y paralelismo
---

## Propósito

Consolidar qué suite cubre qué, flakiness rules y paralelismo.

## Cuándo usarla

Al planificar pruebas y al auditar cobertura.

## Precondiciones

Suites: Unit, Integration, Security, Database, Contract, Mutation, Perf, Chaos.

## Pasos

1. Definir cobertura objetivo por suite (conteos reales a 10-Oct-2026 post-`04-04`: unit 330, integration 99 (92+7 Docker), security 81, database 2, contract 4; intermedios `04-01` 294/67, `04-02` 316/74, `04-03` 326/77; hito 09-Oct: 289/85/62/2; hito 08-Oct: 231/82/59/2; crecer por slice, no fijar 600+/357/136 sin medir).
2. Flakiness rules: `IntegrationTest/xunit.runner.json` y `ContractTest/xunit.runner.json` en SERIE (`parallelizeTestCollections: false`, store InMemory compartido); FsCheck `.Trim()` + mismo casing (InMemory case-sensitive); seed determinista; todo test que cree datos los borra (`DELETE` + `RowVersion`) o usa `Guid` único por test (excepción: flujo único de captura `03-18`, sin tests vecinos que interfieran); seeding directo vía `AppDbContext` scoped cuando no hay `POST`; no asertar `TotalCount==1` exacto en stores compartidos (dashboard: asserts `>=1`).
3. Estrategia `03-09` TwoFactor: stubs controlables en unit vs Otp.NET real solo en integración. Estrategia `03-16` Errors: factories en `Staging`, `Production` con override `UseInMemoryDatabase=true`, sondas `probe` gateadas `EnableProviderStates`+no-prod. Estrategia `04-02`: Stryker local con `test-case-filter FullyQualifiedName~UnitTest.` (5min vs 75min, excluye 9 Docker-fallos). Estrategia `04-03`: `HstsOptions` en factory `Production` (wire no testeable en WAF). Estrategia `04-04`: `SecurityTest/RateLimit` 4 (429 en Login/Global/Admin + estructural reflection toda-action-con-policy) + `UnitTest/RateLimit` 4 (defaults/multiplier/clamp/nombres) + asserts `RateLimiting` en `AppSettingsTests` + `PERF_RATELIMIT_MULTIPLIER` para relajar en perf. `04-05` doc-only: suites heredadas 04-04 sin regresión posible (sin código nuevo).
3. `--blame-crash --blame-hang-timeout 10m` en CI.
4. Pact con proceso real + puerto libre.
5. NBomber con `WithMaxFailCount` en caos.
6. Mapear cada suite a su carpeta: `UnitTest/`, `IntegrationTest/`, `SecurityTest/`, `DatabaseTest/`, `ContractTest/`, `MutationTest/`, `PerformanceTest/`, `ChaosTest/`, `fuzzing/`.

## Checklist

- Matriz suite↔responsabilidad.
- `xunit.runner.json` configurado.
- Coverage threshold 45%.
- Cada suite indica su carpeta real.
- Coverage objetivo por suite definido.

## Criterios de done

Cada tipo de test tiene suite y umbral; flakiness controlado.

## Límites/trampas

No subir paralelismo sin revisar DB InMemory; no olvidar TokenBlacklist estático.

## Referencias

`10-testing-strategy`, `xunit.runner.json`, `AGENTS.md`.
