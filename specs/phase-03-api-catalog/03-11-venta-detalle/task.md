# 03-11 — Venta Detalle (ejecutado T1+T2 2026-10-06)

## T1 — Detalle de venta (ejecutado 2026-10-06)
- **Creados**: `VentaDetalleController` (`api/v{version}/ventas/detalles` + `POST ~/ventas/{idVenta}/detalles`, `[Authorize]` Bearer, ownership por claim `NameIdentifier/sub` + `NOTE (04-01)`), `VentaDetalleService` (`Add/Remove/GetById` + `AutocompleteProductoAsync`), `Dtos/VenVentaDetalleDtos.cs` (Create/Delete), `Validators/VenVentaDetalleValidators.cs`, `stryker-0311.json`.
- **Verificado**: delete restaura stock; ownership ajeno → 403 (`Forbid`); stock insuficiente → 409; `RowVersion` añadido a `VenVentaDetalleDto` (DELETE concurrente).

## T2 — Autocomplete de productos (ejecutado 2026-10-06)
- **Modificado**: `Dtos/ProProductoDtos.cs` (+`ProProductoAutocompleteDto {id, strNombreProducto}`, propiedad `03-03`), `Services/VentaDetalleService.cs` (+`AutocompleteProductoAsync(texto, maxResultados)` sobre `ProProductos`), `Controllers/V1/VentaDetalleController.cs` (`[HttpGet("autocomplete-productos")]` + `CancellationToken` + `BadRequest(new { error })`).
- **Tests**: `UnitTest/VentaDetalle/` (23: T1 + top-N/trim/bordes `0/51/-5 → 10`/borde `50`/forma/caché/valores-versión), `IntegrationTest/VentaDetalle/` (5: flujo add/remove + 403 ajeno + autocomplete 200/400 + `User → 200`), `SecurityTest/VentaDetalle/` (4×401).
- **Verificado**: `dotnet build -c Release --no-restore` 0/0; Unit 199/199, Integration 67/67, Security 53/53; Stryker 90.70% (`stryker-0311.json`, gate ≥80%); `dotnet build` tras cada run Stryker.
- **Guardarraíles**: DTO reutilizado (no duplicado); caché `cache:producto:autocomplete:{version}:{texto}:{max}` sobre `producto:version` (también invalidada en writes del detalle); sin filtro de stock; `ConfigureAwait(false)`.
- **Desviación**: `403` no aplica en autocomplete con Bearer (`User → 200`, espejo T2 03-10).
- **Cierre**: conciliado con archivos reales; `Memoria.md` actualizado.
