# 03-03 — ProProducto

## T1 — CRUD por entidad
- **Crear**: `ProProductoController`, `ProProductoService`, `ProProductoCreateDto`/`ProProductoUpdateDto`/`ProProductoDeleteDto`, `ProProductoCreateValidator`/`ProProductoUpdateValidator`/`ProProductoDeleteValidator`, tests en `UnitTest/ProProducto/`, `IntegrationTest/ProProducto/`, `SecurityTest/ProProducto/`.
- **Verificar**: CRUD completo; cache invalidación; SecurityTest 401/403; Stryker ≥80%.
- **Guardarraíles**: DTOs validados; no exponer entidad; cache no guarda password.
