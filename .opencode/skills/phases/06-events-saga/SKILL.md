---
name: 06-events-saga
description: MassTransit, eventos, saga coreográfica, compensaciones, DLQ
---

## Propósito

MassTransit, eventos, saga coreográfica, compensaciones, DLQ.

## Cuándo usarla

Fase 06 (reemplaza los fakes de `03-12`…`03-15`).

## Pasos

7 eventos, 4 consumers, dual-write legacy, estados, DLQ FIFO.
Reemplazo fake→real: `grep NOTE (06-01/06-02/06-03/06-04)` sobre código+specs, swap `IPedidoEventPublisher`→MassTransit (`Transport=InMemory` local / `SQS` prod), cablear consumers + compensación 2 niveles, re-correr Stryker + build restaurativo por slice tocado.

## Checklist

Transporte InMemory/SQS por config; `IEventPublisher`; idempotencia.

## Criterios de done

Flujo pedido→stock→pago→factura; compensación en fallo.

## Límites/trampas

Reconnection storm si timeouts no tuned; NBomber borra reportes.

## Referencias

`06-01`, `06-02`.
