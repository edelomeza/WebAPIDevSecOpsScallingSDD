# 03-05 — Usuario

## Contexto
Vertical slice CRUD de `SegUsuario` (mismo patrón que `03-01`; password solo hash) más búsqueda paginada por nombre y autocomplete ligero en el mismo flujo. **Depende de**: `02-01`, `03-00`, `03-16`, `04-03`, `05-01`, `04-02`.

## Requisitos
1. DTOs `SegUsuarioCreateDto/UpdateDto/DeleteDto` (`RowVersion` base64 en Update/Delete; jamás exponer `strPWD` ni `str2FASecreto` en lectura) + `SegUsuarioAutocompleteDto` (`{ id, strNombre }` únicamente).
2. Validadores FluentValidation espejo del modelo.
3. Servicio con cache-aside (nunca cachear password) e invalidación en writes; conflicto → 409.
4. Controller `api/v{version:apiVersion}/usuarios` con `[Authorize(Policy="AdminPolicy")]`.
5. `GET /usuarios/search?texto=...&page=1&pageSize=20` → `200 PagedResult<SegUsuarioDto>`; `texto` vacío → `400 { error }`; `texto` máx 50 (`[StringLength(50)]`, espejo de `strNombre`); paginación igual que `GetPaged`. Sin `QueryParams` (coherencia con addenda `03-01`/`03-02`/`03-03` T2).
6. `GET /usuarios/autocomplete?texto=...&maxResultados=10` → `200 IEnumerable<SegUsuarioAutocompleteDto>`; `texto` vacío → `400 { error }`; `maxResultados` fuera de `[1,50]` se normaliza a `10`.
7. Servicio: `SearchByNameAsync(string texto, int page, int pageSize, CancellationToken)` y `AutocompleteAsync(string texto, int maxResultados, CancellationToken)`; `AsNoTracking`, `OrderBy(id)`, `texto.Trim()` con `Contains` sobre `strNombre` (no correo, anti-enumeración); caché versionada 60s `cache:usuario:search/autocomplete:*` sin secretos.
8. Tests unit/integration/security; Stryker ≥80% en el servicio, con test explícito de ausencia de secretos.

## Diseño
- `Dtos/SegUsuarioDtos.cs` (+`SegUsuarioAutocompleteDto`), `Validators/SegUsuarioValidators.cs`, `Services/SegUsuarioService.cs` (+`SearchByNameAsync`/`AutocompleteAsync`), `Services/SegUsuarioPasswordHasher.cs` (`ISegUsuarioPasswordHasher` + fake temporal, ver Límites), `Controllers/V1/SegUsuarioController.cs` (`[HttpGet("search")]`, `[HttpGet("autocomplete")]`, `CancellationToken`, `400` como `BadRequest(new { error })`).
- Hash solo en writes vía `ISegUsuarioPasswordHasher`; lectura y caché nunca tocan `strPWD`/`str2FASecreto`; llaves interpoladas `$"..."`.

## Contratos
- `GET/POST /api/v1/usuarios`, `GET/PUT/DELETE /api/v1/usuarios/{id}`; códigos 200/201/204/400/401/403/404/409.
- `PagedResult<SegUsuarioDto>` (sin `strPWD` ni `str2FASecreto`).
- `GET /api/v1/usuarios/search?texto=...&page=1&pageSize=20` → `200 PagedResult<SegUsuarioDto>`; `400` texto requerido/paginación inválida.
- `GET /api/v1/usuarios/autocomplete?texto=...&maxResultados=10` → `200 [{ id, strNombre }]`; `400` texto requerido.

## Tests
- `UnitTest/SegUsuario/SegUsuarioServiceTests.cs` (26): CRUD + search (página 2 vacía, trim, caché) + autocomplete (top-N, bordes `0/51/-5 → 10`, borde `50` no normalizado, forma mínima) + **ausencia de `strPWD`/`str2FASecreto`** en respuestas serializadas.
- `IntegrationTest/SegUsuario/SegUsuarioControllerTests.cs` (5, `TestAuthHandler`): CRUD + `search`/`autocomplete` 200 (cuerpo sin secretos), 400 sin texto, bordes `0/51 → 10`, 403 rol `User`.
- `SecurityTest/SegUsuario/SegUsuarioSecurityTests.cs` (7): anónimo → `401` en CRUD + `search` + `autocomplete`.
- Stryker (`stryker-0305.json`, `ignore-mutations: ["Boolean"]`): umbral ≥80%, alcanzado 100% (78/78).

## Criterios
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` 100% verdes.
- CRUD completo; cache invalidación en writes; Stryker ≥80% en el servicio.
- `GET /api/v1/usuarios/search?texto=<nombre-seed>` → 200 `TotalCount>=1` sin secretos en el cuerpo; sin `texto` → 400 `error`.
- `GET /api/v1/usuarios/autocomplete?texto=<prefijo>&maxResultados=5` → 200 `<=5` items solo `id`/`strNombre`.
- Tras Stryker: `dotnet build` obligatorio antes de `--no-build` (`AGENTS.md` §4).

## Límites
- Hash temporal `FakeSegUsuarioPasswordHasher` (SHA256+sal fija, `NOTE (04-02)`); `04-02` lo reemplaza por Argon2id sin tocar el servicio.
- JWT real y rate limiting en fase 04; middleware de errores en `03-16`.
- Sin `QueryParams`; `Contains` simple (case-insensitive por collation; InMemory case-sensitive: tests usan mismo casing); sin full-text/acentos/ranking.
- Solo campo `strNombre`; sin búsqueda por correo (anti-enumeración); `OrderBy(id)` fijo.
- Consistencia eventual de lectura ≤60s por rotación de versión en writes.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 06-Oct-2026
- **Detalle:** T1+T2 ejecutados y revisados: build 0/0, Unit 97/97, Security 35/35, Integration 33/33, Stryker 100% (78/78); `Update` sin rotación de password; hasher fake temporal hasta `04-02`.
