---
name: 04-totp-provisioning
description: TOTP real con Otp.NET + DataProtection + enrollment con waiver
---

## Propósito

Provisioning y verificación TOTP real (`03-09` T1): reemplaza el `FakeTotpService 123456` sin romper su contrato en unit.

## Cuándo usarla

Al tocar `api/v1/two-factor`, `ITotpService`/`ITotpProvisioner`, o el enrollment 2FA.

## Precondiciones

`03-06/03-07` (login opaco + temp hex), `04-02` (lockout), `04-01` (secreto en reposo).

## Pasos

1. `OtpNetTotpService` (Otp.NET 1.4.1, `GenerateRandomKey(20)`, ventana ±1) + `ITotpProvisioner` nueva (Fake intacto): Fases A–D con las 5 correcciones del análisis en plan mode (prefijos `attempts:/lockout:` + llave `2fa:{userId}`, `Login2FaService` desprotege antes de `Verify`, setup sin DTO/validador, stubs controlables en unit + TOTP real solo en integración).
2. `TwoFactorSecretProtector` (DataProtection `"TwoFactor"`): el secreto NUNCA va a logs/caché/DB en claro; en enrollment se devuelve UNA vez (flujo TOTP estándar) → waiver critic `Secret` en `TwoFactorDtos.cs:5` documentado en Memoria (tests `DoesNotContain` + protegido en reposo).
3. `TwoFactorService` + `TwoFactorController` (`api/v1/two-factor`): `SetupAsync` retorna `null` ante id inválido/ausente (sin try/catch en controller, canónico `03-16`; `401` idéntico anti-enumeración). `RemoveAsync` de `lockout:` en success ELIMINADO (inaccesible: con lockout verify retorna antes). `new TwoFactorResult{}` en éxito es equivalente conocido (`Enabled=0`). `catch (FormatException)` ELIMINADO (OtpNet solo lanza `ArgumentException`, probado empíricamente).
4. Payload medido `155 < nvarchar(200)` → sin migración.
5. Tests: stubs controlables en unit + TOTP real solo en integración (`TwoFactor 6/6 + Login2Fa 4/4`; Docker ausente → Testcontainers no ejecutables, solo unit). `NOTE (prod)` fusionado a `NOTE (03-09)`.
6. Stryker `stryker-0309.json`: `68.42% → 79.57% → 94.62% → 93.41%` final tras refactor post-critic (`break 80`, 4 runs, `-f|--config-file`; `--config` es `Unrecognized option` en v5). `nuget.config` suma `Otp.*`. Tras cada run: `dotnet build` restaurativo.

## Checklist

- Ventana ±1 probada; secreto protegido en reposo; enrollment 1 vez con waiver registrado.
- `SetupAsync null` sin try/catch; sin `RemoveAsync lockout` en success; sin `catch FormatException`.
- Stryker ≥80% + build restaurativo.

## Criterios de done

`Unit 275/275 (+31)`, `Security 62/62 (+2)`, `Integration TwoFactor 6/6 + Login2Fa 4/4`; Stryker `93.41%`.

## Límites/trampas

- TTL 5min / lockout 15min / JWT `2fa_temp` / TOTP setup real extendido / `429` → diferidos (`NOTE 04-01/04-02/04-04`, `03-09` T2).
- `node -e` para parsear `mutation-report.json` cuando PowerShell enreda la navegación.

## Referencias

`specs/phase-03-api-catalog/03-09-*`, `stryker-0309.json`, `core/deferred-scope-fakes` (swap ejecutado), `phases/03-errors-middleware` (canon null sin try/catch), `operations/critic-guardrails` (waiver), `Memoria.md` (`03-09`).
