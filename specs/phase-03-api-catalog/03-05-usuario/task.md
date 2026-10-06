# 03-05 — SegUsuario

## T1 — CRUD + SearchByName + Autocomplete (pendiente, integrado)
- **Crear**: `SegUsuarioController`, `SegUsuarioService` (hashing vía `04-02`), `SegUsuarioCreateDto`/`SegUsuarioUpdateDto`/`SegUsuarioDeleteDto` (sin `strPWD`/`str2FASecreto` en lectura) + `SegUsuarioAutocompleteDto {id, strNombre}`, `SegUsuarioCreateValidator`/`SegUsuarioUpdateValidator`/`SegUsuarioDeleteValidator`, tests en `UnitTest/SegUsuario/`, `IntegrationTest/SegUsuario/`, `SecurityTest/SegUsuario/`.
- **Modificar** (mismo flujo): `SegUsuarioService` (+`SearchByNameAsync(page/pageSize)` / `AutocompleteAsync(maxResultados)`), `SegUsuarioController` (`[HttpGet("search")]` / `[HttpGet("autocomplete")]` + `CancellationToken` + `BadRequest(new { error })`).
- **Verificar**: CRUD completo; cache invalidación; `search` 200/`TotalCount` y `autocomplete` top-N sin secretos; `dotnet build -c Release --no-restore` 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` verdes; Stryker ≥80%; `dotnet build` tras Stryker.
- **Guardarraíles**: DTOs validados; no exponer entidad; secretos jamás en DTOs/caché/logs (test de ausencia); sin `QueryParams`; `Contains` en `strNombre` (no correo); `ConfigureAwait(false)`.
