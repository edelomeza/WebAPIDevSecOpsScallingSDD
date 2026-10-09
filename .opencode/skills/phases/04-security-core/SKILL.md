---
name: 04-security-core
description: Hashing, JWT, rate limit, headers de seguridad, object-level auth
---

## Propósito

Hashing, JWT, rate limit, headers, object-level auth.

## Cuándo usarla

Fase 4.

## Pasos

PasswordHasherService Argon2id+BCrypt; JWT HS256≥32B; policies; headers+CSP nonce; ForbiddenAccessException→403.
Realidad vigente `04-02`: hasher `Argon2IdSegUsuarioPasswordHasher` (64MB/3iter, PHC propio, BCrypt solo `Verify`, `NeedsRehash` sin escritura) + lockout persistente `SegBloqueo` 5→15min vía `ILoginLockoutStore`/`TimeProvider` (fuera de la caché; `Login2Fa/TwoFactor`/blacklist siguen en `attempts:/lockout:/blacklist:` 120s); temp 2FA hex 64 `ToHexString` (no Base64: evita `password/secret/token` de `CacheService`) en `cache:login2fa:{hex}` TTL 120s + gate `IsHexToken`; JWT real en `04-jwt-refresh`; TOTP real en `phases/04-totp-provisioning` (OtpNet ±1, DataProtection, `2fa:{userId}`, `SetupAsync→null`, waiver `Secret`).

## Checklist

`ValidAlgorithms`; lockout 5→15min; CORS única; assembly integrity.

## Criterios de done

alg=none rechazado; headers presentes; 403 en ownership.

## Límites/trampas

Key corta rompe startup; HSTS solo prod.

## Referencias

`04-01`…`04-03`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`.
