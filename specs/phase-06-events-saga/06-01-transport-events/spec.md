# 06-01 — Transporte y eventos

## Contexto
Bus de eventos con MassTransit y consumers. **Depende de**: `01-02`, `02-01`.

## Requisitos
1. Configurar MassTransit con transporte InMemory en local y SQS en prod (flag `Transport`).
2. Implementar consumers con DLQ y reintentos.
3. SQS FIFO + DLQ en prod.

## Diseño
- `Transport=InMemory` local / `SQS` prod (ver 01-04).

## Contratos
- Eventos: `PedidoCreadoEvent`, `StockValidadoEvent`, `StockRechazadoEvent`, `PagoProcesadoEvent`, `PagoRechazadoEvent`, `FacturaGeneradoEvent`, `FacturaRechazadaEvent` (schemas en `06-03`).

## Tests
- `IntegrationTest/Events/`, `ChaosTest/Experiments/`.

## Criterios
- InMemory local; SQS en prod por Transport.

## Límites
- Infra SQS real solo en despliegue (fase 09).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
