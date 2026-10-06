# plan-search-by-name.md — 03-03 T2 Addendum (SearchByName)

1. Mapeo

- Modificar: `WebAPIDevSecOpsScallingSDD/Services/ProProductoService.cs`
  (+`SearchByNameAsync` + `IProProductoService`).
- Modificar: `WebAPIDevSecOpsScallingSDD/Controllers/V1/ProProductoController.cs`
  (`[HttpGet("search")]`, `texto` + `page/pageSize`,
  `CancellationToken`, `{ error }`).
- Sin cambios en `Dtos/` ni `Validators/`.
- Modificar tests: `UnitTest/ProProducto/ProProductoServiceTests.cs`,
  `IntegrationTest/ProProducto/ProProductoControllerTests.cs`,
  `SecurityTest/ProProducto/ProProductoSecurityTests.cs`.
- Modificar: `stryker-0303.json` (ampliar `mutate`).

2. Decisiones

- Mismo flujo; `page/pageSize` sueltos (validación `GetPaged`);
  `texto` requerido `[StringLength(50)]` + `Trim()` + `Contains` en
  `strNombreProducto`; vacío → 400.
- Caché versionada 60s `$"..."`; `AsNoTracking` + `OrderBy(id)`;
  `ConfigureAwait(false)`.

3. Guardarraíles

- No exponer entidad; no tocar `AdminPolicy`; sin secretos/usings muertos;
  `400` siempre `{ error }`; prefijo `cache:` + `producto:`; TTL 60s.
- `dotnet build` tras Stryker antes de `--no-build`.

4. Pruebas

- Unit: filtra/ordena/trim/caché/vacío-sin-coincidencias.
- Integration (`TestAuthHandler`): 200/400 (sin texto, texto > 50, `page=0`)/403.
- Security: 1×401 anónimo.
- Stryker ≥80% (objetivo 100%).

5. Secuencia

1. Servicio + interfaz → 2. Controller → 3. Unit → Integration → Security →
   build Release → Stryker → 4. Fusionar Detalle en `spec.md` + `Memoria.md`.
