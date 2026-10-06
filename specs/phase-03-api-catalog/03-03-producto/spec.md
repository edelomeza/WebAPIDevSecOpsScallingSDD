# 03-03 — Producto

## Contexto
Vertical slice CRUD de `ProProducto` (mismo patrón que `03-01`). **Depende de**: `02-01`, `03-00`, `03-16`, `04-03`, `05-01`.

## Requisitos
1. DTOs `ProProductoCreateDto/UpdateDto/DeleteDto` (`RowVersion` base64 en Update/Delete).
2. Validadores FluentValidation espejo del modelo (nombre ≤50, precio ≥0, existencia ≥0).
3. Servicio con cache-aside e invalidación en writes; conflicto → 409.
4. Controller `api/v{version:apiVersion}/productos` con `[Authorize(Policy="AdminPolicy")]`.
5. Tests unit/integration/security; Stryker ≥80% en el servicio.

## Diseño
- `Dtos/`, `Validators/`, `Services/ProProductoService.cs`, `Controllers/V1/ProProductoController.cs`.
- Caché `cache:producto:{id}` + páginas versionadas TTL 60s (mismo mecanismo que `03-01`).

## Contratos
- `GET/POST /api/v1/productos`, `GET/PUT/DELETE /api/v1/productos/{id}`; códigos 200/201/204/400/401/403/404/409.
- `PagedResult<ProProductoDto>`.

## Tests
- `UnitTest/ProProducto/` (14 tests: servicio, fake de caché), Stryker 100% (45/45, `stryker-0303.json`).
- `IntegrationTest/ProProducto/` (3: CRUD+409, 400 precio/existencia/mismatch, 403 con `TestAuthHandler`).
- `SecurityTest/ProProducto/` (5×401 anónimo).

## Criterios
- CRUD completo; cache invalidación en writes; tests verdes; Stryker ≥80% en el servicio.

## Límites
- JWT real y rate limiting en fase 04; middleware de errores en `03-16`.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 05-Oct-2026
- **Detalle:** slice `ProProducto` + Stryker 100%, `AdminPolicy` stub, 401/403/409, precio/existencia ≥0, URL nullable solo longitud.
