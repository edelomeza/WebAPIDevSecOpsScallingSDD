# plan.md — 03-01-cliente

1. Mapeo

- Crear: Controllers/ClienteController.cs.
- Crear: Services/ClienteService.cs, Dto/*.
- Modificar: Program.cs (DI, rate limiting policies).
- Crear: UnitTest/clienteTests.cs, IntegrationTest/clienteTests.cs, SecurityTest/clienteTests.cs.

2. Decisiones

- DTOs Create/Update/Delete, PagedResult, FluentValidation, cache-aside, AdminOnly+AdminPolicy.

3. Guardarraíles

- FluentValidation obligatorio.
- No exponer entidad directamente.
- Cache invalidación en writes.

4. Pruebas

- UnitTest/Cliente/, IntegrationTest/Cliente/, SecurityTest/Cliente/.
- Stryker ≥80% en services.

5. Secuencia

1. Entidad/DTOs/FluentValidation.
2. Service con cache.
3. Controller + auth.
4. Tests.
