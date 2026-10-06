# plan-search.md — 03-02 T2 Addendum (Search)

1. Mapeo

- Modificar: `WebAPIDevSecOpsScallingSDD/Services/EmpEmpleadoService.cs`
  (+`SearchAsync` + `IEmpEmpleadoService`).
- Modificar: `WebAPIDevSecOpsScallingSDD/Controllers/V1/EmpEmpleadoController.cs`
  (`[HttpGet("search")]`, `texto` + `idTipoEmpleado` + `page/pageSize`,
  `CancellationToken`, `{ error }`).
- Sin cambios en `Dtos/` ni `Validators/`.
- Modificar tests: `UnitTest/EmpEmpleado/EmpEmpleadoServiceTests.cs`,
  `IntegrationTest/EmpEmpleado/EmpEmpleadoControllerTests.cs`,
  `SecurityTest/EmpEmpleado/EmpEmpleadoSecurityTests.cs`.
- Modificar: `stryker-0302.json` (ampliar `mutate`).

2. Decisiones

- Mismo flujo; `page/pageSize` sueltos (validación `GetPaged`);
  `texto` `[StringLength(50)]` + `Trim()` + `Contains` en
  nombre/apellidos (OR); tipo como AND; `id<=0` → 400; id inexistente → vacío.
- Sin filtros = todo paginado (no 400).
- Caché versionada 60s `$"..."`; `AsNoTracking` + `OrderBy(id)`;
  `ConfigureAwait(false)`.

3. Guardarraíles

- No exponer entidad; no tocar `AdminPolicy`; sin secretos/usings muertos;
  `400` siempre `{ error }`; prefijo `cache:` + `empleado:`; TTL 60s.
- No buscar por CURP; no chequeo de existencia del tipo en lectura.
- `dotnet build` tras Stryker antes de `--no-build`.

4. Pruebas

- Unit: nombre/apellido/combinado/tipo-solo/sin-filtros/trim/vacío/cache.
- Integration (`TestAuthHandler`): 200/400 (`id=0`, `page=0`)/403.
- Security: 1×401 anónimo.
- Stryker ≥80% (objetivo 100%).

5. Secuencia

1. Servicio + interfaz → 2. Controller → 3. Unit → Integration → Security →
   build Release → Stryker → 4. Fusionar Detalle en `spec.md` + `Memoria.md`.
