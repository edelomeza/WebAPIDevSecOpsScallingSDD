# 03-02 — EmpEmpleado

## T1 — CRUD por entidad
- **Crear**: `EmpEmpleadoController`, `EmpEmpleadoService`, `EmpEmpleadoCreateDto`/`EmpEmpleadoUpdateDto`/`EmpEmpleadoDeleteDto`, `EmpEmpleadoCreateValidator`/`EmpEmpleadoUpdateValidator`/`EmpEmpleadoDeleteValidator`, tests en `UnitTest/EmpEmpleado/`, `IntegrationTest/EmpEmpleado/`, `SecurityTest/EmpEmpleado/`.
- **Verificar**: CRUD completo; cache invalidación; SecurityTest 401/403; Stryker ≥80%.
- **Guardarraíles**: DTOs validados; no exponer entidad; cache no guarda password.
