---
name: 03-saga-endpoints
description: Endpoints saga: pedido, pago, factura, dashboard
---

## Propósito

Endpoints saga: pedido, pago, factura, dashboard.

## Cuándo usarla

Al crear `03-12`, `03-13`, `03-14`, `03-15`.

## Precondiciones

`06-02`, `06-03`, `06-04`, `05-01`, `03-12…03-15` definidos.

## Pasos

1. `VenPedido` con estados y publicación `PedidoCreadoEvent`.
2. `VenPedidoPago` con `strIdTransaccion` único.
3. `VenPedidoFactura` con folio `F-{año}-{seq}` desde Redis.
4. Dashboard con métricas de saga y depth de cola.
5. Proteger con `AdminOnly`+`AdminPolicy`.

## Checklist

Endpoints protegidos; estados correctos; folio desde Redis.

## Criterios de done

Pedido→pago→factura con estados correctos; dashboard responde.

## Límites/trampas

No exponer saga sin auth; no olvidar DLQ/compensación.

## Referencias

`03-12…03-15`, `06-02`, `06-04`.
