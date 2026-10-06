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

1. Definir cobertura objetivo por suite (unit 600+, integration 357, security 136).
2. Flakiness rules: FsCheck `.Trim()`, seed determinista, `maxParallelThreads:4`.
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
