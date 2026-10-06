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

1. JWT HS256 con clave ≥32 bytes, `ClockSkew=Zero`, `ValidAlgorithms`.
2. `RefreshTokenService` con hash SHA-256, rotación, revocación.
3. Blacklist de refresh en Redis `blacklist:{jti}`.
4. Endpoints `/auth/refresh` y `/auth/logout`.

## Checklist

`alg=none` rechazado; token reutilizado → 401; logout revoca.

## Criterios de done

`SecurityTest/Jwt/JwtTests.cs` verde; refresh rotado en cada uso.

## Límites/trampas

No loggear tokens; blacklist debe tener TTL.

## Referencias

`04-01`, `RefreshTokenService.cs`, `Program.cs`.
