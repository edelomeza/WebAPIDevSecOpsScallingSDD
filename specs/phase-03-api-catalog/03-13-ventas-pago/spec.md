# 03-13 — Ventas pago

## Contexto
Endpoint de consulta/procesamiento de pago de la saga. **Depende de**: `03-12`, `06-01`, `06-02`, `06-03`, `04-04`.

## Requisitos
1. `GET /api/v1/ventas/pago/{id}` (`AdminOnly+AdminPolicy`) devuelve `VenPedidoPagoResponseDto`.
2. Opcional `VenPedidoPagoCreateDto` + validador; `strIdTransaccion` único.

## Diseño
- Pago enlazado a `VenPedido`; eventos `PagoProcesadoEvent`/`PagoRechazadoEvent` en fase 06.

## Contratos
- `VenPedidoPagoResponseDto`; códigos 200/401/403/404.

## Tests
- `IntegrationTest/Saga/VentasPagoTests.cs`.

## Criterios
- Endpoints AdminOnly+AdminPolicy.

## Límites
- Lógica de cobro y compensaciones en fase 06.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
