# plan.md — 03-14-ventas-factura

1. Mapeo

- Crear: VentasFacturaController, VentasFacturaService, VenPedidoFactura, FacturaConsumer.

2. Guardarraíles

- Folio F-{año}-{seq} desde Redis; único.

3. Pruebas

- IntegrationTest/Saga/VentasFacturaTests.cs.

4. Secuencia

1. Entidad factura.
2. Service.
3. Controller.
