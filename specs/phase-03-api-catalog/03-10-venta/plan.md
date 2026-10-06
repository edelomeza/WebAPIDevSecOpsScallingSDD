# plan.md — 03-10-venta

1. Mapeo

- Crear: VentaController, VentaService, DTOs, RaceConditionTests.cs.
- Más (T2): `Services/VentaService.cs` (+`SearchAsync` + interfaz), `Controllers/V1/VentaController.cs` (`[HttpGet("search")]`, 4 filtros + `page/pageSize`, `CancellationToken`, `{ error }`).
- Modificar tests: `UnitTest/Venta/`, `IntegrationTest/Venta/`, `SecurityTest/Venta/` (+ search).
- Crear/ampliar config Stryker del slice (`ignore-mutations: ["Boolean"]`).

2. Decisiones

- `page/pageSize` sueltos (validación de slices); clave exacta `Trim()+==`, nombre `Contains` vía JOIN; rango inicio/fin incluido con `inicio>fin` → 400; sin filtros = todo.
- Caché versionada 60s `$"..."` con fechas en formato round-trip; `AsNoTracking` + `OrderBy(id)`; `ConfigureAwait(false)`.

3. Guardarraíles

- Stock y venta en misma Tx.
- Race condition: 5 POST con existencia=1 → 1 éxito, 4×400.
- Search: no exponer entidades relacionadas completas (solo `VenVentaDto`); `400` siempre `{ error }`; prefijo `cache:` + `venta:`; TTL 60s; tolerar `dteFechaHoraCompra` nula.
- `dotnet build` tras Stryker antes de `--no-build`.

4. Pruebas

- UnitTest/Venta/ (T1 race + T2 filtros/JOIN/rango/caché).
- IntegrationTest/Venta/ (T1 + T2 200/400/403).
- SecurityTest/Venta/ (T1 + 1×401 en `search`).
- Stryker ≥80% en el servicio.

5. Secuencia

1. Tx única.
2. Validación.
3. Race test.
4. Search (servicio → controller → Unit → Integration → Security → Stryker).
5. Conciliar spec/plan con archivos reales y resolver aprobación.
