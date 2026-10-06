---
name: 02-test-data
description: Datos de prueba concretos y reproducibles para tests
---

## Propósito

Datos concretos de prueba.

## Cuándo usarla

Antes de escribir tests unitarios/integration/security.

## Precondiciones

`02-02` definido.

## Pasos

1. Crear `UnitTest/Common/TestDataFactory.cs`.
2. Definir seeds: admin, cliente 1, producto 1 con `existencia=1`.
3. Documentar `PERF_LOGIN_USER` y estados de `VenPedido`.
4. Referenciar desde specs que consumen IDs.

## Checklist

IDs asumibles; `PERF_LOGIN_USER` documentado; specs citan `02-04`.

## Criterios de done

Tests usan fixtures compartidos; no duplican datos.

## Límites/trampas

No crear datos en runtime de producción.

## Referencias

`02-04`, `UnitTest/Common/TestDataFactory.cs`.
