# 02-04 — Datos de prueba

## Contexto
Datos concretos y deterministas para tests (unitarios, integración, perf). **Depende de**: `02-02`.

## Requisitos
1. Crear `UnitTest/Common/TestDataFactory.cs` pública con constantes de IDs seed y builders deterministas.
2. Extender `/provider-states` con estados `race`, `saga`, `perf` idempotentes.
3. Materializar `PERF_LOGIN_USER` como `SegUsuario` id=2 `perf` (hash placeholder; credencial real solo por env en fase 10).
4. Sembrar `VenPedido` único en estado `Registrado` (más estados en fase 06).
5. Documentar IDs asumidos en specs `03-10` y `03-12`.

## Diseño
- `TestDataFactory`: `SeedClienteId/SeedProductoId/SeedUsuarioId/SeedVentaId = 1`, `PerfUsuarioId = 2`, `SeedPedidoId = 11111111-...`, `RaceStockExistencia = 1` + métodos `CreateCliente/Producto/Usuario/Pedido`.
- `DatabaseSeeder`: `ProProducto` id=1 con existencia=1; `race` resetea existencia=1; `saga` = base; `perf` añade usuario perf.
- `SecurityTest` referencia el proyecto `UnitTest` para reutilizar la factory.

## Contratos
- Estados de `/provider-states`: `base`, `saga` (seed mínimo), `race` (resetea existencia=1), `perf` (añade usuario perf).
- IDs asumibles: cliente 1, producto 1, usuario 1, perf 2, venta 1, pedido seed `Registrado`.

## Tests
- `UnitTest/Common/TestDataFactoryTests.cs`: constantes = seeder; builders deterministas.
- `IntegrationTest/Common/ProviderStatesTests.cs`: `race` → existencia 1; `perf` → usuario perf.

## Criterios
- Tests pueden asumir existencia de estos IDs.
- `dotnet test UnitTest/IntegrationTest` verdes.

## Límites
- Sin credenciales reales en repo; dataset masivo de perf en fase 10; estados saga en fase 06.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** `TestDataFactory`, estados `race/saga/perf`, `PERF_LOGIN_USER` id=2.
