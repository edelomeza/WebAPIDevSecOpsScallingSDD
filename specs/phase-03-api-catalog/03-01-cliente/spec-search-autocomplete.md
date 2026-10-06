# 03-01 T2 Addendum — Cliente: SearchByName + Autocomplete

## Contexto
Addendum de `03-01` (no modifica el `✅ Aprobado` T1). Completa el slice
`CliCliente` con dos lecturas omitidas, dentro del mismo flujo (mismo
controller, servicio, auth `AdminPolicy` y caché). **Depende de**: `03-01`,
`02-01`, `03-00`, `03-16`, `05-01`.

## Requisitos
1. `GET /api/v{version:apiVersion}/clientes/search?texto=...&page=1&pageSize=20`
   → `200 PagedResult<CliClienteDto>`; `texto` nulo/vacío/blanco → `400`
   `{ error = "El texto de búsqueda es requerido." }`; `texto` máx 100
   (`[StringLength(100)]`, espejo de `CliCliente.strNombreCliente`).
2. Paginación igual que `GetPaged`: `page < 1 || pageSize < 1 ||
   pageSize > 100` → `400 { error }`. Sin `QueryParams` por decisión.
3. `GET /api/v{version:apiVersion}/clientes/autocomplete?texto=...&maxResultados=10`
   → `200 IEnumerable<CliClienteAutocompleteDto>` (`{ id, strNombreCliente }`
   únicamente); `texto` vacío → `400 { error }`; `maxResultados` fuera de
   `[1,50]` se normaliza a `10`.
4. Servicio: `SearchByNameAsync(string texto, int page, int pageSize,
   CancellationToken)` y `AutocompleteAsync(string texto, int maxResultados,
   CancellationToken)`; `AsNoTracking`, `OrderBy(id)`, `Contains` sobre
   `strNombreCliente` con `texto.Trim()`; case-insensitive por collation
   del proveedor, sin normalización adicional.
5. Caché TTL 60s versionada (mismo mecanismo T1):
   `cache:cliente:search:{version}:{texto}:{page}:{pageSize}` y
   `cache:cliente:autocomplete:{texto}:{max}`; interpolación `$"..."`.
6. Auth heredada de clase `[Authorize(Policy="AdminPolicy")]`.
7. Tests en las mismas carpetas del slice; Stryker ≥80% en lo nuevo.

## Diseño
- Modificar: `Dtos/CliClienteDtos.cs` (+`CliClienteAutocompleteDto`),
  `Services/CliClienteService.cs` (+2 métodos + interfaz),
  `Controllers/V1/CliClienteController.cs` (`[HttpGet("search")]`,
  `[HttpGet("autocomplete")]`, `CancellationToken`,
  `BadRequest(new { error })`, nunca `BadRequest(string)`).
- Rutas literales: no colisionan con `[HttpGet("{id:int}")]` por la
  restricción `:int`, pero se declaran explícitas (el snippet sin ruta
  no enruta).
- Paquetes: sin cambios.

## Contratos
- `GET /api/v1/clientes/search?texto=ana&page=1&pageSize=20` →
  `200 { Items, TotalCount, Page, PageSize }` PascalCase; `400/401/403`.
- `GET /api/v1/clientes/autocomplete?texto=ana&maxResultados=10` →
  `200 [{ id, strNombreCliente }]` ordenados por `id`; `400/401/403`.

## Tests
- `UnitTest/CliCliente/`: search pagina/ordena/cachea/trim; autocomplete
  top-N, bordes `0/51/-5 → 10`, forma mínima, llaves/TTL (`cache:cliente:…`, 60s).
- `IntegrationTest/CliCliente/` (`TestAuthHandler`): 200 + `TotalCount`,
  sin texto → 400 con `error`, `page=0` → 400, bordes `0/51 → 10`,
  `403` rol `User` en ambas rutas.
- `SecurityTest/CliCliente/`: 2×401 anónimo en rutas nuevas.
- Stryker (`stryker-0301.json` ampliado, `ignore-mutations: ["Boolean"]`):
  ≥80%, objetivo 100%.

## Criterios
- `dotnet build -c Release --no-restore` → 0/0.
- `dotnet test UnitTest -c Release --no-build` 100% verde.
- `dotnet test IntegrationTest -c Release --no-build` 100% verde.
- `dotnet test SecurityTest -c Release --no-build` 100% verde.
- `GET /api/v1/clientes/search?texto=<nombre-seed>` → 200 con
  `TotalCount>=1`; sin `texto` → 400 con propiedad `error`.
- `GET /api/v1/clientes/autocomplete?texto=<prefijo>&maxResultados=5` →
  200 con `<=5` elementos y solo `id`/`strNombreCliente`.
- Stryker en `CliClienteService` ≥80%.
- Tras Stryker: `dotnet build` obligatorio antes de cualquier test
  `--no-build` (binarios mutantes en `bin/`, ver `AGENTS.md` §4).

## Límites
- Sin `QueryParams`; sin rate limiting propio (fase `04-04`); sin middleware
  global de errores (fase `03-16`).
- Búsqueda `Contains` simple; case-insensitive solo por collation del
  proveedor (SQL Server CI por defecto; InMemory es case-sensitive: los
  tests usan mismo casing). Sin full-text, sin normalización de acentos,
  sin ranking por relevancia.
- Autocomplete no paginado; invalidación solo por rotación de versión en
  writes (lecturas eventualmente consistentes ≤60s).
- `403` con JWT real diferido a fase 04 (stub `AdminPolicy`).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** addendum T2 pendiente revisión; al aprobar, fusionar Detalle
  en `spec.md` principal y registrar en `Memoria.md`.
