# 03-01 T2 Addendum — SearchByName + Autocomplete (pendiente)

## T2 — Mismo flujo, revisión independiente
- **Modificar**: `Dtos/CliClienteDtos.cs` (+`CliClienteAutocompleteDto {id, strNombreCliente}`), `Services/CliClienteService.cs` (+`SearchByNameAsync(page/pageSize)` / `AutocompleteAsync(maxResultados)`), `Controllers/V1/CliClienteController.cs` (`[HttpGet("search")]` / `[HttpGet("autocomplete")]` + `CancellationToken` + `BadRequest(new { error })`).
- **Tests**: `UnitTest/CliCliente/` (paginación, orden, caché, trim, top-N, bordes `0/51 → 10`), `IntegrationTest/CliCliente/` (200/400/403), `SecurityTest/CliCliente/` (2×401).
- **Verificar**: `dotnet build -c Release --no-restore` 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` verdes; Stryker ≥80%; `dotnet build` tras Stryker.
- **Guardarraíles**: sin `QueryParams`; `Contains` basta; llaves interpoladas TTL 60s; sin PII extra; `ConfigureAwait(false)`.
- **Cierre**: fusionar Detalle en `spec.md` principal, actualizar `Memoria.md`.
