# 03-04 — VenCatEstado

## T1 — CRUD por entidad
- **Crear**: `VenCatEstadoController`, `VenCatEstadoService`, `VenCatEstadoCreateDto`/`VenCatEstadoUpdateDto`/`VenCatEstadoDeleteDto`, `VenCatEstadoCreateValidator`/`VenCatEstadoUpdateValidator`/`VenCatEstadoDeleteValidator`, tests en `UnitTest/VenCatEstado/`, `IntegrationTest/VenCatEstado/`, `SecurityTest/VenCatEstado/`.
- **Verificar**: CRUD completo; cache invalidación; SecurityTest 401/403; Stryker ≥80%.
- **Guardarraíles**: DTOs validados; no exponer entidad; cache no guarda password.
