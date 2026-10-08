# 03-09 — Two Factor

## T1 — Setup/verify TOTP (real, cierra NOTE 03-09)
- **Crear**: `TwoFactorController`, `TwoFactorService`, OtpNet `ITotpService` real (ventana ±1), DTOs (`TwoFactorSetupRequest/Response`, `TwoFactorVerifyRequest/Response`), `TwoFactorSetupRequestValidator`, `TwoFactorVerifyRequestValidator`, `stryker-0309.json`.
- **Modificar**: `SegUsuario` (uso `str2FASecreto`/`bln2FAHabilitado`, setup no habilita hasta verify), `Program.cs` (DI real, retirar `FakeTotpService`).
- **Verificar**: setup Bearer → 200 `otpauth://` + secreto (solo una vez, cifrado en reposo, nunca en logs/caché); verify TOTP válido → habilita 2FA; código malo/replay → 401 idéntico; N fallos → 423; re-setup regenera e invalida anterior.
- **Diferidos con NOTE**: JWT real → `04-01`; TTL/lockout real → `04-02`; rate limiting 429 → `04-04`.
