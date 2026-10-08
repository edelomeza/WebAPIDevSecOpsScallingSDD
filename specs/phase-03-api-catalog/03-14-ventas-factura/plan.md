# plan.md — 03-14-ventas-factura

1. Mapeo

- Crear: `Dtos/FacturaDtos.cs` (`VenPedidoFacturaResponseDto`), `Services/VentasFacturaService.cs` (`IVentasFacturaService.GetByIdAsync`), `Controllers/V1/VentasFacturaController.cs` (`GET {id:int} api/v1/ventas/factura`, `AdminPolicy`), DI en `Program.cs`, `stryker-0314.json`. `VenPedidoFactura` ya existía (fase 02); `FacturaConsumer`/eventos/folio diferidos a fase 06 (decisión del usuario).
- Tests: `UnitTest/VentasFactura/VentasFacturaServiceTests.cs` (5), `IntegrationTest/Saga/VentasFacturaTests.cs` (3), `SecurityTest/Saga/VentasFacturaSecurityTests.cs` (1×401).

2. Guardarraíles

- Solo lectura: `cache:factura:{id}` TTL 60s, interpolación `$"..."`, `AsNoTracking`, `ConfigureAwait(false)` (no en cuerpos `[Fact]`, xUnit1030); `NOTE (06-02/06-04/04-04)` en servicio, nunca `TODO` (S1135); sin `password/secret/token` en llaves.
- Inserción en Integration vía `AppDbContext` scoped (sin POST); limpieza cliente/producto vía API `DELETE` (store InMemory compartido, serie).

3. Pruebas

- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security|Database>Test -c Release --no-build` 100% verdes.
- Stryker `stryker-0314.json` (`VentasFacturaService`, `ignore-mutations Boolean`) ≥80% + `dotnet build` restaurativo tras cada run.

4. Secuencia

1. DTOs + servicio.
2. Controller + DI.
3. Tests 3 suites.
4. Build + suites + Stryker + restaurativo.
5. Conciliación spec/plan/task + fila `03-17` (ya coincidente) + `Memoria.md`.
