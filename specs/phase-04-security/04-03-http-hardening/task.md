# 04-03 — HTTP Hardening (tareas, delta sobre 01-02/03-16 ✅)

> Base ya hecha en `01-02` (pipeline + CORS + HSTS no-Dev) y `03-16` (`ExceptionHandlingMiddleware` + `ForbiddenAccessException`→403 + sondas `probe/*`). Ejecutado 10-Oct-2026 (ver **Ejecutado** en cada T). Skills: `phases/04-security-core`, `phases/03-errors-middleware`, `operations/critic-guardrails`, `operations/drift-guards`.

## T1 — SecurityHeadersMiddleware único (Req 1)
- **Crear**: `Middleware/SecurityHeadersMiddleware.cs` — `public sealed` (`NonceItemKey="CspNonce"`, `ThrowIfNull`, `ConfigureAwait(false)`, interpolación): 4 headers siempre + CSP con nonce (`RandomNumberGenerator.GetBytes(16)` → Base64, `Items["CspNonce"]`); exención `scalar`/`openapi` (`StartsWithSegments` `OrdinalIgnoreCase`, early-exit sin nonce); set síncrono antes de `_next` (sin `OnStarting`: `DefaultHttpContext` no lo dispara y el outer ya sobrevive a `WriteErrorAsync`).
- **Modificar**: `Program.cs` — `app.UseMiddleware<SecurityHeadersMiddleware>()` lo más externo absoluto (antes de `DeveloperExceptionPage`; cubre 403/500; orden `01-02` aditivo + documentado en `spec.md Diseño`).
- **No tocar**: CORS, `ExceptionHandlingMiddleware`, Assembly Integrity, `docs/endpoints.md`.
- **Ejecutado 10-Oct-2026**: build 0/0 tras T1+T2.
- **Verificar**:
  - `dotnet build -c Release --no-restore` → 0 errores.
  - `dotnet test UnitTest -c Release --no-build --filter SecurityHeaders` → 10/10.

## T2 — HSTS 365d (Req 2)
- **Modificar**: `Program.cs` — `services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365))` (sin `IncludeSubDomains`/preload); `app.UseHsts()` intacto con gate no-Dev (esta versión no expone overload con `Action<HstsOptions>`).
- **Ejecutado 10-Oct-2026**: `SecurityTest --filter HeaderTests` 3/3 (HSTS por `HstsOptions` en factory `Production`: TestServer siempre http, emisión wire es framework).
- **Verificar**: `dotnet test SecurityTest -c Release --no-build --filter HeaderTests` → 3/3.

## T3 — Tests (Req 3)
- **Crear**: `UnitTest/Middleware/SecurityHeadersTests.cs` (10) + `SecurityTest/Headers/HeaderTests.cs` (3: `/health` con headers+CSP; 403 sonda `Staging`+`EnableProviderStates` con headers+CSP; `HstsOptions` 365d en `Production`).
- **No tocar**: ownership 403 (`VentaDetalleService.cs:203` + sonda existentes bastan).
- **Ejecutado 10-Oct-2026**: `UnitTest` 326/326; `SecurityTest` 77/77.
- **Verificar**:
  - `dotnet test UnitTest -c Release --no-build` → 326/326.
  - `dotnet test SecurityTest -c Release --no-build` → 77/77.
  - `dotnet test IntegrationTest -c Release --no-build` → 92/99 + 7 Docker-only (sin daemon, igual que `04-02`).
  - `dotnet test ContractTest -c Release --no-build` → 4/4.

## T4 — Stryker + gates (igual que 04-02)
- **Crear**: `stryker-0403.json` (mutate solo `SecurityHeadersMiddleware.cs`; `Program.cs` excluido — ruido; `test-projects: [UnitTest]`, `test-case-filter: FullyQualifiedName~UnitTest.`, `ignore-mutations: ["Boolean"]`, break 80).
- **Ejecutado 10-Oct-2026**: Stryker local → **100.00%** (18 mutantes) + `dotnet build` restaurativo (§4, binarios mutantes) + re-test filtro verde.
- **Verificar**:
  - `powershell -File scripts/critic-guardrails.ps1` → `PASS` exit 0.
  - `powershell -File scripts/check_endpoints.ps1` → `OK` exit 0 (56 rutas, sin rutas nuevas).
  - `@security-reviewer` pre-push verde; Stryker ≥80.
- **Pendiente →** `04-04` (rate-limit + matriz auth), `04-05` (ASVS L2), Serilog/OTel (fase 08).
