# plan.md — 03-09-two-factor (ejecutado 2026-10-08, rama `error`)

1. Mapeo

- Crear: `TwoFactorController`, `TwoFactorService`, `OtpNetTotpService`
  (`ITotpProvisioner` nueva), `TwoFactorSecretProtector`, DTOs
  (`TwoFactorSetupResponse`, `TwoFactorVerifyRequest/Response`), único
  `TwoFactorVerifyRequestValidator`, `stryker-0309.json`.
- Modificar: `Login2FaService` (desprotege antes de Verify), `Program.cs` (DI:
  `ITotpService→OtpNet`, DataProtection, protector, provisioner, servicio,
  validador), `SegUsuario` (sin cambios de esquema: payload 155 < 200),
  tests 03-07 (protector + pin Fake en integración), `nuget.config`
  (mapeo `Otp.*`), csproj (`Otp.NET 1.4.1`).

2. Guardarraíles

- Secreto TOTP cifrado en reposo, nunca en logs/caché/llaves (assert
  `DoesNotContain` + `raw != secreto && Unprotect(raw) == secreto`).
- Prefijos caché solo `attempts:`/`lockout:` + llave `2fa:{userId}`;
  MaxAttempts=5, TTL 120s.
- `[Authorize]` + claim `NameIdentifier` → 401 idéntico; 423 genérico.

3. Pruebas

- `UnitTest/TwoFactor/` (stubs controlables; TOTP real con ventana ±1 solo
  donde el desfase es absorbible).
- `IntegrationTest/TwoFactor/` (TOTP real), `SecurityTest/TwoFactor/`
  (anónimo 401 sin fugas).
- Stryker `stryker-0309.json` hasta ≥80 (4 runs → 93.41%).

4. Secuencia (ejecutada con re-verde por fase)

1. Fase A: Otp.NET + protector + provisioner + DI inicial (Fake intacto).
2. Fase B: pivote `Program.cs:75` + `Login2FaService` desprotege + adaptar
   dobles 03-06/03-07 + re-verde (Unit 244, Security 60, Login2Fa 4+2).
3. Fase C: DTOs + `TwoFactorService` + controller + DI final.
4. Fase D: 33 unit + 6 integ + 2 seg + Stryker + cierre SDD.
