# plan.md — 03-02-empleado

1. Mapeo

- Crear: Controllers/V1/EmpEmpleadoController.cs.
- Crear: Services/EmpEmpleadoService.cs, Dtos/EmpEmpleadoDtos.cs, Validators/EmpEmpleadoValidators.cs.
- Modificar: Program.cs (DI del slice; rate limiting real en fase 04).
- Crear: UnitTest/EmpEmpleado/EmpEmpleadoServiceTests.cs, IntegrationTest/EmpEmpleado/EmpEmpleadoControllerTests.cs, SecurityTest/EmpEmpleado/EmpEmpleadoSecurityTests.cs.

2. Decisiones

- DTOs Create/Update/Delete, PagedResult, FluentValidation, cache-aside, AdminOnly+AdminPolicy.

3. Guardarraíles

- FluentValidation obligatorio.
- No exponer entidad directamente.
- Cache invalidación en writes.

4. Pruebas

- UnitTest/EmpEmpleado/, IntegrationTest/EmpEmpleado/, SecurityTest/EmpEmpleado/.
- Stryker ≥80% en services (`stryker-0302.json`, medido 100% 51/51).

5. Secuencia

1. Entidad/DTOs/FluentValidation.
2. Service con cache.
3. Controller + auth.
4. Tests.
