# 03-02 T2 Addendum — Search (pendiente)

## T2 — Mismo flujo, revisión independiente
- **Modificar**: `Services/EmpEmpleadoService.cs` (+`SearchAsync(texto?, idTipoEmpleado?, page/pageSize)`), `Controllers/V1/EmpEmpleadoController.cs` (`[HttpGet("search")]` + `CancellationToken` + `BadRequest(new { error })`). Sin cambios en `Dtos/` ni `Validators/`.
- **Tests**: `UnitTest/EmpEmpleado/` (nombre/apellido/combinado/tipo-solo/sin-filtros/trim/vacío/caché), `IntegrationTest/EmpEmpleado/` (200/400/403), `SecurityTest/EmpEmpleado/` (1×401).
- **Verificar**: `dotnet build -c Release --no-restore` 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` verdes; Stryker ≥80%; `dotnet build` tras Stryker.
- **Guardarraíles**: sin `QueryParams`; `Contains` en 3 campos (no CURP); `id<=0` → 400; llaves interpoladas TTL 60s; `ConfigureAwait(false)`.
- **Cierre**: fusionar Detalle en `spec.md` principal, actualizar `Memoria.md`.
