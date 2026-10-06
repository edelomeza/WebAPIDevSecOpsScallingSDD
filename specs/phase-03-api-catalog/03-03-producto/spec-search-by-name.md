# 03-03 T2 Addendum — Producto: SearchByName

## Contexto
Addendum de `03-03` (no modifica su `✅ Aprobado` T1). Completa el slice
`ProProducto` con la búsqueda paginada por nombre omitida, dentro del mismo
flujo (mismo controller, servicio, auth `AdminPolicy` y caché).
**Depende de**: `03-03`, `02-01`, `03-00`, `03-16`, `05-01`.

## Requisitos
1. `GET /api/v{version:apiVersion}/productos/search?texto=...&page=1&pageSize=20`
   → `200 PagedResult<ProProductoDto>`; `texto` nulo/vacío/blanco → `400`
   `{ error = "El texto de búsqueda es requerido." }`; `texto` máx 50
   (`[StringLength(50)]`, espejo de `ProProducto.strNombreProducto`).
2. Paginación igual que `GetPaged`: `page < 1 || pageSize < 1 ||
   pageSize > 100` → `400 { error }`. Sin `QueryParams` por decisión
   (coherencia con addenda `03-01` T2 y `03-02` T2).
3. Servicio: `SearchByNameAsync(string texto, int page, int pageSize,
   CancellationToken)`; `AsNoTracking`, `OrderBy(id)`, `texto.Trim()` con
   `Contains` sobre `strNombreProducto`; case-insensitive por collation
   del proveedor, sin normalización adicional.
4. Caché TTL 60s versionada (mismo mecanismo T1):
   `cache:producto:search:{version}:{texto}:{page}:{pageSize}`;
   interpolación `$"..."`.
5. Auth heredada de clase `[Authorize(Policy="AdminPolicy")]`.
6. Tests en las mismas carpetas del slice; Stryker ≥80% en lo nuevo.

## Diseño
- Modificar: `Services/ProProductoService.cs` (+`SearchByNameAsync` +
  interfaz), `Controllers/V1/ProProductoController.cs`
  (`[HttpGet("search")]`, `[FromQuery][StringLength(50)] string texto` +
  `page/pageSize` + `CancellationToken`, `400` como
  `BadRequest(new { error })`, nunca `BadRequest(string)` plano del snippet).
- Sin cambios en `Dtos/` (reutiliza `ProProductoDto` + `PagedResult<>`) ni
  en `Validators/` (el `400` por texto vacío/paginación se valida inline en
  controller, igual que `GetPaged`).
- Ruta literal `search`: no colisiona con `[HttpGet("{id:int}")]` por la
  restricción `:int`, pero se declara explícita (el snippet sin ruta no enruta).
- Paquetes: sin cambios.

## Contratos
- `GET /api/v1/productos/search?texto=torn&page=1&pageSize=20` →
  `200 { Items, TotalCount, Page, PageSize }` PascalCase.
- Sin `texto` (o blanco) → `400` con `error`; paginación inválida → `400`;
  `401/403` por `AdminPolicy`.

## Tests
- `UnitTest/ProProducto/` (fake `ICacheService`): filtra por fragmento y
  ordena por `id`, `Trim()` aplicado, 2ª llamada desde caché, página vacía
  (`TotalCount=0`) con texto sin coincidencias, llaves/TTL
  (`cache:producto:search:…`, 60s).
- `IntegrationTest/ProProducto/` (`TestAuthHandler`): 200 con `TotalCount`,
  sin texto → 400 con `error`, `texto` > 50 chars → 400, `page=0` → 400,
  `403` rol `User`.
- `SecurityTest/ProProducto/`: anónimo → `401` en `search` (1 test nuevo).
- Stryker (`stryker-0303.json` ampliado, `ignore-mutations: ["Boolean"]`):
  ≥80%, objetivo 100% como en T1 (45/45).

## Criterios
- `dotnet build -c Release --no-restore` → 0/0.
- `dotnet test UnitTest -c Release --no-build` 100% verde.
- `dotnet test IntegrationTest -c Release --no-build` 100% verde.
- `dotnet test SecurityTest -c Release --no-build` 100% verde.
- `GET /api/v1/productos/search?texto=<nombre-seed>` → 200 con
  `TotalCount>=1`; sin `texto` → 400 con `error`; `?texto=<50+ chars>` → 400.
- Stryker en `ProProductoService` ≥80%.
- Tras Stryker: `dotnet build` obligatorio antes de cualquier test
  `--no-build` (binarios mutantes en `bin/`, ver `AGENTS.md` §4).

## Límites
- Sin `QueryParams`; sin rate limiting propio (fase `04-04`); sin middleware
  global de errores (fase `03-16`).
- Búsqueda `Contains` simple; case-insensitive solo por collation del
  proveedor (SQL Server CI por defecto; InMemory es case-sensitive: los
  tests usan mismo casing). Sin full-text, sin normalización de acentos,
  sin ranking por relevancia.
- Solo campo `strNombreProducto`; sin filtro por descripción, existencia o
  precio; sin ordenamiento configurable (fijo `OrderBy(id)`).
- Invalidación solo por rotación de versión en writes (lecturas
  eventualmente consistentes ≤60s).
- `403` con JWT real diferido a fase 04 (stub `AdminPolicy`).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** addendum T2 pendiente revisión; al aprobar, fusionar Detalle
  en `spec.md` principal y registrar en `Memoria.md`.
