# plan.md — 03-07-login-2fa

1. Mapeo

- Crear: `Dtos/Login2FaDtos.cs`, `Validators/Login2FaValidators.cs`,
  `Services/TotpService.cs` (`ITotpService` + `FakeTotpService`),
  `Services/Login2FaService.cs`, `Controllers/V1/Login2FaController.cs`,
  `stryker-0307.json`.
- Modificar: `Dtos/LoginDtos.cs` (`Requires2fa`, `TempToken?`),
  `Services/LoginService.cs` (rama `RequiresTwoFactor` + emisión temp),
  `Controllers/V1/LoginController.cs` (mapeo 200 2FA), `Program.cs` (DI),
  `stryker-0306.json` intacto (re-run por rama nueva en `LoginService`).

2. Guardarraíles

- Temp opaco hex 64 (no Base64) en `cache:login2fa:{hex}` TTL 120s;
  `NOTE (04-02)` para 5min, `NOTE (04-01)` para JWT `2fa_temp`.
- TOTP fake `123456` con `NOTE (03-09)` para OtpNet ventana ±1.
- Tipos con `2Fa` (Sonar S101); sin `TODO` (S1135 → `NOTE`).
- Sin `ConfigureAwait(false)` en cuerpos `[Fact]` (xUnit1030; solo helpers).
- Nombres `Login2Fa*` anti-colisión + DELETE limpieza (InMemory compartido).

3. Pruebas

- `UnitTest/Login2fa/` (verify + rama requires + fake TOTP).
- `IntegrationTest/Login2fa/` (flujo + 401 idénticos + 423 + 400).
- `SecurityTest/Login2fa/` (401 sin fugas + 400).

4. Secuencia

1. DTOs + validador + `ITotpService` fake.
2. Rama `RequiresTwoFactor` en `LoginService` + `Login2FaService.VerifyAsync`.
3. `Login2FaController` + DI.
4. Tests Unit/Integration/Security.
5. Build 0/0 → suites verdes → Stryker 0307 + re-run 0306 → build
   restaurativo → suites verdes → conciliar spec/plan + Memoria.
