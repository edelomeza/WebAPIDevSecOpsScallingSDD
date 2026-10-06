# 03-03 T2 Addendum — SearchByName (pendiente)

## T2 — Mismo flujo, revisión independiente
- **Modificar**: `Services/ProProductoService.cs` (+`SearchByNameAsync(texto, page/pageSize)`), `Controllers/V1/ProProductoController.cs` (`[HttpGet("search")]` + `CancellationToken` + `BadRequest(new { error })`). Sin cambios en `Dtos/` ni `Validators/`.
- **Tests**: `UnitTest/ProProducto/` (filtra/ordena/trim/caché/vacío), `IntegrationTest/ProProducto/` (200/400/403), `SecurityTest/ProProducto/` (1×401).
- **Verificar**: `dotnet build -c Release --no-restore` 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` verdes; Stryker ≥80%; `dotnet build` tras Stryker.
- **Guardarraíles**: sin `QueryParams`; `Contains` en `strNombreProducto`; texto vacío → 400; llaves interpoladas TTL 60s; `ConfigureAwait(false)`.
- **Cierre**: fusionar Detalle en `spec.md` principal, actualizar `Memoria.md`.
