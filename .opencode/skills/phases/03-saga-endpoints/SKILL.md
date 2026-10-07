---
name: 03-saga-endpoints
description: Endpoints saga: pedido, pago, factura, dashboard
---

## Propósito

Endpoints saga: pedido, pago, factura, dashboard.

## Cuándo usarla

Al crear `03-12`, `03-13`, `03-14`, `03-15`.

## Precondiciones

`03-12…03-15` pueden ejecutarse en modo mínimo fake-first SIN `06-02/06-03/06-04/04-04` previos (vía `NOTE`s + skill `core/deferred-scope-fakes`); el bus real llega en fase 06.

## Pasos

1. `VenPedido` con estado temporal (`"Creado"` + `NOTE 06-04`) y publicación `PedidoCreadoEvent` vía `IPedidoEventPublisher` fake (`NOTE 06-01`); el pedido NO descuenta stock (lo valida el consumer en `06-02`).
2. `VenPedidoPago` con `strIdTransaccion` único.
3. `VenPedidoFactura` con folio `F-{año}-{seq}` desde Redis.
4. Dashboard con métricas de saga y depth de cola.
5. Proteger con `AdminPolicy` (`NOTE 04-04` rate-limit).

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
