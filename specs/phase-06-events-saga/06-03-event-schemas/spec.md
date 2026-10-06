# 06-03 — Esquemas de eventos

## Contexto
Payloads JSON de los 7 eventos de la saga. **Depende de**: `06-01`.

## Requisitos
1. Definir schema JSON por evento.

## Diseño
- Contratos inmutables versionados; PascalCase.

## Contratos
- PedidoCreadoEvent: `{ "pedidoId": int, "clienteId": int, "productoId": int, "cantidad": int }`
- StockValidadoEvent: `{ "pedidoId": int, "productoId": int }`
- StockRechazadoEvent: `{ "pedidoId": int, "motivo": string }`
- PagoProcesadoEvent: `{ "pedidoId": int, "idTransaccion": string, "monto": decimal }`
- PagoRechazadoEvent: `{ "pedidoId": int, "motivo": string }`
- FacturaGeneradoEvent: `{ "pedidoId": int, "folio": string }`
- FacturaRechazadaEvent: `{ "pedidoId": int, "motivo": string }`

## Tests
- Validación de schemas en `IntegrationTest/Saga/`.

## Criterios
- Cada evento tiene schema JSON.

## Límites
- Sin cambios retrocompatibles sin versión nueva.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
