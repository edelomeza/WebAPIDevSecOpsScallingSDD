# 03-07 — Login 2FA

## Contexto
Login en 2 pasos sobre `SegUsuario.bln2FAHabilitado`: `POST /api/v1/auth/login`
bifurca a `requires_2fa` + temp opaco cuando el usuario tiene 2FA habilitado;
`POST /api/v1/auth/login2fa/verify` canjea temp + TOTP por token.
**Depende de**: `03-06` (login base + lockout), `03-05` (usuario con flag 2FA),
`03-09` (setup TOTP real, pendiente), `04-01` (JWT real, pendiente),
`04-02` (Argon2id + TTL 5min, pendiente).
Alcance T1 reducido aprobado por el usuario (06-Oct-2026): temp opaco 120s +
TOTP fake + token opaco, con `NOTE` explícito por diferido (mismo patrón que
`03-06`).

## Requisitos
1. `LoginService.AuthenticateAsync` tras password correcta: si
   `user.bln2FAHabilitado` → `RequiresTwoFactor` + `TempCode` hex 64 (32B,
   `RandomNumberGenerator`); si no → `Authenticated` como antes (sin regresión).
2. Temp de un solo uso en caché `cache:login2fa:{hex}` → `strNombre`, TTL 120s
   (= máximo de `CacheService`, `NOTE (04-02)` para 5min). Hex (no Base64) para
   no tropezar con los patrones prohibidos de `CacheService`
   (`password/secret/token`).
3. `POST /api/v1/auth/login2fa/verify` anónimo (`Login2FaController`, ruta
   `api/v{version:apiVersion}/auth` + `[HttpPost("login2fa/verify")]`,
   `[AllowAnonymous]`) con `Login2FaVerifyRequest { TempToken, TotpCode }` y
   `Login2FaVerifyResponse { Token }` (token opaco 32B, `NOTE (04-01)` JWT).
4. Validador `Login2FaVerifyRequestValidator` (`TempToken` NotEmpty ≤128,
   `TotpCode` NotEmpty `^\d{6}$`).
5. `Login2FaService.VerifyAsync` → `Authenticated | InvalidCredentials |
   LockedOut`: temp nulo/vacío/no-hex/desconocido → 401 idéntico (sin tocar
   caché en el gate; verify dummy anti-enumeración en miss); 5 fallos TOTP →
   423 (`attempts:/lockout:` 120s, igual que `03-06`); éxito consume el temp
   (replay → 401) y limpia intentos; password jamás en llaves ni logs.
6. TOTP vía `ITotpService`; `FakeTotpService` determinista (`123456` con
   secreto no vacío, `NOTE (03-09)` para OtpNet ventana ±1).
7. `LoginController` mapea `RequiresTwoFactor` → 200
   `{ Token:"", Requires2fa:true, TempToken }` (`LoginResponse` extendida,
   compatible: `Token` sigue presente).

## Diseño
- `Dtos/Login2FaDtos.cs`, `Dtos/LoginDtos.cs` (`Requires2fa`, `TempToken?`),
  `Validators/Login2FaValidators.cs`, `Services/TotpService.cs`
  (`ITotpService` + `FakeTotpService`), `Services/Login2FaService.cs`
  (`ILogin2FaService.VerifyAsync`),
  `Services/LoginService.cs` (enum +`RequiresTwoFactor`, `TempCode?`, rama de
  emisión tras `RemoveAsync(attempts)`).
- `Controllers/V1/Login2FaController.cs` con helper `ValidateAsync` igual que
  `LoginController`; 401 `Unauthorized(new { error })`, 423 `StatusCode(423)`.
- `Program.cs`: solo DI (`ITotpService`, `ILogin2FaService`, validador). Sin
  `UseRateLimiter` (429 → `04-04`).
- Nombres con `2Fa` (no `2fa`) por Sonar S101.

## Contratos
- `POST /api/v1/auth/login` con 2FA → `200 { Token:"", Requires2fa:true,
  TempToken }` PascalCase / sin 2FA → `200 { Token }` como antes.
- `POST /api/v1/auth/login2fa/verify` → `200 { Token }` / `400` validación /
  `401 { error }` genérico idéntico (temp malo, TOTP malo, replay, usuario
  borrado/deshabilitado) / `423 { error }` (lockout). `429` diferido a `04-04`.

## Tests
- `UnitTest/Login2fa/Login2FaServiceTests.cs` (14: nulos, nulos/vacíos/no-hex
  sin caché incl. 1-solo-vacío anti-`||→&&`, temp desconocido + semilla dummy
  vía `RecordingTotp`, éxito + replay un solo uso + limpia intentos,
  401 idénticos, 5 fallos → 423 + intentos en 0, borrado/deshabilitado/sin
  secreto tras emisión, llaves `attempts:/cache:login2fa:` + TTL 120s,
  `AsNoTracking`, fake TOTP incl. nulos), Stryker 100% (`stryker-0307.json`,
  `ignore-mutations Boolean`, muta `Login2FaService.cs` + `TotpService.cs`).
- `UnitTest/Login2fa/LoginRequires2FaTests.cs` (4: 2FA → `RequiresTwoFactor` +
  temp hex 64 + llave/TTL, sin 2FA → token sin temp, limpia intentos, password
  mala sin temp).
- `UnitTest/Login/LoginServiceTests.cs` intacto (11, usuarios sin 2FA).
- `IntegrationTest/Login2fa/Login2FaControllerTests.cs` (4: flujo 200 +
  `Requires2fa/TempToken` + verify 200, 401 idénticos, 5 fallos → 423, 400
  vacío/malformado; habilita 2FA vía `AppDbContext` scoped + DELETE limpieza).
- `SecurityTest/Login2fa/Login2FaSecurityTests.cs` (2: 401 anónimo sin fugas,
  400).
- Stryker `stryker-0306.json` re-verde 100% (`LoginService` con rama 2FA).

## Criterios
- `dotnet build -c Release --no-restore` → 0/0.
- `dotnet test UnitTest -c Release --no-build` → 150/150 verde.
- `dotnet test IntegrationTest -c Release --no-build` → 47/47 verde.
- `dotnet test SecurityTest -c Release --no-build` → 43/43 verde.
- `dotnet test DatabaseTest -c Release --no-build` → 2/2 verde.
- Login 2FA válido → 200 `Requires2fa:true` + verify `123456` → 200 con
  `Token`; replay del temp → 401; 5 TOTP malos → siguiente es 423.
- Stryker `Login2FaService`+`TotpService` → 100% (87.76% primer run con 6
  supervivientes → 97.96% con 1 lógico `||→&&` → 100% tras casos 1-solo-vacío);
  `LoginService` → 100% (re-run tras rama 2FA).
- Tras Stryker: `dotnet build` obligatorio antes de cualquier test `--no-build`.

## Límites
- Temp opaco 120s (no JWT con claim `2fa_temp`) y token final opaco → `04-01`.
- TOTP fake `123456` (no OtpNet, no ventana ±1) y sin setup → `03-09`.
- Lockout 120s (no 15 min), sin rehash → `04-02`; rate limiting y 429 → `04-04`.
- `403` no aplica (rutas anónimas); `AdminPolicy` intacta.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** T1 reducido con NOTEs (04-01 temp/JWT, 04-02 TTL, 03-09 TOTP, 04-04 429); Stryker 100% x2.
