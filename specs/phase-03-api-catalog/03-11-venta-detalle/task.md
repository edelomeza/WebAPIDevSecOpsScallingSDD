# 03-11 — Venta Detalle

## T1 — Detalle de venta
- **Crear**: `VentaDetalleController`, `VentaDetalleService`.
- **Verificar**: delete restaura stock; ownership 403.

## T2 — Autocomplete de productos (pendiente)
- **Modificar**: `Dtos/ProProductoDtos.cs` (+`ProProductoAutocompleteDto {id, strNombreProducto}`, propiedad `03-03`), `Services/VentaDetalleService.cs` (+`AutocompleteProductoAsync(texto, maxResultados)` sobre `ProProductos`), `Controllers/V1/VentaDetalleController.cs` (`[HttpGet("autocomplete-productos")]` + `CancellationToken` + `BadRequest(new { error })`).
- **Tests**: `UnitTest/VentaDetalle/` (top-N/trim/bordes `0/51 → 10`/forma/caché), `IntegrationTest/VentaDetalle/` (200/400/403), `SecurityTest/VentaDetalle/` (1×401).
- **Verificar**: `dotnet build -c Release --no-restore` 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` verdes; Stryker ≥80%; `dotnet build` tras Stryker.
- **Guardarraíles**: DTO reutilizado (no duplicado); caché `cache:producto:autocomplete:*` sobre `producto:version`; sin filtro de stock; `ConfigureAwait(false)`.
- **Cierre**: conciliar con archivos reales, actualizar `Memoria.md`.
