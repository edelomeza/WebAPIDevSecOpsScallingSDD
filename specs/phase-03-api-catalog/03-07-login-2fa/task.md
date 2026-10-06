# 03-07 — Login 2FA

## T1 — Login en 2 pasos (alcance reducido con NOTEs, 06-Oct-2026)
- **Crear**: `Login2FaController`, `Login2FaService`, `FakeTotpService`, DTOs
  (`Login2FaVerifyRequest/Response`), `Login2FaVerifyRequestValidator`,
  `stryker-0307.json`.
- **Modificar**: `LoginService` (rama `RequiresTwoFactor` + temp hex),
  `LoginResponse` (`Requires2fa` + `TempToken`), `LoginController` (200 2FA).
- **Verificar**: login 2FA → `Requires2fa:true` + temp hex 120s en
  `cache:login2fa:*`; verify TOTP `123456` → token opaco; replay → 401;
  5 fallos → 423; 401 idénticos (anti-enumeración).
- **Diferidos con NOTE**: temp 5min + JWT `2fa_temp` → `04-01`;
  lockout 15min → `04-02`; TOTP real + setup → `03-09`; 429 → `04-04`.
