# plan.md — 03-08-refresh-logout

1. Mapeo

- Crear: RefreshController, LogoutController, RefreshTokenService.

2. Guardarraíles

- Refresh rotado en cada uso; revocación en logout.
- Blacklist jti en Redis.

3. Pruebas

- SecurityTest/Refresh/.
- UnitTest/RefreshToken/.

4. Secuencia

1. Service.
2. Controllers.
3. Blacklist Redis.
4. Tests.
