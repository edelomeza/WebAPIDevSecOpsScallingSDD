## Checklist PR

- [ ] Build Release 0 errores
- [ ] Tests verdes (--no-build) Unit/Integration/Security
- [ ] Sin secretos (grep Dtos/ + llaves cache; `token` solo en `blacklist:{jti}`)
- [ ] Sin TODO (solo NOTE (XX-YY) -> fase duena en codigo + task.md)
- [ ] Auth explicita (AdminPolicy | [Authorize] | [AllowAnonymous]) + coherencia 03-17 + 401 identicos
- [ ] Llaves cache $"..." + TTL + sin catch nuevo en Controllers (canonico 03-16)
- [ ] Stryker >=80% del slice (report como artefacto o run local) + build restaurativo posterior (si solo nightly, enlazar run)
- [ ] Memoria.md actualizada; spec/03-17 tocada solo si el slice anade endpoint (N/A justificado)
