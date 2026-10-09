# 03-02 — Empleado

## Contexto
Vertical slice CRUD de `EmpEmpleado` (mismo patrón que `03-01`). **Depende de**: `02-01`, `03-00`, `03-16`, `04-03`, `05-01`.

## Requisitos
1. DTOs `EmpEmpleadoCreateDto/UpdateDto/DeleteDto` (`RowVersion` base64 en Update/Delete).
2. Validadores FluentValidation espejo del modelo (nombre ≤50, CURP ≤18 + regex oficial `^[A-Za-z]{4}[0-9]{6}[HhMm][A-Za-z]{5}[A-Za-z0-9][0-9]$` solo si no vacía, FK válida con doble capa: rango >0 en validator + existencia en servicio → 400).
3. Servicio con cache-aside e invalidación en writes; conflicto → 409.
4. Controller `api/v{version:apiVersion}/empleados` con `[Authorize(Policy="AdminPolicy")]`.
5. Tests unit/integration/security; Stryker ≥80% en el servicio.

## Diseño
- `Dtos/`, `Validators/`, `Services/EmpEmpleadoService.cs`, `Controllers/V1/EmpEmpleadoController.cs`.
- Caché `cache:empleado:{id}` + páginas versionadas TTL 60s (mismo mecanismo que `03-01`).

## Contratos
- `GET/POST /api/v1/empleados`, `GET/PUT/DELETE /api/v1/empleados/{id}`; códigos 200/201/204/400/401/403/404/409.
- `PagedResult<EmpEmpleadoDto>`.

## Tests
- `UnitTest/EmpEmpleado/` (18 tests: servicio, fake de caché, FK nula/válida/inválida), Stryker 100% (51/51, `stryker-0302.json`).
- `IntegrationTest/EmpEmpleado/` (4: CRUD+409, 400 CURP/FK/mismatch, FK desconocida→400, 403 con `TestAuthHandler`).
- `SecurityTest/EmpEmpleado/` (5×401 anónimo).

## Criterios
- CRUD completo; cache invalidación en writes; tests verdes; Stryker ≥80% en el servicio.

## Límites
- JWT real y rate limiting en fase 04; middleware de errores en `03-16`.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 05-Oct-2026
- **Detalle:** slice `EmpEmpleado` + Stryker 100%, `AdminPolicy` stub, 401/403/409, FK doble capa →400, CURP regex. T2 search (addendum fusionado 09-Oct-2026): `SearchAsync` + ruta `search`, ver `spec-search.md` y `docs/endpoints.md`.
