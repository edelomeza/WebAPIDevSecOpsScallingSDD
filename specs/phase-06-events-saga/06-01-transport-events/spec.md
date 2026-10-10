# 06-01 — Transporte y eventos

## Contexto
Bus de eventos con MassTransit y consumers. **Depende de**: `01-02`, `02-01`.

## Requisitos
1. Configurar MassTransit con transporte InMemory en local y SQS en prod (flag `Transport`).
2. Implementar consumers con DLQ y reintentos.
3. SQS FIFO + DLQ en prod.

## Diseño
- `Transport=InMemory` local / `SQS` prod (ver 01-04). MassTransit **8.5.11**
  (Apache-2.0, net10.0; se descartó 9.x por licencia comercial Massient).
- 7 eventos `record` inmutables versionados (`SchemaVersion = 1`) en `Events/`.
  Desviación: `PedidoId` es `Guid` (PK real), no `int`; `PedidoCreadoEvent` lleva
  `{PedidoId, ClienteId, Total}` (el pedido tiene N detalles).
- 4 consumers en `Consumers/`: `StockValidator` (valida + reserva),
  `Pago` (valida cobro), `Factura` (folio determinista `F-{año}-{pedidoId:N}`),
  `Compensation` (anula + restaura si hubo reserva).
- Idempotencia por `VenEventoProcesado` UNIQUE (`strNombreEvento`, `idPedido`);
  la marca viaja en el mismo `SaveChanges` que la mutación; el reintento no marca.
- Retry exponencial 5× (500ms→10s) en ambos transportes; SQS con colas
  `saga-*.fifo` + `ConfigureConsumer`; redrive DLQ (`MaxReceiveCount`) en fase 09.
- Cobro fuera de orden → `PagoRechazadoEvent`; factura temprana → reintento, no rechazo.
- `FakePedidoEventPublisher` intacto solo para UnitTest; prod usa MassTransit.

## Contratos
- Eventos: `PedidoCreadoEvent`, `StockValidadoEvent`, `StockRechazadoEvent`, `PagoProcesadoEvent`, `PagoRechazadoEvent`, `FacturaGeneradoEvent`, `FacturaRechazadaEvent` (schemas en `06-03`; diagrama en `docs/saga-state-machine.md`).

## Tests
- `UnitTest/Events/`, `UnitTest/Consumers/`, `UnitTest/Transport/`,
  `IntegrationTest/Saga/TransportTests.cs` (cadena feliz + rechazo).

## Criterios
- InMemory local; SQS en prod por Transport (rama verificada por config, sin AWS vivo).
- `dotnet build -c Release` 0/0; Unit/Integration/Security/Contract verdes;
  Stryker `stryker-0601.json` ≥ 80%; `critic PASS`; `endpoints OK (56)`.

## Límites
- Infra SQS real + caos SQS-caída en fase 09. Outbox EF diferido (duplicados inocuos
  por idempotencia). Compensación 2 niveles completa en 06-02.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario (1 Revisor)
- **Fecha:** 10-Oct-2026
- **Detalle:** T1 ejecutado en `phase06.preview` (transporte + 7 eventos + 4 consumers + tests); firma sin cambios sobre la evidencia.
