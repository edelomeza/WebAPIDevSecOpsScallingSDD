---
name: 04-jwt-refresh
description: Emisión y rotación de JWT con refresh tokens y blacklist
---

## Propósito

Emisión y rotación de tokens.

## Cuándo usarla

Al tocar auth de tokens.

## Precondiciones

`04-02`, `05-01` definidos.

## Pasos

1. JWT HS256 con clave ≥32 bytes (fail-fast si ausente; fallback placeholder solo para no romper tests), `ClockSkew=Zero`, `ValidAlgorithms`; claims `sub/jti/role` — emitir literal `"role"` porque `ClaimTypes.Role` no sobrevive al mapa outbound (inbound lo eleva a `Role` para `AdminPolicy`).
2. `RefreshTokenService` con hash SHA-256, rotación, revocación.
3. Blacklist de refresh en Redis `blacklist:{jti}`.
4. Endpoints `/auth/refresh` y `/auth/logout`.
5. Realidad opaca vigente (`03-08`, `NOTE 04-01`): tokens hex 64 (`RandomNumberGenerator`), `strTokenHash` + rotación vía `strReplacedByTokenHash`, expiración 7d, `blacklist:{jti}=revoked` TTL 120s (`NOTE 04-02`), filas refresh solo en DB (sin `cache:refresh`), sin `System.IdentityModel` (jti como literal), logout fallback `jti=claim ?? hash(refresh)`, 2 controllers separados. TOTP real en `phases/04-totp-provisioning` (`2fa:{userId}`, `SetupAsync→null`, waiver `Secret`, Stryker `93.41%`).
6. Delta `04-01` (ya mergeado): `Services/JwtTokenService.cs` (`IJwtTokenService.CreateAccessToken`, exp 15min) inyectado en `Create/Rotate`; `Program.cs` JwtBearer tras flag `Authentication:UseJwtBearer` (default `true`; `false`=Anonymous legacy; Integration/Contract fuerzan `Test` vía `ConfigureTestServices`) con `TokenValidationParameters` estrictos + `OnTokenValidated` anti-`blacklist:{jti}`; paquete `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12`; claves en tabla `01-04` + `appsettings.Example.json` (sin secretos). Tests `UnitTest/Jwt` 5 + `SecurityTest/Jwt` 5 (alg-none/firma/expirado/reúso/logout-blacklist-replay); Stryker del slice → nightly (tope local 25min, umbral ≥83.93%).

## Checklist

`alg=none` rechazado; token reutilizado → 401; logout revoca.

## Criterios de done

`SecurityTest/Jwt/JwtTests.cs` verde; refresh rotado en cada uso.

## Límites/trampas

No loggear tokens; blacklist debe tener TTL; `EndsWith(char)` + `string.Concat/AsSpan` exigidos por CA1865/CA1845; indexer `IConfiguration` con CS8601 → local + ternaria.

## Referencias

`04-01`, `RefreshTokenService.cs`, `Program.cs`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`.
