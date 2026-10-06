# 03-12 — Ventas pedido

## Contexto
Endpoint de creación de pedido de la saga de ventas. **Depende de**: `03-00`, `03-16`, `04-04`, `06-01`, `06-02`, `06-04`, `03-01`, `03-03`.

## Requisitos
1. `POST /api/v1/ventas/pedido` (`AdminOnly+AdminPolicy`) crea `VenPedido` y publica `PedidoCreadoEvent`.
2. DTOs `PedidoCreateDto/PedidoDetalleCreateDto/PedidoResponseDto/PedidoDetalleResponseDto` + validadores.

## Diseño
- `VenPedido` con estados (valores en fase 06); evento inicial de la saga coreográfica.

## Contratos
- `PedidoCreateDto` → `PedidoResponseDto` (201); códigos 201/400/401/403.
- Datos asumidos (`02-04`): estado `saga`; `VenPedido` id=11111111-… estado `Registrado`; `CliCliente` id=1; `ProProducto` id=1.

## Tests
- `IntegrationTest/Saga/VentasPedidoTests.cs`, `UnitTest/VentasPedido/`.

## Criterios
- Endpoints AdminOnly+AdminPolicy.

## Límites
- Pago/factura/dashboard en `03-13`…`03-15`; máquina de estados en fase 06.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
