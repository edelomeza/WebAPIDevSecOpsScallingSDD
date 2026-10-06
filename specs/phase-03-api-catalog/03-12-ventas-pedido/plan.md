# plan.md — 03-12-ventas-pedido

1. Mapeo

- Crear: VentasPedidoController, VentasPedidoService, VenPedido, PedidoCreadoEvent, StockValidatorConsumer.

2. Guardarraíles

- AdminOnly+AdminPolicy.
- Estados de saga documentados.

3. Pruebas

- IntegrationTest/Saga/VentasPedidoTests.cs.
- UnitTest/VentasPedido/.

4. Secuencia

1. Pedido entity.
2. Service.
3. Controller.
4. Evento.
