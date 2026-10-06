# plan.md — 03-08-refresh-logout

1. Mapeo

- Crear: `Dtos/RefreshDtos.cs`, `Validators/RefreshValidators.cs`, `Services/RefreshTokenService.cs`, `Controllers/V1/RefreshController.cs`, `Controllers/V1/LogoutController.cs`.
- Modificar: `Program.cs` (solo DI servicio + 2 validadores), `Memoria.md`, `spec.md` (conciliación + aprobación).

2. Guardarraíles

- Refresh rotado en cada uso; reuso/expirado → `Invalid`/401 sin eco; revocación en logout.
- Blacklist `blacklist:{jti}="revoked"` TTL 120s (`NOTE 04-02`); sin substring `token` en llaves; sin tocar esquema auth (queda para `04-01`).

3. Pruebas

- `UnitTest/RefreshToken/RefreshTokenServiceTests.cs` (9).
- `IntegrationTest/Refresh/RefreshControllerTests.cs` (5, `TestAuthHandler`).
- `SecurityTest/Refresh/RefreshSecurityTests.cs` (3).

4. Secuencia

1. Service.
2. Controllers.
3. Blacklist Redis.
4. Tests.
