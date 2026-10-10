# plan.md — 06-01-transport-events

1. Mapeo

- Modificar: Program.cs (MassTransit transport), VentasPedidoService.cs
  (respuesta antes de publicar), VentasPagoService.cs (publica PagoProcesadoEvent),
  AppDbContext.cs (VenEventoProcesado + UNIQUE), UnitTest (AppSettingsTests,
  VentasPagoServiceTests, VentasPedidoServiceTests), IntegrationTest/Saga
  (pedido/dashboard con espera de coreografía).
- Crear: Consumers/* (4), Events/* (7 records), Services (SagaStates, EventBus,
  MassTransitPedidoEventPublisher, PagoEventPublisher), Models/VenEventoProcesado.cs,
  Migrations/*_EventosProcesados0601.cs, UnitTest (Events, Consumers, Transport),
  IntegrationTest/Saga/TransportTests.cs, docs/saga-state-machine.md, stryker-0601.json.
- Eliminar: Services/StockValidatorConsumer.cs (stub), scripts scratch.

2. Guardarraíles

- InMemory solo Dev/test; SQS en prod con DLQ, maxReceiveCount=3 (fase 09).
- Consumers idempotentes (UNIQUE + marca con mutación); sin secretos/PII en eventos;
  credenciales SQS solo env/IAM; catch que traga prohibido en Consumers.
- MassTransit admite una sola fábrica de bus (if/else, no dos Using*).

3. Pruebas

- IntegrationTest/Saga/ (cadena feliz hasta Facturado + rechazo hasta Cancelado).
- ChaosTest/Experiments/ (SQS caída → DLQ) en fase 09.

4. Secuencia

1. Paquetes + config + migración.
2. Eventos + consumers + publishers + Program.cs.
3. Tests + Stryker + gates + Memoria.
