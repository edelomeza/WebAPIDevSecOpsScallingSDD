# 04-03 — Hardening HTTP

## Contexto
Cabeceras de seguridad y HSTS endurecido sobre el pipeline `01-02`. **Depende de**: `01-02`, `03-16` (sondas `probe/*` + `ForbiddenAccessException`→403). Delta aditivo: no reimplementa CORS (`01-02`), ni `ForbiddenAccessException`→403 (actual), ni Assembly Integrity (`01-03`). Del 403 solo el test faltante (403+headers).

## Requisitos
1. `SecurityHeadersMiddleware` único (no `CspNonceMiddleware` separada como decía el plan previo): `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-XSS-Protection: 0` en todas las respuestas sin gate de entorno; CSP con nonce por request (`RandomNumberGenerator` 16B → Base64 en `HttpContext.Items["CspNonce"]`): `default-src 'none'; script-src 'nonce-{n}'; style-src 'nonce-{n}'; frame-ancestors 'none'; base-uri 'none'; object-src 'none'; form-action 'none'`; exención `scalar`/`openapi` (sin CSP ni nonce, resto de headers sí).
2. HSTS 365d solo `MaxAge` (sin `IncludeSubDomains`/preload), manteniendo el gate no-Dev.
3. Tests: `UnitTest/Middleware/SecurityHeadersTests.cs` (10) + `SecurityTest/Headers/HeaderTests.cs` (3); 403 ownership ajeno sigue cubierto por `VentaDetalleService.cs:203` + sonda (sin tocar `ExceptionHandlingMiddleware`).

## Diseño
- Middleware: `Middleware/SecurityHeadersMiddleware.cs` (`public sealed`, `NonceItemKey="CspNonce"`, `ThrowIfNull`, `ConfigureAwait(false)`; interpolación, no `+`); `IsExempt` via `StartsWithSegments("/scalar"|"/openapi", OrdinalIgnoreCase)` con early-exit (sin nonce en `Items`); `GenerateNonce` = `RandomNumberGenerator.GetBytes(16)` → Base64 (16B); set síncrono antes de `_next`.
- Inserción lo más externa absoluta (`Program.cs`, antes de `DeveloperExceptionPage`): así los 403/500 de `ExceptionHandlingMiddleware` también llevan headers. Orden `01-02` intacto (aditivo).
- HSTS: `services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365))` en `AddWebApiDevSecOpsServices`; `app.UseHsts()` sin cambios (esta versión no tiene overload con `Action<HstsOptions>`).
- Sin `OnStarting` a propósito: `DefaultHttpContext` no dispara `OnStarting` (solo servidor real) y el set síncrono outer ya sobrevive a `WriteErrorAsync` (no limpia headers); `OnStarting` dejaría mutantes supervivientes en Stryker.

## Contratos
- Interno: `SecurityHeadersMiddleware.{NonceItemKey, InvokeAsync}`; `HstsOptions.MaxAge=365d` (`HSTS solo no-Dev`). Sin endpoints nuevos.

## Tests
- `UnitTest/Middleware/SecurityHeadersTests.cs` (10: 2 nulos, headers+CSP+nonce Base64-16B, nonce único por request, `Theory` 5 exentas sin CSP/`Items`, headers fijados antes de `_next`).
- `SecurityTest/Headers/HeaderTests.cs` (3: `/health` anónimo con headers+CSP; `/api/v1/probe/forbidden` 403 (`CreateProbeFactory("Staging")` + `EnableProviderStates`, patrón `ErrorHandlingTests.cs:175-194`) con headers+CSP; `HstsOptions` en factory `Production`: `MaxAge=365d`, sin subdominios/preload).

## Criterios
- `dotnet build -c Release --no-restore` → 0 errores; `UnitTest` 326/326 (316 + 10 nuevos); `SecurityTest` 77/77 (74 + 3 nuevos); `IntegrationTest` 92/99 + 7 Docker-only excluidos (sin daemon local, igual que `04-02`, 0 regresiones); `ContractTest` 4/4 (10-Oct-2026).
- Stryker local `stryker-0403.json` (UnitTest, `test-case-filter`, sin Docker): **100.00%** (18 mutantes) + `dotnet build` restaurativo posterior.
- `critic-guardrails` PASS; `check_endpoints` OK (56 rutas, sin rutas nuevas).

## Límites
- HSTS en wire no verificable con `WebApplicationFactory` (TestServer siempre http; `X-Forwarded-Proto` no aplicado por `RemoteIpAddress` ante `ForwardedHeadersOptions` inline): se aserta `HstsOptions` en factory `Production`; la emisión es código del framework.
- Sin `IncludeSubDomains`/preload (decisión); rate-limit (`04-04`) y Serilog/OTel (fase 08) fuera de alcance.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador con evidencia (ver Criterios)
- **Revisores:** —
- **Fecha:** 10-Oct-2026
- **Detalle:** implementado según plan; pendiente 1 revisor + `@security-reviewer` pre-push.
