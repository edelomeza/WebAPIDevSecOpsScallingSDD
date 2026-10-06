# 03-04 — Estado de venta

## Contexto
Vertical slice CRUD del catálogo `VenCatEstado` (mismo patrón que `03-01`, sin `RowVersion`). **Depende de**: `02-01`, `03-00`, `03-16`, `04-03`, `05-01`.

## Requisitos
1. DTOs `VenCatEstadoCreateDto/UpdateDto/DeleteDto` (sin `RowVersion`: el catálogo no implementa `IConcurrenteAuditable`).
2. Validadores FluentValidation espejo del modelo (valor ≤50, descripción ≤200).
3. Servicio con cache-aside e invalidación en writes.
4. Controller `api/v{version:apiVersion}/estados-venta` con `[Authorize(Policy="AdminPolicy")]`.
5. Tests unit/integration/security; Stryker ≥80% en el servicio.

## Diseño
- `Dtos/`, `Validators/`, `Services/VenCatEstadoService.cs`, `Controllers/V1/VenCatEstadoController.cs`.
- Caché `cache:estado-venta:{id}` + páginas versionadas TTL 60s.

## Contratos
- `GET/POST /api/v1/estados-venta`, `GET/PUT/DELETE /api/v1/estados-venta/{id}`; códigos 200/201/204/400/401/403/404.
- `PagedResult<VenCatEstadoDto>`.

## Tests
- `UnitTest/VenCatEstado/` (13 tests: servicio, fake de caché, persistencia cross-contexto), Stryker 100% (43/43, `stryker-0304.json`).
- `IntegrationTest/VenCatEstado/` (3: CRUD sin 409, 400 vacío/mismatch, 403 con `TestAuthHandler`).
- `SecurityTest/VenCatEstado/` (5×401 anónimo).

## Criterios
- CRUD completo; cache invalidación en writes; tests verdes; Stryker ≥80% en el servicio.

## Límites
- JWT real y rate limiting en fase 04; middleware de errores en `03-16`.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** slice `VenCatEstado` (sin RowVersion, sin 409) + Stryker 100%, `AdminPolicy` stub, 401/403, sin unicidad en strValor.
