# 03-08 — Refresh y logout

## Contexto
Rotación de refresh tokens y revocación con blacklist en logout. **Depende de**: `04-01`, `03-06`.

## Requisitos
1. `POST /api/v1/auth/refresh` con `RefreshRequest/RefreshResponse`; rota el token (emite nuevo, revoca el anterior vía `strReplacedByTokenHash`).
2. `POST /api/v1/auth/logout` (Bearer) con `LogoutRequest`; revoca y registra `jti` en blacklist.
3. Validadores `RefreshRequestValidator`, `LogoutRequestValidator`.

## Diseño
- `Dtos/RefreshDtos.cs` (`RefreshRequest{RefreshToken}`, `RefreshResponse{Token,RefreshToken}`, `LogoutRequest{RefreshToken}`), `Validators/RefreshValidators.cs` (`NotEmpty` + `Max200` x2), `Services/RefreshTokenService.cs` (`IRefreshTokenService.Create/Rotate/Revoke/LogoutAsync`; SHA-256 hex 64 en `strTokenHash`, rotación vía `strReplacedByTokenHash`, expiración 7d con `NOTE (04-01)`, `DbUpdateConcurrencyException` → `Invalid`), `Controllers/V1/RefreshController.cs` (`POST refresh`, `[AllowAnonymous]`) + `LogoutController.cs` (`POST logout`, `[Authorize]`, claim `jti`/`NameIdentifier` o fallback hash del refresh con `NOTE (04-01)`).
- Refresh tokens hasheados en `SegRefreshToken`; blacklist `blacklist:{jti}="revoked"` en `ICacheService` TTL 120s con `NOTE (04-02)` (sin substring `token` en llaves; filas refresh solo en DB).
- `Program.cs`: solo DI (servicio + 2 validadores); sin cambio de esquema auth (queda para `04-01`).

## Contratos
- Refresh → 200/401; logout → 204/401.

## Tests
- `UnitTest/RefreshToken/RefreshTokenServiceTests.cs` (9: ctor-nulos, create-hash-solo, rotación+link, reúso→`Invalid`, desconocido/expirado/no-hex/nulo→`Invalid`, revoke-una-vez, logout-fallback-blacklist+TTL, logout-claim, logout-vacío).
- `IntegrationTest/Refresh/RefreshControllerTests.cs` (5: rota+reúso-401+segunda-rotación-200, desconocido-401-sin-eco, 400, logout-204+luego-401, logout-sin-auth-401; `TestAuthHandler`).
- `SecurityTest/Refresh/RefreshSecurityTests.cs` (3: 401-sin-fugas, 400, logout-anónimo-401).

## Criterios
- `dotnet build -c Release --no-restore` → 0/0.
- `dotnet test UnitTest -c Release --no-build` → 159/159 verde.
- `dotnet test SecurityTest -c Release --no-build` → 46/46 verde.
- `dotnet test IntegrationTest -c Release --no-build` → 52/52 verde.
- `POST /api/v1/auth/refresh` válido → 200 con par nuevo y distinto; reuso del anterior → 401 genérico sin eco; `POST /api/v1/auth/logout` válido → 204 y refresh posterior → 401 con `blacklist:{jti}` en caché.

## Límites
- Emisión JWT en `04-01` (access opaco hex 32B temporal + `NOTE`); TTL definitivo blacklist/refresh y 429 en `04-02`/`04-04`.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** slice `Refresh & Logout` en alcance opaco (rotación + revocación + blacklist `jti` con fallback), verificado build + 3 suites verdes.
