---
name: 03-auth-endpoints
description: Login, 2FA, refresh, logout con anti-enumeration y lockout
---

## Propósito

Login, 2FA, refresh, logout.

## Cuándo usarla

`03-06`…`03-09` (alcance opaco acotado con usuario + `NOTE`s, JWT real en `04-01`).

## Pasos

LoginService anti-enumeración (mismo body 401 + verify dummy `_dummyHash`) + lockout (`attempts:{nombre}` TTL 120s = máx `CacheService`, `lockout:{nombre}` al 5º fallo; 1–5→401, siguiente→423; `NOTE 04-02` 15min); token opaco 32B `RandomNumberGenerator` (`NOTE 04-01`, no JWT); 2FA temp hex 64 (no Base64: `token` prohibido en llaves) en `cache:login2fa:{hex}` TTL 120s + `FakeTotpService 123456` (`NOTE 03-09`); refresh rotación vía `strReplacedByTokenHash` + blacklist `blacklist:{jti}` TTL 120s; logout fallback `jti ?? hash(refresh)`.
Doubles de test: `RecordingHasher` (fija semilla dummy), `RecordingTotp`.
TOTP real `03-09` (ver `phases/04-totp-provisioning`): `OtpNetTotpService` (ventana ±1, `GenerateRandomKey(20)`) + `TwoFactorSecretProtector` (DataProtection `"TwoFactor"`) + `ITotpProvisioner` nueva con Fake intacto; llave `2fa:{userId}` + `attempts:/lockout:`; `SetupAsync→null` sin try/catch (canónico `03-16`); `RemoveAsync lockout` en success eliminado; waiver critic `Secret` enrollment 1 vez; Stryker `93.41%` (`stryker-0309.json`, `-f`).

## Checklist

401 idénticos sin fugas; lockout 5→423; temp un solo uso (replay 401); gate hex/`IsHexToken`; secreto/password jamás en llaves/logs/DTOs; `429` diferido (`NOTE 04-04`).

## Checklist

Rate limit Login/Login2faVerify; Redis lockout; JWT+refresh emitidos.

## Criterios de done

Login 200 con token; credenciales malas 401 sin revelar; 2FA verify OK.

## Límites/trampas

Timing attack; token reutilizado debe fallar.

## Referencias

`03-06`…`03-09`, `04-01`, `04-02`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`.
