# plan.md — 03-16-errors-http (ejecutado 2026-10-09, rama `phase03.9`)

1. Mapeo

- Crear: `Middleware/ExceptionHandlingMiddleware.cs`, `Dtos/ErrorResponseDto.cs`,
  `Services/NotFoundException.cs`, `Services/ForbiddenAccessException.cs`,
  sondas probe en `Program.cs`, `stryker-0316.json`.
- Modificar: `Program.cs` (middleware primero; elimina `UseExceptionHandler`
  no-Dev), 8 controllers (purga try/catch + 21 `NotFound()`→throw),
  `VentaDetalleService.EnsureOwner` (→`ForbiddenAccessException`),
  10 factories de integración a `Staging`, test EmpEmpleado FK (400→422),
  `UnitTest/VentaDetalle` (4 asserts →`ForbiddenAccessException`).

2. Guardarraíles

- Un solo middleware; sin try/catch ad-hoc (critic PASS).
- 500 genérico sin stack trace en prod (`Detail` solo no-prod).

3. Pruebas

- `UnitTest/Errors/` (stubs `IHostEnvironment`/`IHttpResponseFeature`).
- `IntegrationTest/Errors/ErrorHandlingTests.cs` (Staging + `EnableProviderStates`;
  `Production` para `Detail` ausente y sonda deshabilitada).
- Stryker `stryker-0316.json` hasta ≥80 (1 run → 100%).

4. Secuencia (ejecutada con re-verde por fase)

1. Fase A: middleware + tipos + pipeline.
2. Fase B: purga + migración 404 + factories Staging + re-verde.
3. Fase C: sondas gated.
4. Fase D: 14 unit + 8 integ + Stryker + cierre SDD.
