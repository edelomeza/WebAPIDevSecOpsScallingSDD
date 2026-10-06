# plan.md — 03-04-estado-venta

1. Mapeo

- Crear: Controllers/V1/VenCatEstadoController.cs.
- Crear: Services/VenCatEstadoService.cs, Dtos/VenCatEstadoDtos.cs, Validators/VenCatEstadoValidators.cs.
- Modificar: Program.cs (DI del slice; rate limiting real en fase 04).
- Crear: UnitTest/VenCatEstado/VenCatEstadoServiceTests.cs, IntegrationTest/VenCatEstado/VenCatEstadoControllerTests.cs, SecurityTest/VenCatEstado/VenCatEstadoSecurityTests.cs.

2. Decisiones

- DTOs Create/Update/Delete, PagedResult, FluentValidation, cache-aside, AdminOnly+AdminPolicy.

3. Guardarraíles

- FluentValidation obligatorio.
- No exponer entidad directamente.
- Cache invalidación en writes.

4. Pruebas

- UnitTest/VenCatEstado/, IntegrationTest/VenCatEstado/, SecurityTest/VenCatEstado/.
- Stryker ≥80% en services (`stryker-0304.json`, medido 100% 43/43).

5. Secuencia

1. Entidad/DTOs/FluentValidation.
2. Service con cache.
3. Controller + auth.
4. Tests.
