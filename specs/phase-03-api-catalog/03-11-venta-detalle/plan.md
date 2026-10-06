# plan.md — 03-11-venta-detalle

1. Mapeo

- Crear: VentaDetalleController, VentaDetalleService.
- Más (T2): `Dtos/ProProductoDtos.cs` (+`ProProductoAutocompleteDto`, propiedad `03-03`), `Services/VentaDetalleService.cs` (+`AutocompleteProductoAsync`), `Controllers/V1/VentaDetalleController.cs` (`[HttpGet("autocomplete-productos")]`, `CancellationToken`, `{ error }`).
- Modificar tests: `UnitTest/VentaDetalle/`, `IntegrationTest/VentaDetalle/`, `SecurityTest/VentaDetalle/` (+ autocomplete).
- Crear/ampliar config Stryker del slice (`ignore-mutations: ["Boolean"]`).

2. Decisiones

- DTO de producto reutilizado cross-slice (no duplicar); `texto` requerido `[StringLength(50)]` + `Trim()` + `Contains`; `maxResultados [1,50] → 10`.
- Caché `cache:producto:autocomplete:…` 60s `$"..."` sobre `producto:version` existente; `AsNoTracking` + `OrderBy(id)`; `ConfigureAwait(false)`.

3. Guardarraíles

- Restore stock en delete.
- Ownership: 403 si no es dueño.
- Autocomplete: solo `{id, strNombreProducto}`; `400` siempre `{ error }`; sin filtro de stock (se valida en el alta); `dotnet build` tras Stryker antes de `--no-build`.

4. Pruebas

- SecurityTest/VentaDetalle/ (T1 ownership + 1×401 autocomplete).
- UnitTest/VentaDetalle/ (T1 + top-N/bordes/forma/caché).
- IntegrationTest/VentaDetalle/ (T1 + 200/400/403).
- Stryker ≥80% en el servicio.

5. Secuencia

1. Detalle entity.
2. Delete restore.
3. Ownership check.
4. Autocomplete (DTO `03-03` → servicio → controller → Unit → Integration → Security → Stryker).
5. Conciliar spec/plan con archivos reales y resolver aprobación.
