# plan.md — 04-01-jwt-refresh

Base aprobada: `03-08` ✅ (slice opaco + `NOTE 04-01/04-02`). Este plan es solo el delta JWT (no recrea el slice).

## 1. Mapeo

- **No tocar** (reusar de `03-08`): `Dtos/RefreshDtos.cs`, `Validators/RefreshValidators.cs`, `Services/RefreshTokenService.cs` (lógica hash/rotación/revocación), `Controllers/V1/RefreshController.cs`, `Controllers/V1/LogoutController.cs`, `Models/SegRefreshToken`.
- **Modificar**: `WebAPIDevSecOpsScallingSDD/Program.cs` (`WebApiServiceCollectionExtensions`, hoy L51-52) — sustituir esquema `"Anonymous"` por `JwtBearer` HS256 con `TokenValidationParameters` estrictos; mantener `AdminPolicy` + orden `UseAuthentication` → `UseAuthorization`.
- **Modificar (swap fake→real, variante `slice-scaffolder`)**: `Services/RefreshTokenService.cs:69,100` — `Convert.ToHexString(RandomNumberGenerator.GetBytes(32))` (access opaco, `NOTE 04-01`) → JWT emitido con claims `sub/jti/role/2fa_temp`; `LogoutAsync` fallback `jti ?? hash(refresh)` → `jti` siempre del claim.
- **Crear**: `Services/JwtTokenService.cs` (`IJwtTokenService.CreateAccessToken`; key ≥32B fail-fast, fallback placeholder solo si ausente), `UnitTest/Jwt/JwtTokenServiceTests.cs` (5), `SecurityTest/Jwt/JwtTests.cs` (5, ver §3).
- **Config**: claves `Jwt:Key/Issuer/Audience` + `Authentication:UseJwtBearer` (default `true`) según tabla `01-04` + `appsettings.Example.json` (placeholder en repo, secreto real solo env); `nuget.config` sin cambios (`Microsoft.*` ya mapea `JwtBearer`).

## 2. Guardarraíles

- Key ≥32B por env, nunca en Git; arranque falla si corta (check `security-reviewer` JWT).
- `ValidAlgorithms=[HS256]`; `alg=none` → 401 (no degradación).
- `ClockSkew=Zero`; `ValidateIssuer/ValidateAudience/ValidateLifetime=true`.
- Refresh: SHA-256 hex en `strTokenHash`, rotación vía `strReplacedByTokenHash`, `DbUpdateConcurrencyException` → `Invalid`; reutilizado → 401 genérico sin eco.
- Blacklist: solo `blacklist:{jti}="revoked"` TTL 120s (`NOTE 04-02`); sin `cache:refresh`; sin `token` en otra llave (bloqueante critic B2).
- Sin `System.IdentityModel` nuevo si se evita; tokens nunca en logs/bodies error (`phases/03-errors-middleware`).
- No tocar pipeline `UseWebApiDevSecOpsPipeline` fuera del esquema auth; rate-limit/TTL definitivos quedan en `04-04`/`04-02`; Redis real en `05-01`.

## 3. Pruebas

- Reusar verdes: `UnitTest/RefreshToken/RefreshTokenServiceTests.cs` (9), `IntegrationTest/Refresh/RefreshControllerTests.cs` (5, `TestAuthHandler` + DELETE limpieza), `SecurityTest/Refresh/RefreshSecurityTests.cs` (3).
- Nuevo `UnitTest/Jwt/JwtTokenServiceTests.cs` (5, sin servidor): nulos-ctor, key corta → throw, fallback sin clave, claims+`alg`+`exp`, jti único.
- Nuevo `SecurityTest/Jwt/JwtTests.cs` (5, HTTP contra `Program` real con JwtBearer): (a) `alg=none` → 401, (b) firma adulterada → 401, (c) expirado → 401 sin eco, (d) refresh válido → 200 + reúso → 401 sin eco, (e) logout → 204 + replay Bearer → 401 (`blacklist:{jti}` vía `OnTokenValidated`) + refresh revocado → 401.
- Gates: `scripts/critic-guardrails.ps1 → PASS`, `scripts/check_endpoints.ps1 → OK`, `security-reviewer` pre-push (`edit: deny`), Fase A `slice-scaffolder` con test que aserta cada diferido.

## 4. Secuencia

1. `grep NOTE (04-01)` en `Services/` + `Controllers/V1/` para acotar el swap.
2. JwtBearer options en `Program.cs` (detrás de flag/config si rompe `TestAuthHandler` de Integration).
3. Swap access opaco → JWT en `Create/Rotate` (mantener hash/rotación intactos).
4. `SecurityTest/Jwt/JwtTests.cs` en rojo → verde.
5. Re-verde `UnitTest/RefreshToken` + `IntegrationTest/Refresh` + `SecurityTest/Refresh` + Stryker si toca servicio.
6. `critic + endpoints + security-reviewer`; conciliar `spec/plan/task` + `Desviaciones` + entrada `Memoria.md` (ver `core/traceability` §4-6).
