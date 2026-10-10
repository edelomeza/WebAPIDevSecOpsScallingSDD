# plan.md — 04-03-http-hardening (ejecutado 10-Oct-2026)

1. Mapeo

- Crear: `Middleware/SecurityHeadersMiddleware.cs`, `UnitTest/Middleware/SecurityHeadersTests.cs`, `SecurityTest/Headers/HeaderTests.cs`, `stryker-0403.json`.
- Modificar: `Program.cs` (inserción outermost + `AddHsts` 365d), `spec/plan/task.md` de `04-03` (reescritura estilo `04-02`), `Memoria.md`.

2. Guardarraíles

- Un solo middleware (no `CspNonceMiddleware` separada); outermost absoluto; orden `01-02` aditivo.
- HSTS solo `MaxAge` via `AddHsts` (sin `IncludeSubDomains`/preload), gate no-Dev intacto.
- Sin `OnStarting`: set síncrono outer cubre 403/500 y es testeable en `DefaultHttpContext`.
- HSTS wire no testeable en TestServer → assert `HstsOptions` en factory `Production`.
- Sin rutas nuevas (`docs/endpoints.md` intacto); sin tocar `ExceptionHandlingMiddleware`/CORS/Assembly Integrity.

3. Pruebas

- `UnitTest/Middleware/SecurityHeadersTests.cs` (10: 2 nulos, headers+CSP+nonce 16B, unicidad, `Theory` 5 exentas, set-antes-de-`_next`).
- `SecurityTest/Headers/HeaderTests.cs` (3: `/health` con headers, 403 sonda con headers, `HstsOptions` 365d).
- `IntegrationTest` sin cambios (92/99 + 7 Docker-only); `ContractTest` (4).

4. Secuencia (ejecutada)

1. Middleware + `Program.cs` + build 0/0. 2. Unit + Security wire (ajustes: `AddHsts`, S3358, HSTS por opciones). 3. Suites + critic + endpoints + Stryker 100% + build restaurativo + re-test filtro. 4. Docs + Memoria.
