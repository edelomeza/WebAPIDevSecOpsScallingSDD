# 06-01 — Transport & Events

## T1 — Transporte y eventos
- **Modificar**: `Program.cs` (MassTransit transport).
- **Crear**: `Consumers/*`, `Events/*`.
- **Verificar**: InMemory local; SQS prod con DLQ; consumers idempotentes.

## T1 ejecutado (evidencia)
- Paquetes: `MassTransit` + `MassTransit.AmazonSQS` 8.5.11 (Apache-2.0);
  `nuget.config` +`MassTransit*`/`AWSSDK.*`; `Transport=InMemory` + `Sqs:*` en
  `appsettings.Example.json` y fila `01-04` a Implementado.
- Bus en `Program.cs`: `AddConsumers` + `UsingInMemory` (retry 5×) o rama `SQS`
  (colas `saga-*.fifo`, región requerida fail-fast); credenciales por IAM.
- Contratos: 7 `record` con `SchemaVersion = 1` (desviación `Guid` documentada).
- Consumers: reserva de stock, validación de cobro, folio determinista,
  compensación con restauración guardada; `VenEventoProcesado` + migración
  `EventosProcesados0601` (factory temporal borrada).
- Tests: `UnitTest` 367/367 (+37: Events 4, Consumers 26, Transport 3, pago 4);
  `IntegrationTest/Saga` 21/21 (Transport 2 + esperas de coreografía).
- Lecciones: una sola fábrica de bus; marca con mutación (no antes);
  `folio` es campo mandado (no secreto); cobro fuera de orden se rechaza.
- Desviaciones: bus abstracto `IEventBus` (+ `IPedidoEventPublisher`/
  `IPagoEventPublisher` conservados) en vez de `IEventPublisher` de la skill;
  retry InMemory 5× 500ms→10s (la competencia Pago/Factura por el mismo evento
  exige horizonte amplio); `S1135`/`S125`/`CA1861`/`CA1002` cazados en el slice.
- **Verificar**: `critic PASS`; `endpoints OK (56)`; Stryker `stryker-0601.json` **90.77%** (residuo clasificado: adaptadores `Consume` + equivalente `>=500`).
- Pendiente: firma del spec; SQS vivo + caos en fase 09; 06-02 compensación total.
