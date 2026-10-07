---
name: 06-saga-state-machine
description: Estados y transiciones de la saga de ventas
---

## Propósito

Estados y transiciones de la saga.

## Cuándo usarla

Al diseñar o revisar saga de ventas.

## Precondiciones

`06-02` definido.

## Pasos

1. Enumerar estados: Pendiente, StockValidado, PagoProcesado, Facturado, Compensado.
2. Definir transiciones y consumer responsable.
3. Documentar compensación en 2 niveles.
4. Crear `docs/saga-state-machine.md`.

## Checklist

Cada transición indica consumer y condición; compensación documentada.

## Criterios de done

Diagrama sin estados huérfanos; `IntegrationTest/Saga/` verde.

## Límites/trampas

No transiciones sin consumer; no estados sin evidencia.
Conflicto abierto (fijar al ejecutar `06-04`): esta skill enumera `Pendiente, StockValidado, PagoProcesado, Facturado, Compensado`, pero el código aprobado usa `Registrado` (`02-04`/`TestDataFactory`) y `"Creado"` (`03-12` + `NOTE 06-04`). El canónico se define en `06-04`, no aquí.

## Referencias

`06-04`, `docs/saga-state-machine.md`.
