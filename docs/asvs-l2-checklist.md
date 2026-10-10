# Checklist OWASP ASVS L2 (04-05, canónica)

Fuente única de verdad para estado ASVS L2 por capítulo.
`specs/phase-04-security/04-05-owasp-asvs-l2/spec.md` solo enlaza aquí; no duplica la tabla.
Regla: ningún `Cubierto` sin evidencia resoluble (archivo + test). Ver skill `04-auth-matrix`.

## Estado por capítulo

| Capítulo ASVS | Estado | Evidencia | Deuda / NOTEs |
|---|---|---|---|
| V1 Arquitectura y diseño | Cubierto | Constitución `specs/phase-00-constitution/00-01-principles-stack/spec.md`; pipeline `WebAPIDevSecOpsScallingSDD/Program.cs` (`UseRateLimiter` → `UseAuthentication` → `UseAuthorization`, orden constitucional 04-04); `docs/endpoints.md` (56 rutas) + `scripts/check_endpoints.ps1` OK | — |
| V2 Autenticación | Parcial | JWT HS256 `Services/JwtTokenService.cs` + `UnitTest/Jwt/JwtTokenServiceTests.cs` (5) + `SecurityTest/Jwt/JwtTests.cs` (5: `alg=none`/firma/expirado); Argon2id `Services/SegUsuarioPasswordHasher.cs` + `UnitTest/Login/PasswordHasherTests.cs` (10); lockout DB `Services/LoginLockoutStore.cs` + `UnitTest/Login/LoginLockoutStoreTests.cs` (6); anti-enumeración `Services/LoginService.cs` + `SecurityTest/Login/AntiEnumerationTests.cs` (3) | `NeedsRehash` sin escritura (`04-02` Límites); temp-2FA 120s vs 5min objetivo |
| V3 Gestión de sesiones | Parcial | Refresh rotado `Services/RefreshTokenService.cs` (`strTokenHash` SHA-256 hex, `strReplacedByTokenHash`, 7d) + `UnitTest/RefreshToken/RefreshTokenServiceTests.cs` (9); blacklist `blacklist:{jti}="revoked"` + `OnTokenValidated`; `SecurityTest/Refresh/RefreshSecurityTests.cs` (3) | Blacklist TTL 120s, no 15min (`NOTE (04-02)` en `RefreshTokenService.cs:49`); filas refresh solo DB, sin `cache:refresh` |
| V4 Control de acceso | Cubierto | `AdminPolicy` (rol `Admin`; `AdminOnly` no existe en código) en 16 controllers; ownership `Services/VentaDetalleService.cs:203` (`EnsureOwner` → `ForbiddenAccessException` → 403); matriz `docs/rate-limit-matrix.md` (49 filas) + `SecurityTest/RateLimit/RateLimitTests.cs` (4: estructural reflection toda-action-con-policy) | — |
| V5 Validación y codificación | Cubierto | FluentValidation en frontera (`Validators/*`, `ValidateAsync` → 400 `ValidationProblem` / `{ error }` manual en lecturas); `UnitTest/*/*ValidatorTests.cs`; errores uniformes `Middleware/ExceptionHandlingMiddleware.cs` + `UnitTest/Errors/ExceptionHandlingMiddlewareTests.cs` | Outlier documentado: FK `EmpEmpleado` → 422 desde servicio, no 400 |
| V6 Criptografía | Cubierto | Argon2id 64MB/3 iter `Services/SegUsuarioPasswordHasher.cs` (formato PHC `$argon2id$v=19$…`); fallback BCrypt solo `Verify $2a$/$2b$` migración; JWT HS256 key ≥32B solo env (`Jwt:Key`, `ClockSkew=Zero`, `ValidAlgorithms=[HS256]`); `strPWD nvarchar(200)` (PHC 85–120 chars) | Sin Argon2id débil; sin secretos en repo (ver `critic-guardrails.ps1` PASS) |
| V7 Errores, logs y auditoría | Parcial | `Middleware/ExceptionHandlingMiddleware.cs` (`ErrorResponse { Error, Status, TraceId }`, `Detail` solo no-prod, tokens hex nunca en bodies); Serilog base | `audit hash chain` es fase 08 pendiente; OTel/Prometheus fuera de alcance |
| V8 Protección de datos | Cubierto | Password nunca en caché/logs: guardarraíl `Services/CacheService.cs` (prefijos `blacklist:/attempts:/lockout:/cache:`, `token` prohibido salvo `blacklist:{jti}`) + `SecurityTest/Cache/NoLeakTests.cs` (4) + `SecurityTest/Cache/LockoutTests.cs` (4); DTOs sin secretos (`SegUsuarioDto`, `LoginRequest`) | — |
| V9 Comunicaciones | Parcial | HSTS 365d solo `MaxAge` no-Dev (`Program.cs` `AddHsts`/`UseHsts`); TLS vía `HttpsRedirection` + `ForwardedHeaders`; CORS single-origin (`01-02`) | HSTS en wire no verificable con `WebApplicationFactory` (solo aserción `HstsOptions.MaxAge=365d` en factory `Production`); sin `IncludeSubDomains`/preload por decisión |
| V14 Configuración y despliegue | Cubierto | `Middleware/SecurityHeadersMiddleware.cs` (nosniff/DENY/referrer/`X-XSS-Protection: 0`, CSP nonce 16B por request, exención `scalar`/`openapi`) + `UnitTest/Middleware/SecurityHeadersTests.cs` (10) + `SecurityTest/Headers/HeaderTests.cs` (3); `Services/AssemblyIntegrityCheck.cs` + `SecurityTest/Startup/AssemblyIntegrityTests.cs`; rate-limit 5 policies `Services/RateLimitOptions.cs` + `UnitTest/RateLimit/RateLimitOptionsTests.cs` (4) | `PERF_*` solo perf, nunca prod |

## Exclusiones (sin ASVS, documentado el porqué)

| Superficie | Motivo |
|---|---|
| `GET /api/v1/ping`, `/health`, `/health/ready` | sondas vida/orquestador; 429 aquí = reinicios en cascada (ver `04-04`) |
| `/api/v1/probe/*`, `POST /provider-states` | solo no-prod tras `EnableProviderStates`; herramientas de test |
| L3 | Fuera de alcance (`spec.md` Límites: solo L2) |

## Evidencia de suites (10-Oct-2026)

- `UnitTest` 330/330, `SecurityTest` 81/81, `IntegrationTest` 99/99 (92+7 Docker-only CI), `ContractTest` 4/4, `DatabaseTest` 2/2 (estado heredado de `04-04` ✅; sin código nuevo en `04-05`).
- `scripts/critic-guardrails.ps1` PASS; `scripts/check_endpoints.ps1` OK (56 rutas).
- Stryker slices: `04-02` 90.85%, `04-03` 100.00%; `04-01` → nightly (tope local 25min).
