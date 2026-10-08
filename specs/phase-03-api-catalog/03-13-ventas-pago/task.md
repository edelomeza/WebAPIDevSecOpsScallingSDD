# 03-13 — Ventas Pago

## T1 — Endpoint pago saga (ejecutado 2026-10-07)
- **Creados**: `VentasPagoController`, `VentasPagoService`, `PagoDtos`, `PagoValidators`, `stryker-0313.json`. `VenPedidoPago` ya existía (fase 02).
- **Diferido a fase 06** (decisión del usuario): `PagoConsumer`, eventos `PagoProcesado/Rechazado`, publisher.
- **Verificado**: `strIdTransaccion` único (duplicado → 409; `NULL` múltiple permitido); GET por id (`{id:int}`) y por pedido (`pedido/{pedidoId:guid}`).
