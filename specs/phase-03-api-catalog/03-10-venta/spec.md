# 03-10 — Venta

## Contexto
Venta síncrona (legacy) con descuento de stock en la misma transacción, más búsqueda multifiltro de ventas en el mismo flujo. **Depende de**: `03-01`, `03-05`, `03-04`, `03-03`.

## Requisitos
1. `POST /api/v1/ventas` (Bearer) crea `VenVenta` + detalles y descuenta stock en una Tx (`Services/VentaService.cs`, `Controllers/V1/VentaController.cs`; `GET /api/v1/ventas/{id}` auxiliar para `CreatedAtAction`, fila añadida en `03-17`).
2. DTOs `VenVentaCreateDto` (+item) / `VenVentaDto` (+detalle) + validadores `VenVentaCreateValidator`/`VenVentaDetalleCreateItemValidator` (`Dtos/VenVentaDtos.cs`, `Validators/VenVentaValidators.cs`). `UpdateDto/DeleteDto` diferidos: sin rutas en catálogo `03-17` (T1 solo exige POST).
3. Carrera: 5 POST paralelos con existencia=1 → 1 éxito, 4×409 (criterio reescrito desde `4×400` por decisión de usuario 06-Oct-2026: stock insuficiente y concurrencia unificados en `ConcurrencyConflictException` → 409).
4. `GET /api/v{version:apiVersion}/ventas/search?strClaveVenta=...&strNombreCliente=...&dteFechaInicio=...&dteFechaFin=...&page=1&pageSize=20` → `200 PagedResult<VenVentaDto>`; los 4 filtros opcionales se combinan con AND; sin filtros equivale a paginado completo. Sin `QueryParams` (coherencia con `03-01`/`03-02`/`03-03` T2).
5. `strClaveVenta` máx 10 (`[StringLength(10)]`, espejo del modelo; coincidencia exacta con `Trim()`); `strNombreCliente` máx 100 (`[StringLength(100)]`, espejo de `CliCliente.strNombreCliente`; `Contains` con `Trim()` vía JOIN a `CliCliente`).
6. `dteFechaInicio > dteFechaFin` → `400 { error }`; el rango filtra sobre `dteFechaHoraCompra` tolerando nulos (registros sin fecha se excluyen solo si hay filtro de fecha).
7. Paginación igual que los slices: `page < 1 || pageSize < 1 || pageSize > 100` → `400 { error }`.
8. Servicio: `SearchAsync(string? clave, string? nombreCliente, DateTime? inicio, DateTime? fin, int page, int pageSize, CancellationToken)`; `AsNoTracking`, `OrderBy(id)`; caché versionada 60s `cache:venta:search:{version}:{clave}:{nombre}:{ini}:{fin}:{page}:{pageSize}`; interpolación `$"..."`.
9. Auth del slice (`AdminPolicy` / Bearer según defina el T1) heredada en `search`.
10. Tests unit/integration/security del search; Stryker ≥80% en lo nuevo.

## Diseño
- Servicio transaccional (`IsRelational` → Tx explícita en SQL; `SaveChanges` atómico en InMemory, que eleva `TransactionIgnoredWarning` a error) con concurrencia optimista sobre `ProProducto` (`RowVersion`); stock insuficiente y `DbUpdateConcurrencyException` → `ConcurrencyConflictException` → 409; FK desconocida → `ValidationException` → 422; validación sintáctica → 400.
- Totales (`decTotalVenta = decPrecio × piezas`) calculados servidor; `idSegUsuario` en body con `NOTE (04-01)` (migrar a claim `sub`); `dteFechaHoraCompra = UtcNow` servidor.
- Caché versionada `venta:version` TTL 60s `$"..."` (`cache:venta:{id}`); `AsNoTracking`, `OrderBy(id)`, `ConfigureAwait(false)`.
- Search: query con JOIN a `CliCliente` (`idCliCliente`) para el filtro por nombre; clave exacta + nombre `Contains` + rango de fechas (AND); fechas en llave de caché con formato round-trip (`o`) o ticks.
- `Controllers/V1/VentaController.cs` (`[HttpGet("search")]`, `CancellationToken`, `400` como `BadRequest(new { error })`); sin cambios en DTOs de escritura.

## Contratos
- `VenVentaCreateDto` → 201 Created; códigos 201/400/401/409/422.
- Datos asumidos (`02-04`): estado `race`; `ProProducto` id=1 existencia=1; `CliCliente` id=1; `SegUsuario` id=1.
- `GET /api/v1/ventas/search?strNombreCliente=ana&dteFechaInicio=2026-01-01&dteFechaFin=2026-12-31&page=1&pageSize=20` → `200 { Items, TotalCount, Page, PageSize }` PascalCase; `400` rango invertido/paginación inválida; `401/403`.

## Tests
- `UnitTest/Venta/` (17: `VentaServiceTests` 8 + `VenVentaValidatorTests` 2 + `VentaSearchTests` 7: clave exacta/trim, nombre-JOIN, rango+nulos, AND, paginado, nulos-sin-filtro, caché/TTL), `IntegrationTest/Venta/` (`VentaControllerTests` 9 + `RaceConditionTests` 1 en MsSql Testcontainers puerto 14336 + estado `race`), `SecurityTest/Venta/` (3×401).
- Search Integration: 200 + `TotalCount` (clave/nombre/sin-filtros/rango-vacío), rango invertido → 400, `page=0`/clave larga → 400, rol `User` → 200 (Bearer sin policy: **403 no aplica**, espejo `03-06`).
- Stryker `stryker-0310.json` (`VentaService.cs`, `ignore-mutations Boolean`): **82.11%** T2 (83.93% T1) — gate ≥80% cumplido.

## Criterios
- 5 POST paralelos con existencia=1 → 1×201, 4×409 (MsSql real; stock final 0).
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test UnitTest` → 176/176; `SecurityTest` → 49/49; `IntegrationTest` → 62/62.
- Stryker en `VentaService` ≥80% (medido 82.11% T2); `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).
- `GET /api/v1/ventas/search?strClaveVenta=<clave>` → 200 con `TotalCount>=1`; `?dteFechaInicio=<fin>&dteFechaFin=<inicio>` → 400 con `error`.

## Criterios
- 5 POST paralelos con existencia=1 → 1×201, 4×409 (MsSql real; stock final 0).
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test UnitTest` → 169/169; `SecurityTest` → 48/48; `IntegrationTest` → 58/58.
- Stryker en `VentaService` ≥80% (medido 83.93%); `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).

## Límites
- Saga asíncrona en `03-12`…`03-15` / fase 06.
- Sin `QueryParams`; clave exacta (no parcial); `Contains` en nombre por collation (InMemory case-sensitive: tests usan mismo casing); sin full-text/acentos/ranking.
- Sin filtros por usuario/estado/importe; `OrderBy(id)` fijo.
- Consistencia eventual de lectura ≤60s por rotación de versión en writes.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador con evidencia (T1+T2)
- **Revisores:** —
- **Fecha:** 06-Oct-2026
- **Detalle:** T1 + T2 ejecutados (Tx + race 1×201/4×409 + search multifiltro + Stryker 82.11%, suites verdes); criterio race reescrito `4×400`→`4×409` por decisión de usuario; 403 no aplica en search (Bearer sin policy); pendiente firma.
