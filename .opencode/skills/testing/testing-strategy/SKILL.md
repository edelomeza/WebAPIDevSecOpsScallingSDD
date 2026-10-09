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

1. Definir cobertura objetivo por suite (conteos reales a 09-Oct-2026: unit 289, integration 85 (+Errors 8/8), security 62, database 2, contract 4; hito previo 08-Oct: 231/82/59/2; crecer por slice, no fijar 600+/357/136 sin medir).
2. Flakiness rules: `IntegrationTest/xunit.runner.json` y `ContractTest/xunit.runner.json` en SERIE (`parallelizeTestCollections: false`, store InMemory compartido); FsCheck `.Trim()` + mismo casing (InMemory case-sensitive); seed determinista; todo test que cree datos los borra (`DELETE` + `RowVersion`) o usa `Guid` único por test (excepción: flujo único de captura `03-18`, sin tests vecinos que interfieran); seeding directo vía `AppDbContext` scoped cuando no hay `POST`; no asertar `TotalCount==1` exacto en stores compartidos (dashboard: asserts `>=1`).
3. Estrategia `03-09` TwoFactor: stubs controlables en unit vs Otp.NET real solo en integración. Estrategia `03-16` Errors: factories en `Staging`, `Production` con override `UseInMemoryDatabase=true`, sondas `probe` gateadas `EnableProviderStates`+no-prod.
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
