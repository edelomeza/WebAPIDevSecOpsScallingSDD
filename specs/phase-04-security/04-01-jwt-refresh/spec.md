# 04-01 — JWT y refresh

## Contexto
Emisión y rotación de tokens JWT con blacklist. **Depende de**: `04-02`, `05-01`.

## Requisitos
1. JWT HS256 con key ≥32B, `ClockSkew=Zero`, `ValidAlgorithms`; claims `sub/jti/role/2fa_temp`.
2. `RefreshTokenService`: hash SHA-256, rotación y revocación sobre `SegRefreshToken`.
3. Blacklist `blacklist:{jti}` en caché.
4. Endpoints `/auth/refresh` y `/logout`.

## Diseño
- `TokenValidationParameters` estrictos; reemplaza el `AnonymousChallengeHandler` stub de `03-01`.

## Contratos
- Claves `Jwt:Key/Issuer/Audience` (ver 01-04; secreto solo por env).

## Tests
- `SecurityTest/Jwt/JwtTests.cs`, `UnitTest/RefreshToken/RefreshTokenServiceTests.cs`.

## Criterios
- `alg=none` rechazado; token reutilizado falla.

## Límites
- Sin 2FA aquí (ver `03-07`/`03-09`).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
