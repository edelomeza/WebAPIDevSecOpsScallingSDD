# 03-09 — Two Factor

## T1 — Setup/verify TOTP (real, cierra NOTE 03-09) ✅ ejecutado 2026-10-08
- **Crear**: `TwoFactorController` (`api/v1/two-factor`, `[Authorize]`),
  `TwoFactorService` (setup sin parámetro + verify con rate-limit),
  `OtpNetTotpService` (`ITotpService` + nueva `ITotpProvisioner`, ventana ±1),
  `TwoFactorSecretProtector` (DataProtection propósito `TwoFactor`),
  DTOs (`TwoFactorSetupResponse`, `TwoFactorVerifyRequest/Response`), único
  `TwoFactorVerifyRequestValidator` (`^\d{6}$`, sin DTO/validador de setup),
  `stryker-0309.json`.
- **Modificar**: `Login2FaService` (desprotege antes de `Verify`, dummy
  anti-enumeración si falla); `Program.cs` (DI real, `FakeTotpService` retirado
  de prod y fijado solo en tests 03-07); `SegUsuario` sin cambio de esquema
  (payload 155 < 200); `nuget.config` + csproj (`Otp.NET 1.4.1`).
- **Verificar**: setup Bearer → 200 `otpauth://` + secreto (cifrado en reposo,
  nunca en logs/caché); verify válido → habilita 2FA; malo/desconocido → 401
  idéntico; 5 fallos → 423; re-setup regenera e invalida anterior; setup no
  resetea attempts. Unit 275/275, Security 62/62, Integration 6/6 + Login2Fa
  4/4, Stryker 93.41% (break 80).
- **Diferidos con NOTE**: JWT real → `04-01`; TTL/lockout real → `04-02`; rate
  limiting 429 → `04-04`; persistencia DataProtection prod (NOTE en
  `Program.cs`).
