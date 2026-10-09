# 04-01 — JWT & Refresh (tareas, delta sobre 03-08 ✅)

> Base ya hecha en `03-08`: `RefreshTokenService`/DTOs/validators/2 controllers + 9+5+3 tests verdes. No recrear. Skills: `phases/04-jwt-refresh`, `phases/03-errors-middleware`, `operations/critic-guardrails`, `operations/drift-guards`.

## T1 — Emisión y rotación JWT (Req 1-4)
- **Modificar (hecho)**: `WebAPIDevSecOpsScallingSDD/Program.cs` (`WebApiServiceCollectionExtensions`, bloque `// (04-01)`) — `Anonymous` → `JwtBearer` HS256 tras flag `Authentication:UseJwtBearer` (`Jwt:Key` ≥32B por env con fail-fast, `ClockSkew=Zero`, `ValidAlgorithms=[HS256]`, `ValidateIssuer/Audience/Lifetime`, claims `sub/jti/role` + `OnTokenValidated` anti-blacklist); mantiene `AdminPolicy` y orden middleware.
- **Modificar (swap, hecho)**: `WebAPIDevSecOpsScallingSDD/Services/RefreshTokenService.cs` (vecindad `grep "NOTE (04-01)"`) — access opaco → `_jwt.CreateAccessToken` en `Create/Rotate`; conserva hash SHA-256/rotación `strReplacedByTokenHash`/exp 7d/`DbUpdateConcurrencyException→Invalid`; conserva `blacklist:{jti}` TTL 120s (`NOTE (04-02)`), solo DB.
- **Crear**: `WebAPIDevSecOpsScallingSDD/Services/JwtTokenService.cs` + `UnitTest/Jwt/JwtTests.cs` (5) + `SecurityTest/Jwt/JwtTests.cs` (5: `alg=none`→401, firma adulterada→401, expirado→401, reúso→401 sin eco, logout 204 → replay Bearer 401 con blacklist).
- **Ejecutado 09-Oct-2026**: build 0 + Unit 294/294 + Security 67/67 + Integration 92/92 (+7 Docker-only) + Contract 4/4 + `critic PASS` + `endpoints OK`; Stryker slice → nightly.
- **No tocar**: `Dtos/RefreshDtos.cs`, `Validators/RefreshValidators.cs`, controllers, `docs/endpoints.md` (canónico, no duplicar).
- **Verificar**:
  - `dotnet build -c Release --no-restore` → 0 errores.
  - `dotnet test UnitTest -c Release --no-build` → 100% verde.
  - `dotnet test SecurityTest -c Release --no-build` → 100% verde (incluye `JwtTests`).
  - `dotnet test IntegrationTest -c Release --no-build --filter Refresh` → 5/5.
  - `powershell -File scripts/critic-guardrails.ps1` → `PASS` exit 0.
  - `powershell -File scripts/check_endpoints.ps1` → `OK` exit 0.
  - `@security-reviewer` pre-push verde (`edit: deny`).
- **Pendiente →** `04-04` (429 refresh/logout), `04-02` (TTL definitivo), `05-01` (Redis real).
