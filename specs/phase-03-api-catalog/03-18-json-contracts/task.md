# 03-18 — JSON Contracts

## T1 — Fixtures por captura real + test de convención (ejecutado `phase03.11`)

- **Crear**: `ContractTest/Fixtures/*.json` (14 fixtures capturados del wire con `CONTRACT_CAPTURE=1`).
- **Crear**: `ContractTest/FixtureCaptureTests.cs` (flujo admin: ping → usuarios/clientes/productos/estados → paged/autocomplete → login → refresh vía `IRefreshTokenService` scoped → pedido → pago → venta → dashboard → 404; sin `CONTRACT_CAPTURE` valida status sin escribir), `JsonContractTests.cs` (existencia + convención de nombres + sin secretos), `Common/ContractAuthHandler.cs`, `xunit.runner.json` en serie.
- **Corregir**: `spec.md` (nombres reales `PagoResponseDto`/`Login2FaVerifyResponse`, convención medida, PactNet/`Login2FaVerifyResponse` diferidos a fase 10).
- **Actualizar**: skill `testing-pact` + `Memoria.md`.

## Verificar

- `dotnet test ContractTest -c Release --no-build` → 4/4 verde.
- `powershell -File scripts/critic-guardrails.ps1` → `PASS exit 0`.
- Regeneración: `$env:CONTRACT_CAPTURE="1"` + `dotnet test --filter FixtureCaptureTests` reescribe los 14 JSON.

## Pendiente → fase 10

- PactNet + verificación provider contra proceso real + fixture `Login2FaVerifyResponse` (requiere TOTP enrolado).
- Estado `🚧 Borrador con evidencia`; `✅ Aprobado` solo si el usuario indica revisor.
