# 03-18 — Contratos JSON

## Contexto
Fixtures JSON reales por endpoint para contratos y Pact. **Depende de**: `03-00`.

## Requisitos
1. Proveer fixtures JSON de `LoginResponse`, `PagedResult<T>`, `DashboardDto` y uno por endpoint.
2. Cubrir `RefreshResponse`, `Login2faVerifyResponse`, `VenVentaDto`, `VenPedidoPagoResponseDto`, `VenPedidoFacturaResponseDto`.

## Diseño
- Fixtures versionados junto a los tests de contrato; PascalCase estricto.

## Contratos
- Fixtures: `LoginResponse`, `PagedResult<T>`, `DashboardDto`, `RefreshResponse`, `Login2faVerifyResponse`, `VenVentaDto`, `VenPedidoPagoResponseDto`, `VenPedidoFacturaResponseDto`.

## Tests
- `ContractTest` (Pact, fase 10).

## Criterios
- Cada endpoint tiene fixture JSON.

## Límites
- Verificación Pact contra proceso real en fase 10.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
