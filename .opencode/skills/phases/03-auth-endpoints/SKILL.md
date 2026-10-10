---
name: 03-auth-endpoints
description: Login, 2FA, refresh, logout con anti-enumeration y lockout
---

## Propósito

Login, 2FA, refresh, logout.

## Cuándo usarla

`03-06`…`03-09` (alcance opaco acotado con usuario + `NOTE`s, JWT real en `04-01`).

## Pasos

LoginService anti-enumeración (mismo body 401 + verify dummy `_dummyHash`) + lockout persistente `04-02` (tabla `SegBloqueo` 5→15min vía `ILoginLockoutStore`/`TimeProvider`; 1–5→401, siguiente→423; `Login2Fa/TwoFactor` conservan `attempts:/lockout:` en caché); hasher real `Argon2IdSegUsuarioPasswordHasher` (Fake intacto solo tests); JWT real `04-01` (`JwtTokenService.CreateAccessToken` `sub/jti/role` literal — `ClaimTypes.Role` no sobrevive outbound — HS256 exp 15min key≥32B fail-fast; flag `Authentication:UseJwtBearer` + `TokenValidationParameters` estrictos + `OnTokenValidated` anti-`blacklist:{jti}`; refresh/login opacos conservan `NOTE (04-01)` donde aplica); 2FA temp hex 64 (no Base64: `token` prohibido en llaves) en `cache:login2fa:{hex}` TTL 120s + `FakeTotpService 123456` (`NOTE 03-09`); refresh rotación vía `strReplacedByTokenHash` + blacklist `blacklist:{jti}` TTL 120s; logout fallback `jti ?? hash(refresh)`.
Doubles de test: `RecordingHasher` (fija semilla dummy), `RecordingTotp`.
TOTP real `03-09` (ver `phases/04-totp-provisioning`): `OtpNetTotpService` (ventana ±1, `GenerateRandomKey(20)`) + `TwoFactorSecretProtector` (DataProtection `"TwoFactor"`) + `ITotpProvisioner` nueva con Fake intacto; llave `2fa:{userId}` + `attempts:/lockout:`; `SetupAsync→null` sin try/catch (canónico `03-16`); `RemoveAsync lockout` en success eliminado; waiver critic `Secret` enrollment 1 vez; Stryker `93.41%` (`stryker-0309.json`, `-f`).

## Checklist

401 idénticos sin fugas; lockout 5→423; temp un solo uso (replay 401); gate hex/`IsHexToken`; secreto/password jamás en llaves/logs/DTOs; rate-limit vigente `04-04` (`Login`/`Login2faVerify`/`Global`/`Admin`, ver `core/auth-matrix`).

## Criterios de done

Login 200 con token; credenciales malas 401 sin revelar; 2FA verify OK.

## Límites/trampas

Timing attack; token reutilizado debe fallar.

## Referencias

`03-06`…`03-09`, `04-01`, `04-02`, `04-04`, `phases/04-jwt-refresh`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`, `core/auth-matrix`.
