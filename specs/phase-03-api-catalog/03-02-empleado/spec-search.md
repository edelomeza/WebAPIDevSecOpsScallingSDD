# 03-02 T2 Addendum — Empleado: Search

## Contexto
Addendum de `03-02` (no modifica su T1). Completa el slice `EmpEmpleado` con
la búsqueda combinada omitida (texto en nombre/apellidos + filtro por tipo),
dentro del mismo flujo (mismo controller, servicio, auth `AdminPolicy` y
caché). **Depende de**: `03-02`, `02-01`, `03-00`, `03-16`, `05-01`.

## Requisitos
1. `GET /api/v{version:apiVersion}/empleados/search?texto=...&idTipoEmpleado=...&page=1&pageSize=20`
   → `200 PagedResult<EmpEmpleadoDto>`.
2. Ambos filtros opcionales: `texto` nulo/vacío/blanco se ignora;
   `idTipoEmpleado` nulo se ignora; sin filtros equivale a `GetPaged`.
   `texto` máx 50 (`[StringLength(50)]`, espejo de los campos de nombre).
3. `idTipoEmpleado <= 0` → `400 { error }` (espejo del validator
   `GreaterThan(0)` de writes). Id inexistente pero `> 0` → página vacía
   (`200`, `TotalCount=0`), sin chequeo de existencia.
4. Paginación igual que `GetPaged`: `page < 1 || pageSize < 1 ||
   pageSize > 100` → `400 { error }`. Sin `QueryParams` por decisión
   (coherencia con addendum `03-01` T2).
5. Servicio: `SearchAsync(string? texto, int? idTipoEmpleado, int page,
   int pageSize, CancellationToken)`; `AsNoTracking`, `OrderBy(id)`,
   `texto.Trim()` con `Contains` sobre `strNombre`, `strAPaterno`,
   `strAMaterno` (OR); case-insensitive por collation del proveedor.
6. Caché TTL 60s versionada (mismo mecanismo T1):
   `cache:empleado:search:{version}:{texto}:{tipo}:{page}:{pageSize}`;
   interpolación `$"..."`.
7. Auth heredada de clase `[Authorize(Policy="AdminPolicy")]`.
8. Tests en las mismas carpetas del slice; Stryker ≥80% en lo nuevo.

## Diseño
- Modificar: `Services/EmpEmpleadoService.cs` (+`SearchAsync` + interfaz),
  `Controllers/V1/EmpEmpleadoController.cs` (`[HttpGet("search")]`,
  `[FromQuery] string? texto` + `[FromQuery] int? idTipoEmpleado` +
  `page/pageSize` + `CancellationToken`, `400` como
  `BadRequest(new { error })`).
- Sin cambios en `Dtos/` (reutiliza `EmpEmpleadoDto` + `PagedResult<>`) ni
  en `Validators/` (el `400` por `id<=0` se valida inline en controller,
  igual que la paginación de `GetPaged`).
- Rango `idTipoEmpleado` no se verifica contra catálogo en lectura
  (a diferencia de `EnsureTipoEmpleadoExistsAsync` en writes → `400`).
- Paquetes: sin cambios.

## Contratos
- `GET /api/v1/empleados/search?texto=ana&idTipoEmpleado=2&page=1&pageSize=20`
  → `200 { Items, TotalCount, Page, PageSize }` PascalCase.
- `GET /api/v1/empleados/search` (sin filtros) → `200` todo paginado.
- `400` paginación inválida / `idTipoEmpleado<=0`; `401/403` por `AdminPolicy`.
- CURP no es criterio de búsqueda (no enumerable por texto libre).

## Tests
- `UnitTest/EmpEmpleado/` (fake `ICacheService`): filtra por nombre y por
  apellido, combina texto+tipo (AND), tipo solo, sin filtros = todo paginado,
  `Trim()` aplicado, `idTipoEmpleado` inexistente → vacío, 2ª llamada desde
  caché, llaves/TTL (`cache:empleado:search:…`, 60s).
- `IntegrationTest/EmpEmpleado/` (`TestAuthHandler`): 200 con `TotalCount`,
  sin filtros → 200, `idTipoEmpleado=0/-1` → 400, `page=0` → 400,
  `403` rol `User`.
- `SecurityTest/EmpEmpleado/`: anónimo → `401` en `search` (1 test nuevo).
- Stryker (`stryker-0302.json` ampliado, `ignore-mutations: ["Boolean"]`):
  ≥80%, objetivo 100% como en T1 (51/51).

## Criterios
- `dotnet build -c Release --no-restore` → 0/0.
- `dotnet test UnitTest -c Release --no-build` 100% verde.
- `dotnet test IntegrationTest -c Release --no-build` 100% verde.
- `dotnet test SecurityTest -c Release --no-build` 100% verde.
- `GET /api/v1/empleados/search?texto=<nombre-seed>` → 200 con
  `TotalCount>=1`; `?idTipoEmpleado=999999` → 200 con `TotalCount=0`;
  `?idTipoEmpleado=0` → 400 con propiedad `error`.
- Stryker en `EmpEmpleadoService` ≥80%.
- Tras Stryker: `dotnet build` obligatorio antes de cualquier test
  `--no-build` (binarios mutantes en `bin/`, ver `AGENTS.md` §4).

## Límites
- Sin `QueryParams`; sin rate limiting propio (fase `04-04`); sin middleware
  global de errores (fase `03-16`).
- Búsqueda `Contains` simple; case-insensitive solo por collation del
  proveedor (SQL Server CI por defecto; InMemory es case-sensitive: los
  tests usan mismo casing). Sin full-text, sin normalización de acentos,
  sin ranking por relevancia.
- Sin filtro por CURP ni por otros campos; sin ordenamiento configurable
  (fijo `OrderBy(id)`).
- Invalidación solo por rotación de versión en writes (lecturas
  eventualmente consistentes ≤60s).
- `403` con JWT real diferido a fase 04 (stub `AdminPolicy`).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** addendum T2 pendiente revisión; al aprobar, fusionar Detalle
  en `spec.md` principal y registrar en `Memoria.md`.
