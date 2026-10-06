# plan.md — 03-03-producto

1. Mapeo

- Crear: Controllers/V1/ProProductoController.cs.
- Crear: Services/ProProductoService.cs, Dtos/ProProductoDtos.cs, Validators/ProProductoValidators.cs.
- Modificar: Program.cs (DI del slice; rate limiting real en fase 04).
- Crear: UnitTest/ProProducto/ProProductoServiceTests.cs, IntegrationTest/ProProducto/ProProductoControllerTests.cs, SecurityTest/ProProducto/ProProductoSecurityTests.cs.

2. Decisiones

- DTOs Create/Update/Delete, PagedResult, FluentValidation, cache-aside, AdminOnly+AdminPolicy.

3. Guardarraíles

- FluentValidation obligatorio.
- No exponer entidad directamente.
- Cache invalidación en writes.

4. Pruebas

- UnitTest/ProProducto/, IntegrationTest/ProProducto/, SecurityTest/ProProducto/.
- Stryker ≥80% en services (`stryker-0303.json`, medido 100% 45/45).

5. Secuencia

1. Entidad/DTOs/FluentValidation.
2. Service con cache.
3. Controller + auth.
4. Tests.
