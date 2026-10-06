# 03-01 — Cliente

## Contexto
Primer vertical slice: CRUD de `CliCliente` con validación, caché, auth y concurrencia optimista. **Depende de**: `02-01`, `03-00`, `03-16`, `04-03`, `05-01`.

## Requisitos
1. DTOs `CliClienteDto`/`Create/Update/DeleteDto` (`RowVersion` base64 en Update/Delete).
2. Validadores FluentValidation espejo del modelo.
3. Servicio con cache-aside e invalidación en writes; conflicto de concurrencia → 409.
4. Controller `api/v{version:apiVersion}/clientes` con `[Authorize(Policy="AdminPolicy")]`.
5. Tests unit/integration/security; Stryker ≥80% en el servicio.

## Diseño
- `Dtos/`, `Validators/`, `Services/CliClienteService.cs` (público), `Controllers/V1/CliClienteController.cs`.
- Caché: `cache:cliente:{id}` + `cache:cliente:page:{version}:...` TTL 60s; writes rotan `cache:cliente:version` (sin wildcard en `CacheService`).
- `Program.cs`: policy `AdminPolicy` (rol Admin) + `AnonymousChallengeHandler` (401 sin esquemas; JWT real en fase 04); `UseAuthentication` antes de `UseAuthorization`.
- Paquete `FluentValidation` 12.1.1.

## Contratos
- `GET/POST /api/v1/clientes`, `GET/PUT/DELETE /api/v1/clientes/{id}`; códigos 200/201/204/400/401/403/404/409.
- `PagedResult<CliClienteDto>` con `Items/TotalCount/Page/PageSize`.

## Tests
- `UnitTest/CliCliente/` (servicio, fake de caché, 14 tests).
- `IntegrationTest/CliCliente/` (CRUD, 409, 403 con `TestAuthHandler`).
- `SecurityTest/CliCliente/` (5×401 anónimo).
- Stryker acotado (`stryker-0301.json`, `ignore-mutations Boolean`): 100% (45/45).

## Criterios
- CRUD completo; cache invalidación en writes; tests verdes.

## Límites
- 403 con JWT real diferido a fase 04; rate limiting en `04-04`; middleware de errores en `03-16` (try/catch manual por ahora).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** slice `CliCliente` + Stryker 100%, `AdminPolicy` stub, 401/403.
