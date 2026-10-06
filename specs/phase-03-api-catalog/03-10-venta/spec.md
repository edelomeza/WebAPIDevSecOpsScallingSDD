# 03-10 — Venta

## Contexto
Venta síncrona (legacy) con descuento de stock en la misma transacción, más búsqueda multifiltro de ventas en el mismo flujo. **Depende de**: `03-01`, `03-05`, `03-04`, `03-03`.

## Requisitos
1. `POST /api/v1/ventas` (Bearer) crea `VenVenta` + detalles y descuenta stock en una Tx.
2. DTOs `VenVentaCreateDto/UpdateDto/DeleteDto` + validadores `VenVenta*Validator`.
3. Carrera: 5 POST paralelos con existencia=1 → 1 éxito, 4×400.
4. `GET /api/v{version:apiVersion}/ventas/search?strClaveVenta=...&strNombreCliente=...&dteFechaInicio=...&dteFechaFin=...&page=1&pageSize=20` → `200 PagedResult<VenVentaDto>`; los 4 filtros opcionales se combinan con AND; sin filtros equivale a paginado completo. Sin `QueryParams` (coherencia con `03-01`/`03-02`/`03-03` T2).
5. `strClaveVenta` máx 10 (`[StringLength(10)]`, espejo del modelo; coincidencia exacta con `Trim()`); `strNombreCliente` máx 100 (`[StringLength(100)]`, espejo de `CliCliente.strNombreCliente`; `Contains` con `Trim()` vía JOIN a `CliCliente`).
6. `dteFechaInicio > dteFechaFin` → `400 { error }`; el rango filtra sobre `dteFechaHoraCompra` tolerando nulos (registros sin fecha se excluyen solo si hay filtro de fecha).
7. Paginación igual que los slices: `page < 1 || pageSize < 1 || pageSize > 100` → `400 { error }`.
8. Servicio: `SearchAsync(string? clave, string? nombreCliente, DateTime? inicio, DateTime? fin, int page, int pageSize, CancellationToken)`; `AsNoTracking`, `OrderBy(id)`; caché versionada 60s `cache:venta:search:{version}:{clave}:{nombre}:{ini}:{fin}:{page}:{pageSize}`; interpolación `$"..."`.
9. Auth del slice (`AdminPolicy` / Bearer según defina el T1) heredada en `search`.
10. Tests unit/integration/security del search; Stryker ≥80% en lo nuevo.

## Diseño
- Servicio transaccional con concurrencia optimista sobre `ProProducto`; 409/400 ante stock insuficiente.
- Search: query con JOIN a `CliCliente` (`idCliCliente`) para el filtro por nombre; clave exacta + nombre `Contains` + rango de fechas (AND); fechas en llave de caché con formato round-trip (`o`) o ticks.
- `Controllers/V1/VentaController.cs` (`[HttpGet("search")]`, `CancellationToken`, `400` como `BadRequest(new { error })`); sin cambios en DTOs de escritura.

## Contratos
- `VenVentaCreateDto` → 201 Created; códigos 201/400/401/409/422.
- Datos asumidos (`02-04`): estado `race`; `ProProducto` id=1 existencia=1; `CliCliente` id=1; `SegUsuario` id=1.
- `GET /api/v1/ventas/search?strNombreCliente=ana&dteFechaInicio=2026-01-01&dteFechaFin=2026-12-31&page=1&pageSize=20` → `200 { Items, TotalCount, Page, PageSize }` PascalCase; `400` rango invertido/paginación inválida; `401/403`.

## Tests
- `UnitTest/Venta/`, `IntegrationTest/Venta/`, `SecurityTest/Venta/` (T1: race 5 POST).
- Search Unit (fake `ICacheService`): clave exacta, nombre vía JOIN, rango de fechas, combinación AND de 4 filtros, sin filtros = todo, `inicio>fin` no llega al servicio (400 en controller vía integration), nulos de fecha, caché y llaves/TTL.
- Search Integration (`TestAuthHandler`): 200 + `TotalCount`, rango invertido → 400, `page=0` → 400, `403` rol `User`.
- Search Security: anónimo → `401` en `search`.
- Stryker (config del slice, `ignore-mutations: ["Boolean"]`): ≥80%.

## Criterios
- 5 POST paralelos con existencia=1 → 1 éxito, 4×400.
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` 100% verdes.
- `GET /api/v1/ventas/search?strClaveVenta=<clave-seed>` → 200 con `TotalCount>=1`; `?dteFechaInicio=<fin>&dteFechaFin=<inicio>` → 400 con `error`.
- Stryker en `VentaService` ≥80%; `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).

## Límites
- Saga asíncrona en `03-12`…`03-15` / fase 06.
- Sin `QueryParams`; clave exacta (no parcial); `Contains` en nombre por collation (InMemory case-sensitive: tests usan mismo casing); sin full-text/acentos/ranking.
- Sin filtros por usuario/estado/importe; `OrderBy(id)` fijo.
- Consistencia eventual de lectura ≤60s por rotación de versión en writes.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** T1 venta legacy + T2 search multifiltro especificados; pendientes de ejecución.
