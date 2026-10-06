# plan.md — 04-01-jwt-refresh

1. Mapeo

- Crear: Services/RefreshTokenService.cs, Dto/Refresh*.
- Modificar: Program.cs (JwtBearer options, blacklist Redis).

2. Guardarraíles

- Key ≥32 bytes.
- alg=none rechazado.
- ClockSkew=Zero.
- Refresh hasheado SHA-256, rotado.

3. Pruebas

- SecurityTest/Jwt/JwtTests.cs.
- UnitTest/RefreshToken/RefreshTokenServiceTests.cs.

4. Secuencia

1. Service.
2. Options JWT.
3. Blacklist Redis.
4. Tests.
