# 03-11 — Venta detalle

## Contexto
Detalle de venta legacy con restauración de stock al eliminar, más autocomplete de productos para el alta de detalles en el mismo flujo. **Depende de**: `03-10`, `03-03`.

## Requisitos
1. `POST /api/v1/ventas/{id}/detalles` y `DELETE /api/v1/ventas/detalles/{id}` (Bearer).
2. DTOs `VenVentaDetalleCreateDto/UpdateDto/DeleteDto` + validadores.
3. Delete restaura stock; ownership ajeno → 403.
4. `GET /api/v{version:apiVersion}/ventas/detalles/autocomplete-productos?texto=...&maxResultados=10` → `200 IEnumerable<ProProductoAutocompleteDto>` (`{ id, strNombreProducto }` únicamente); `texto` nulo/vacío/blanco → `400 { error = "El texto de búsqueda es requerido." }`; `texto` máx 50 (`[StringLength(50)]`, espejo de `ProProducto.strNombreProducto`); `maxResultados` fuera de `[1,50]` se normaliza a `10`.
5. DTO `ProProductoAutocompleteDto` **propiedad del slice `03-03`** (`Dtos/ProProductoDtos.cs`), consumido por `VentaDetalleService` (primera reutilización cross-slice; `03-11` ya depende de `03-03`).
6. Servicio: `AutocompleteProductoAsync(string texto, int maxResultados, CancellationToken)` sobre `ProProductos` (no sobre el detalle); `AsNoTracking`, `OrderBy(id)`, `texto.Trim()` con `Contains` sobre `strNombreProducto`.
7. Caché TTL 60s `cache:producto:autocomplete:{texto}:{max}` con la **versión existente `producto:version`** (los writes de `ProProducto` ya la rotan); interpolación `$"..."`; sin precio/existencia en la entrada ligera.
8. Auth del slice heredada en la ruta nueva.
9. Tests unit/integration/security del autocomplete; Stryker ≥80% en lo nuevo.

## Diseño
- Servicio transaccional; verificación de pertenencia del detalle a la venta y al usuario.
- Autocomplete: lectura solo-`ProProductos`, top-N, sin JOIN al detalle; controller `VentaDetalleController` (`[HttpGet("autocomplete-productos")]`, `CancellationToken`, `400` como `BadRequest(new { error })`).
- Sin cambios en DTOs de escritura del detalle.

## Contratos
- `VenVentaDetalleCreateDto` → 201; códigos 201/400/401/403/404; DELETE → 204/401/403/404.
- `GET /api/v1/ventas/detalles/autocomplete-productos?texto=torn&maxResultados=10` → `200 [{ id, strNombreProducto }]` (máx N, ordenados por `id`); `400` texto requerido; `401/403`.

## Tests
- `UnitTest/VentaDetalle/`, `IntegrationTest/VentaDetalle/`, `SecurityTest/VentaDetalle/` (T1: restore + ownership).
- Autocomplete Unit (fake `ICacheService`): top-N, `Trim()`, bordes `0/51/-5 → 10`, forma mínima `{id, strNombreProducto}`, 2ª llamada desde caché, llaves/TTL (`cache:producto:autocomplete:…`, 60s).
- Autocomplete Integration (`TestAuthHandler`): 200 con forma mínima, sin texto → 400 con `error`, bordes `0/51 → 10`, `403` rol `User`.
- Autocomplete Security: anónimo → `401` (1 test nuevo).
- Stryker (config del slice, `ignore-mutations: ["Boolean"]`): ≥80%.

## Criterios
- Delete restaura stock; ownership 403.
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` 100% verdes.
- `GET .../autocomplete-productos?texto=<fragmento-seed>&maxResultados=5` → 200 con `<=5` items solo `id`/`strNombreProducto`; sin `texto` → 400 con `error`.
- Stryker en `VentaDetalleService` ≥80%; `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).

## Límites
- Saga asíncrona en `03-12`…`03-15` / fase 06.
- `Contains` simple (collation del proveedor; InMemory case-sensitive: tests usan mismo casing); sin full-text/acentos/ranking.
- Sin filtro por existencia > 0 (fiel al snippet; el stock se valida al crear el detalle en el T1); sin precio en el DTO ligero; `OrderBy(id)` fijo.
- Consistencia eventual ≤60s (versión `producto:version` rotada por writes de `03-03`).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 07-Oct-2026
- **Detalle:** T1+T2 ejecutados y firmados sin cambios sobre la evidencia: `VentaDetalleController`/`VentaDetalleService` (`POST /ventas/{id}/detalles`, `DELETE /ventas/detalles/{id}`, `GET autocomplete-productos`), DTO `ProProductoAutocompleteDto` en `03-03` reutilizado; build 0/0, Unit 199/199, Security 53/53, Integration 67/67, Stryker 90.70% (`stryker-0311.json`).
- **Desviaciones registradas:** `User → 200` en autocomplete (Bearer sin policy, espejo 03-10; no 403); `RowVersion` añadido a `VenVentaDetalleDto` (necesario para DELETE concurrente); el servicio invalida también `producto:{id}` + `producto:version` (stock mutado; bug de caché stale cazado por test).
