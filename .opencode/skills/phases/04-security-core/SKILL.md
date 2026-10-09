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
Realidad opaca vigente: lockout `attempts:{nombre}` TTL 120s (máx `CacheService`, `NOTE 04-02` → 15min) + `lockout:{nombre}` al 5º fallo; temp 2FA hex 64 `ToHexString` (no Base64: evita `password/secret/token` de `CacheService`) en `cache:login2fa:{hex}` TTL 120s + gate `IsHexToken`; refresh opaco + logout fallback (ver `04-jwt-refresh`); TOTP real en `phases/04-totp-provisioning` (OtpNet ±1, DataProtection, `2fa:{userId}`, `SetupAsync→null`, waiver `Secret`).

## Checklist

`ValidAlgorithms`; lockout 5→15min; CORS única; assembly integrity.

## Criterios de done

alg=none rechazado; headers presentes; 403 en ownership.

## Límites/trampas

Key corta rompe startup; HSTS solo prod.

## Referencias

`04-01`…`04-03`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`.
