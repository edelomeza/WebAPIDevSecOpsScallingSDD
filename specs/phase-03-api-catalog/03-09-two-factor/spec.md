# 03-09 — Two-factor

## Contexto
Alta y verificación TOTP sobre `SegUsuario`. Cierra el NOTE 03-09 (stub
`FakeTotpService`). **Depende de**: `03-05`, `03-07`. **Implementado en**:
rama `error` (2026-10-08).

## Requisitos
1. `POST /api/v1/two-factor/setup` (Bearer, sin cuerpo) devuelve `otpauth://`
   + secreto en claro una sola vez; lo guarda cifrado en `str2FASecreto` y deja
   `bln2FAHabilitado=false` hasta verify.
2. `POST /api/v1/two-factor/verify` (Bearer, `{"TotpCode":"123456"}`) habilita
   2FA (`bln2FAHabilitado=true`).
3. Validador único `TwoFactorVerifyRequestValidator` (`^\d{6}$`); setup no lleva
   DTO (con `[ApiController]` un DTO vacío devolvería 400).
4. TOTP real Otp.NET (`OtpNetTotpService: ITotpService, ITotpProvisioner`),
   ventana `VerificationWindow(previous: 1, future: 1)`; secreto
   `KeyGeneration.GenerateRandomKey(20)`.
5. Secreto en reposo cifrado con `ITwoFactorSecretProtector` (DataProtection,
   propósito `"TwoFactor"`); nunca en logs/caché/llaves. Medido: payload 155
   caracteres < `nvarchar(200)` — sin migración.
6. Rate-limit verify: prefijos `attempts:`/`lockout:` (los únicos admitidos por
   `CacheService.BuildKey`) + llave `2fa:{userId}` (por Id, sin PII),
   `MaxAttempts=5`, TTL 120s. Solo verify cuenta; setup no resetea attempts.
7. `Login2FaService.VerifyAsync` desprotege antes de verificar (verify dummy
   anti-enumeración si `Unprotect` falla); suites 03-07 fijadas a stub
   determinista en tests.
8. Re-setup regenera e invalida el anterior; código malo vs usuario
   desconocido → 401 idéntico; N fallos → 423.

## Diseño
- `ITotpProvisioner` (nueva, `GenerateSecret`/`BuildOtpAuthUri`) en vez de
  extender `ITotpService`: `FakeTotpService` y `RecordingTotp` intactos.
- `TwoFactorService` depende de `ITotpService` + `ITotpProvisioner` (misma
  instancia OtpNet en prod, stubs en unit).
- Mensajes internos: `SetupAsync` retorna null ante id inválido/ausente (sin
  try/catch en el controlador, canónico 03-16); el controlador mapea null al
  mismo 401 (`Credenciales inválidas.`).
- `TwoFactorStatus` con `Enabled=0` primero: el mutante `new TwoFactorResult{}`
  en el path de éxito es equivalente conocido (documentado en Memoria).
- Success solo limpia `attempts:` (el `lockout:` es inalcanzable ahí: con
  lockout activo verify retorna antes; TTL 120s lo expira).

## Contratos
- `TwoFactorSetupResponse { Secret, OtpAuthUri }`,
  `TwoFactorVerifyRequest { TotpCode }`, `TwoFactorVerifyResponse { Enabled }`;
  200/400/401/423.

## Tests
- `UnitTest/TwoFactor/` (33: servicio 17 con stubs controlables, OtpNet 6,
  protector 6, validador 2 + base).
- `IntegrationTest/TwoFactor/` (6, TOTP real dependiente del tiempo),
  `SecurityTest/TwoFactor/` (2, 401 anónimo sin cuerpo ni fugas).
- `stryker-0309.json`: muta `TwoFactorService.cs` + `OtpNetTotpService.cs` +
  `TwoFactorSecretProtector.cs`, break 80 → **93.41%** (4 runs).

## Criterios
- Setup devuelve otpauth://; verify habilita 2FA; re-setup invalida anterior.
- Unit 275/275, Security 62/62, Integration TwoFactor 6/6 + Login2Fa 4/4.
- Stryker ≥80% (93.41%).

## Límites
- JWT real → `04-01`; TTL/lockout 15min real → `04-02`; rate limiting 429 →
  `04-04` (NOTEs en código).
- DataProtection sin persistencia (keyring efímero, NOTE prod).
- Docker no disponible en este entorno: suites Testcontainers
  (Database/ProviderStates/Race/CacheFallback) no ejecutadas aquí.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario
- **Fecha:** 2026-10-09
- **Detalle:** implementado y mergeado en PR #13; waiver critic `Secret`
  enrollment documentado en Memoria.
