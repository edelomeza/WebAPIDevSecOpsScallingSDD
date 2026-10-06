# 03-14 — Ventas factura

## Contexto
Endpoint de factura de la saga con folio secuencial. **Depende de**: `03-13`, `06-01`, `06-02`, `06-03`, `05-01`, `04-04`.

## Requisitos
1. `GET /api/v1/ventas/factura/{id}` (`AdminOnly+AdminPolicy`) devuelve `VenPedidoFacturaResponseDto`.
2. Opcional `VenPedidoFacturaCreateDto` + validador; folio `F-{año}-{seq}` generado con Redis.

## Diseño
- Secuencia del folio con contador en caché; `strFolioFactura` único.

## Contratos
- `VenPedidoFacturaResponseDto`; códigos 200/401/403/404.

## Tests
- `IntegrationTest/Saga/VentasFacturaTests.cs`.

## Criterios
- AdminOnly+AdminPolicy.

## Límites
- Emisión fiscal y compensaciones en fase 06.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
