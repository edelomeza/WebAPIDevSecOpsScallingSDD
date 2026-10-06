# 03-06 — Login

## Contexto
Endpoint de login con anti-enumeración y lockout. **Depende de**: `03-05`, `04-01`, `04-02`, `04-03`.
Alcance T1 reducido aprobado por el usuario (06-Oct-2026): 401 genérico + lockout +
validación ahora; 429 → `04-04`, JWT real → `04-01`, rehash transparente → `04-02`
(diferidos con `NOTE` explícito en código y Memoria).

## Requisitos
1. `POST /api/v1/auth/login` anónimo (`LoginController`, ruta `api/v{version:apiVersion}/auth`
   + `[HttpPost("login")]`, `[AllowAnonymous]`) con `LoginRequest { strNombre, strPasswordPlano }`
   (identificador `strNombre`, decisión de usuario) y `LoginResponse { Token }` (token opaco
   32B `NOTE (04-01)`, no JWT real).
2. Validador `LoginRequestValidator` (`strNombre` NotEmpty ≤50, `strPasswordPlano` NotEmpty).
3. Respuesta 401 genérica idéntica para usuario inexistente y password mala (assert de
   igualdad en tests) + verify fake contra hash dummy agnóstico al hasher futuro;
   lockout tras 5 intentos (`attempts:{nombre}` contador + `lockout:{nombre}` flag, TTL 120s
   = máximo de `CacheService`, `NOTE (04-02)` para 15 min); 1–5 → 401, siguiente → 423.
4. Password jamás en llaves de caché ni logs.

## Diseño
- `Dtos/LoginDtos.cs`, `Validators/LoginRequestValidator.cs`,
  `Services/LoginService.cs` (`ILoginService.AuthenticateAsync` →
  `LoginResult { Authenticated | InvalidCredentials | LockedOut }`) sobre `SegUsuario` +
  `ISegUsuarioPasswordHasher` fake actual (sin `NeedsRehash`: añadirlo sería código muerto).
- `Controllers/V1/LoginController.cs` con helper `ValidateAsync` igual que los slices previos;
  401 `Unauthorized(new { error })`, 423 `StatusCode(423, new { error })`.
- `Program.cs`: solo DI (`ILoginService` + validador). Sin `LoginPolicy` (la ruta es anónima,
  corrección al `plan.md` original) y sin `UseRateLimiter` (difiere 429 a `04-04`).

## Contratos
- `LoginRequest` → `200 { Token }` PascalCase / `400` validación / `401 { error }` genérico /
  `423 { error }` (lockout). `429` diferido a `04-04` (policy `Login 5/5min`).

## Tests
- `UnitTest/Login/LoginServiceTests.cs` (11: nulos, éxito con token + limpia intentos,
  401 idénticos, blancos/nulos sin tocar caché, 5 fallos → 423 + intentos en 0, desconocido
  también bloquea, llaves `attempts:/lockout:` + TTL 120s, `AsNoTracking`, semilla dummy vía
  `RecordingHasher`), Stryker 100% (`stryker-0306.json`, `ignore-mutations Boolean`).
- `IntegrationTest/Login/LoginControllerTests.cs` (4: flujo 200 + `Token`, 401 idénticos,
  5 fallos → 423, 400; nombres `Login*` anti-colisión + DELETE de limpieza por InMemory
  compartido, `TestAuthHandler` solo para crear/borrar el usuario).
- `SecurityTest/Login/LoginSecurityTests.cs` (2: 401 anónimo sin fugas de password/hash, 400).

## Criterios
- `dotnet build -c Release --no-restore` → 0/0.
- `dotnet test UnitTest -c Release --no-build` → 131/131 verde.
- `dotnet test IntegrationTest -c Release --no-build` → 43/43 verde.
- `dotnet test SecurityTest -c Release --no-build` → 39/39 verde.
- `POST /api/v1/auth/login` válido → 200 con `Token`; password mala e usuario inexistente →
  401 con body idéntico; 5 fallos → siguiente es 423.
- Stryker en `LoginService` → 100% (80.65% primer run, 6 mutantes → 100% tras
  `IsNullOrEmpty`, tests de nulos/bloqueo e `RecordingHasher`).
- Tras Stryker: `dotnet build` obligatorio antes de cualquier test `--no-build`.

## Límites
- JWT real y refresh en `04-01` (token opaco temporal); rate limiting y 429 en `04-04`;
  rehash transparente y lockout de 15 min en `04-02`.
- Interacción con 2FA (`bln2FAHabilitado`) → `03-07`, fuera de este slice.
- `403` no aplica (ruta anónima); `AdminPolicy` intacta.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** slice `Login` en alcance reducido (401 + lockout TTL 120s + validación),
  Stryker 100%, diferidos con NOTE (04-01 token, 04-02 hasher/rehash/15min, 04-04 429).
