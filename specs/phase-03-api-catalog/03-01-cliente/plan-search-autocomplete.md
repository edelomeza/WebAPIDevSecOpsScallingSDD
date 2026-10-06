# plan-search-autocomplete.md — 03-01 T2 Addendum

1. Mapeo

- Modificar: `WebAPIDevSecOpsScallingSDD/Dtos/CliClienteDtos.cs`
  (+`CliClienteAutocompleteDto`).
- Modificar: `WebAPIDevSecOpsScallingSDD/Services/CliClienteService.cs`
  (+`SearchByNameAsync`, `AutocompleteAsync` + interfaz).
- Modificar: `WebAPIDevSecOpsScallingSDD/Controllers/V1/CliClienteController.cs`
  (`[HttpGet("search")]`, `[HttpGet("autocomplete")]`, `CancellationToken`,
  `{ error }`).
- Modificar tests: `UnitTest/CliCliente/CliClienteServiceTests.cs`,
  `IntegrationTest/CliCliente/CliClienteControllerTests.cs`,
  `SecurityTest/CliCliente/CliClienteSecurityTests.cs`.
- Modificar: `stryker-0301.json` (ampliar `mutate`).

2. Decisiones

- Mismo flujo; `page/pageSize` sueltos (validación `GetPaged`);
  `texto.Trim()`, `[StringLength(100)]`; `maxResultados [1,50] → 10`.
- Caché versionada 60s `$"..."`; `AsNoTracking` + `OrderBy(id)` + `Contains`;
  `ConfigureAwait(false)`; DTO mínimo.

3. Guardarraíles

- No exponer entidad; no tocar `AdminPolicy`; sin secretos/usings muertos;
  `400` siempre `{ error }`; prefijo `cache:` + `cliente:`; TTL 60s.
- `dotnet build` tras Stryker antes de `--no-build`.

4. Pruebas

- Unit: search pagina/ordena/cachea/trim; autocomplete top-N/bordes/forma.
- Integration (`TestAuthHandler`): 200/400/403 en ambas rutas.
- Security: 2×401 anónimo.
- Stryker ≥80% (objetivo 100%).

5. Secuencia

1. DTO → 2. Servicio → 3. Controller → 4. Unit → Integration → Security →
   build Release → Stryker → 5. Fusionar Detalle en `spec.md` + `Memoria.md`.
