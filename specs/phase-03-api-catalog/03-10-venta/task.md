# 03-10 — Venta

## T1 — Venta síncrona
- **Crear**: `VentaController`, `VentaService`, DTOs, `RaceConditionTests`.
- **Verificar**: stock y venta en misma Tx; 5 POST con `existencia=1` → 1 éxito, 4×409 (reescrito desde 4×400 por decisión 409-todo-conflicto, 06-Oct-2026).

## T2 — Search multifiltro (ejecutado 06-Oct-2026)
- **Modificar**: `Services/VentaService.cs` (+`SearchAsync(clave?, nombreCliente?, inicio?, fin?, page/pageSize)` con JOIN a `CliCliente`), `Controllers/V1/VentaController.cs` (`[HttpGet("search")]` + `CancellationToken` + `BadRequest(new { error })`).
- **Tests**: `UnitTest/Venta/` (clave exacta/nombre-JOIN/rango inicio-fin/AND/sin-filtros/nulos-fecha/caché), `IntegrationTest/Venta/` (200/400 rango invertido/400 paginación/403), `SecurityTest/Venta/` (1×401).
- **Verificar**: `dotnet build -c Release --no-restore` 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` verdes; Stryker ≥80%; `dotnet build` tras Stryker.
- **Guardarraíles**: sin `QueryParams`; clave exacta + nombre `Contains` vía JOIN + rango inicio/fin; `inicio>fin` → 400; fechas nulas toleradas; llaves interpoladas TTL 60s; `ConfigureAwait(false)`.
- **Cierre**: conciliar con archivos reales, actualizar `Memoria.md`.
- **Desviación registrada**: 403 no aplica en search (Bearer sin policy; rol `User` → 200 verificado en test).
