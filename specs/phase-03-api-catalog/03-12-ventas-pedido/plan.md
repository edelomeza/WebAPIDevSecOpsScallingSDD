# plan.md — 03-12-ventas-pedido (ejecutado T1 mínimo 2026-10-07)

1. Mapeo

- Creados: VentasPedidoController, VentasPedidoService, PedidoDtos, PedidoValidators, PedidoCreadoEvent, IPedidoEventPublisher/FakePedidoEventPublisher, StockValidatorConsumer (stub). `VenPedido` ya existía (fase 02).

2. Guardarraíles

- AdminOnly+AdminPolicy (`NOTE 04-04` rate-limit).
- Estado temporal `"Creado"` (`NOTE 06-04`); fake InMemory (`NOTE 06-01`); `try/catch` manual hasta `03-16`.
- Stryker ≥80% (`ignore-mutations Boolean`); `dotnet build` restaurativo tras cada run.

3. Pruebas

- `UnitTest/VentasPedido/` (13) + `IntegrationTest/Saga/VentasPedidoTests.cs` (5) + `SecurityTest/Saga/` (2).
- Verificado: build 0/0; Unit 212/212; Security 55/55; Integration 72/72; Stryker 87.76% + build restaurativo.

4. Secuencia

1. DTOs + validators.
2. Evento + publisher fake + servicio.
3. Consumer stub.
4. Controller + DI.
5. Tests + Stryker + docs.
