# 03-01 — CliCliente

## T1 — CRUD por entidad
- **Crear**: `CliClienteController`, `CliClienteService`, `CliClienteCreateDto`/`CliClienteUpdateDto`/`CliClienteDeleteDto`, `CliClienteCreateValidator`/`CliClienteUpdateValidator`/`CliClienteDeleteValidator`, tests en `UnitTest/CliCliente/`, `IntegrationTest/CliCliente/`, `SecurityTest/CliCliente/`.
- **Verificar**: CRUD completo; cache invalidación; SecurityTest 401/403; Stryker ≥80%.
- **Guardarraíles**: DTOs validados; no exponer entidad; cache no guarda password.
