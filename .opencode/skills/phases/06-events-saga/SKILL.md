---
name: 06-events-saga
description: MassTransit, eventos, saga coreográfica, compensaciones, DLQ
---

## Propósito

MassTransit, eventos, saga coreográfica, compensaciones, DLQ.

## Cuándo usarla

Fase 8.

## Pasos

7 eventos, 4 consumers, dual-write legacy, estados, DLQ FIFO.

## Checklist

Transporte InMemory/SQS por config; `IEventPublisher`; idempotencia.

## Criterios de done

Flujo pedido→stock→pago→factura; compensación en fallo.

## Límites/trampas

Reconnection storm si timeouts no tuned; NBomber borra reportes.

## Referencias

`06-01`, `06-02`.
