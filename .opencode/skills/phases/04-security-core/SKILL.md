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
Realidad vigente `04-02`: hasher `Argon2IdSegUsuarioPasswordHasher` (`Konscious.Security.Cryptography.Argon2 1.3.1`, 64MB/3iter, `DegreeOfParallelism=Min(4,CPU)` interno, PHC `$argon2id$v=19$...`) + `BCrypt.Net-Next 4.0.3` solo `Verify`; interfaz `+= NeedsRehash` (Fake `=>false`); `PasswordHasherOptions` (`PasswordHasher:` sin secretos); DI factory; lockout opción (b) tabla `SegBloqueo` (`strNombre` único, `intIntentosFallidos`, `dteBloqueoHasta`, migración `SegBloqueo0402` sin `_` por CA1707) + `ILoginLockoutStore`/`EfLoginLockoutStore` (`TimeProvider`, 1 reintento `DbUpdateConcurrencyException` fail-closed, rearme limpio) — bloquear por nombre cubre desconocidos; fuera de la caché; `Login2Fa/TwoFactor`/blacklist siguen en `attempts:/lockout:/blacklist:` 120s; rehash-escritura diferida (`AsNoTracking`+`RowVersion`); `strPWD nvarchar(200)` basta (PHC ≤120c); seed placeholder/fake ⇒ reset password fail-closed; `Verify` fail-closed con `catch when` ante hashes corruptos (nunca 500); Stryker `stryker-0402.json` **90.85%** (142 mutantes, 13 equivalentes) con `test-case-filter FullyQualifiedName~UnitTest.` (5min vs 75min sin filtro); temp 2FA hex 64 `ToHexString` (no Base64: evita `password/secret/token` de `CacheService`) en `cache:login2fa:{hex}` TTL 120s + gate `IsHexToken`; JWT real en `04-jwt-refresh`; TOTP real en `phases/04-totp-provisioning` (OtpNet ±1, DataProtection, `2fa:{userId}`, `SetupAsync→null`, waiver `Secret`).
Realidad `04-03`: `SecurityHeadersMiddleware` único lo más externo (`NonceItemKey="CspNonce"`, 4 headers siempre + CSP con nonce 16B Base64, exención `scalar`/`openapi` early-exit sin nonce; antes de `DeveloperExceptionPage`, cubre 403/500; sin `CspNonceMiddleware` separada ni `OnStarting` — `DefaultHttpContext` no lo dispara y el set síncrono outer ya sobrevive a `WriteErrorAsync`) + HSTS 365d solo `MaxAge` vía `AddHsts` (`UseHsts()` sin overload con options; gate no-Dev, sin `IncludeSubDomains`/preload); HSTS en wire no testeable en TestServer (siempre http; `X-Forwarded-Proto` no aplicado por `RemoteIpAddress` ante `ForwardedHeadersOptions` inline) → assert `HstsOptions` en factory `Production`; Stryker `stryker-0403.json` **100%** (18 mutantes) + `dotnet build` restaurativo.
Realidad `04-04/04-05`: rate-limit vigente + ASVS canónico — ver `core/auth-matrix` (no se duplica aquí).

## Checklist

`ValidAlgorithms`; lockout 5→15min; CORS única; assembly integrity.

## Criterios de done

alg=none rechazado; headers presentes; 403 en ownership.

## Límites/trampas

Key corta rompe startup; HSTS solo prod; `S101` exige `Argon2Id` (D mayúscula); `S1135` caza `todo` en minúsculas dentro de comentarios (redecir "cada hash…"); `dotnet ef` sin factory falla (`DbContext` sin `OnConfiguring`) → factory temporal + borrar; `HstsOptions` vive en `Microsoft.AspNetCore.HttpsPolicy` (no `Builder`).

## Referencias

`04-01`…`04-05`, `core/auth-matrix`, `phases/04-jwt-refresh`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`, `stryker-0402.json`, `stryker-0403.json`.
