# 04-01 — JWT y refresh

## Contexto
Delta JWT real sobre la base opaca ya aprobada en `03-08` ✅ (06-Oct-2026): rotación + revocación + blacklist funcionan con tokens hex 64 temporales. Esta spec introduce la emisión/validación JWT HS256 y reemplaza el stub `AnonymousChallengeHandler` de `03-01`, sin reescribir el slice `03-08`. **Depende de**: `03-08`, `03-01`, `01-04`. **Diferido a**: `04-02` (TTL definitivo), `04-04` (rate-limit), `05-01` (swap a Redis real).

Skills enlazadas (no copiadas): `phases/04-jwt-refresh`, `phases/04-totp-provisioning`, `phases/03-errors-middleware`, `operations/drift-guards`, `operations/critic-guardrails`, `core/traceability`, `core/spec-first-writing`.

## Requisitos
1. Configurar JwtBearer HS256 con key ≥32B solo por env, `ClockSkew=Zero`, `ValidAlgorithms=[HS256]`, validación de `Issuer/Audience`; claims `sub/jti/role/2fa_temp`.
2. Conservar `RefreshTokenService` (`Create/Rotate/Revoke/LogoutAsync`): hash SHA-256 hex en `strTokenHash`, rotación vía `strReplacedByTokenHash`, expiración 7d, `DbUpdateConcurrencyException` → `Invalid` (access ahora JWT vía `IJwtTokenService`; `grep "04-01"` en `Services/RefreshTokenService.cs`).
3. Conservar blacklist `blacklist:{jti}="revoked"` en `ICacheService` TTL 120s (ver `NOTE (04-02)` en `Services/RefreshTokenService.cs:49`); filas refresh solo en DB, sin `cache:refresh`, sin substring `token` en llaves.
4. Mantener endpoints `POST /api/v1/auth/refresh` (`[AllowAnonymous]`) y `POST /api/v1/auth/logout` (`[Authorize]`, fallback `jti=claim ?? hash(refresh)`); catálogo canónico en `docs/endpoints.md` (no duplicar tabla, ver `operations/drift-guards`).

## Diseño
- `Program.cs` (`WebApiServiceCollectionExtensions`, bloque `// (04-01)`): esquema `"Anonymous"` → `JwtBearer` tras flag `Authentication:UseJwtBearer` (default `true`) con `TokenValidationParameters` estrictos + `OnTokenValidated` anti-`blacklist:{jti}`; mantener `AdminPolicy` y orden `UseAuthentication` → `UseAuthorization` (no tocar pipeline en `UseWebApiDevSecOpsPipeline`).
- No tocar: `Dtos/RefreshDtos.cs`, `Validators/RefreshValidators.cs`, `Services/RefreshTokenService.cs` (lógica), `Controllers/V1/RefreshController.cs`, `Controllers/V1/LogoutController.cs` — salvo reemplazar el `Token` opaco por JWT emitido en `Create/Rotate` (variante swap fake→real de `slice-scaffolder`: `grep NOTE (04-01)`, re-Stryker).
- Sin `System.IdentityModel` nuevo si se evita (jti como literal, precedente `03-08`); tokens hex nunca en logs/bodies de error (ver `phases/03-errors-middleware`).

## Contratos
- Claves `Jwt:Key/Jwt:Issuer/Jwt:Audience` según tabla canónica `01-04` (placeholder `PLACEHOLDER_HS256_KEY_MIN_32_BYTES`; secreto real solo por env, nunca en Git).
- DTOs sin cambios: `RefreshRequest{RefreshToken}`, `RefreshResponse{Token,RefreshToken}`, `LogoutRequest{RefreshToken}` (convención legacy medida, NO PascalCase puro).
- Redis keys sin cambios: solo `blacklist:{jti}` contiene `token` (excepción única del critic B2).

## Tests
- Reusar verdes `03-08` (ajustado 1 aserción por formato JWT): `UnitTest/RefreshToken/RefreshTokenServiceTests.cs` (9, `CreateReturnsJwtAccessAndHashedRefresh`), `IntegrationTest/Refresh/RefreshControllerTests.cs` (5, `TestAuthHandler`), `SecurityTest/Refresh/RefreshSecurityTests.cs` (3).
- Crear: `UnitTest/Jwt/JwtTokenServiceTests.cs` (5: nulos-ctor, key corta → `InvalidOperationException`, fallback placeholder sin clave, claims `sub/jti/role` + `alg` HS256 + `exp` futuro, jti único) + `SecurityTest/Jwt/JwtTests.cs` (5: `alg=none` → 401, firma adulterada → 401, expirado → 401 sin eco, reúso → 401 sin eco, logout 204 → replay Bearer 401 + refresh revocado 401).
- Medido 09-Oct-2026: UnitTest 294/294, SecurityTest 67/67, IntegrationTest 92/92 (7 Docker-only excluidos local: 2 `CacheFallback` + 4 `ProviderStates` + 1 `RaceCondition`, sin daemon `npipe://./pipe/docker_engine`), ContractTest 4/4.

## Criterios
- `dotnet build -c Release --no-restore` → 0 errores.
- `dotnet test UnitTest -c Release --no-build` → 294/294 verde.
- `dotnet test SecurityTest -c Release --no-build` → 67/67 verde (incluye 5 `JwtTests`).
- `dotnet test IntegrationTest -c Release --no-build` → 92/92 verde sin Docker (más 7 Docker-only solo CI).
- `powershell -File scripts/critic-guardrails.ps1` → `PASS` exit 0.
- `powershell -File scripts/check_endpoints.ps1` → `OK` exit 0 (56 rutas con fila en `docs/endpoints.md`).
- Stryker slice (`JwtTokenService` + `RefreshTokenService`) ≥83.93% → diferido a nightly (tope local 25 min, ver `Desviaciones`).
- Gate Fase 1: Fase A `slice-scaffolder` (test-aserta-diferido) + `security-reviewer` (`edit: deny`, 4 bloqueantes + checks JWT/hash/headers) verdes.

## Límites
- Sin 2FA aquí (ver `03-07`/`03-09`; TOTP real en `phases/04-totp-provisioning`).
- Sin rate-limit 429 aquí (ver `04-04` + matriz auth/rate-limit).
- Sin swap a Redis real aquí (ver `05-01`; `ICacheService` + TTL 120s sigue válido por `NOTE (04-02)`).
- Sin Pact aquí (captura real queda para fase 10; contract por fixtures `03-18`).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario (1 Revisor)
- **Fecha:** 09-Oct-2026
- **Detalle:** T1 ejecutado y mergeado en PR #20 (`1f33c2d`): build 0 + Unit 294/294 + Security 67/67 + Integration 92/92 + Contract 4/4 + `critic PASS` + `endpoints OK` + `@security-reviewer PASS` (0 bloqueantes); Stryker slice → nightly.
- **Desviaciones registradas:** (1) `RefreshTokenServiceTests.CreateReturnsHexPairAndPersistsHashOnly` → `CreateReturnsJwtAccessAndHashedRefresh` (access JWT con 2 `.`, ya no hex 64; refresh sigue hex 64). (2) `nuget.config` sin cambios (`Microsoft.*` + `Microsoft.IdentityModel.*` ya cubren `JwtBearer 10.0.12`; restore verde; bins unificados 8.19.2, MSB3277 solo ruido). (3) `appsettings.json` sin sección `Jwt` (ausente → fallback placeholder en código; secreto real por env en prod). (4) Stryker slice → nightly (corrida acotada superó 25 min local; umbral ≥83.93% pendiente nightly según `AGENTS.md §4`). (5) `LogoutController.cs:39 NOTE (04-01)` conservado (fallback `jti ?? hash` intacto como defensa).
