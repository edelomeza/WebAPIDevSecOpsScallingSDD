# plan.md — 03-05-usuario

1. Mapeo

- Igual que 03-01, más Services/SegUsuarioService.cs con hashing vía 04-02.
- Más: `Dtos/SegUsuarioDtos.cs` (+`SegUsuarioAutocompleteDto {id, strNombre}`), `Services/SegUsuarioService.cs` (+`SearchByNameAsync`/`AutocompleteAsync`), `Controllers/V1/SegUsuarioController.cs` (`[HttpGet("search")]`, `[HttpGet("autocomplete")]`, `CancellationToken`, `{ error }`).
- Modificar tests: `UnitTest/SegUsuario/`, `IntegrationTest/SegUsuario/`, `SecurityTest/SegUsuario/` (+ ausencia de secretos).
- Crear/ampliar config Stryker del slice (`ignore-mutations: ["Boolean"]`).

2. Decisiones

- `page/pageSize` sueltos (validación `GetPaged`); `texto` requerido `[StringLength(50)]` + `Trim()` + `Contains` en `strNombre` (no correo, anti-enumeración); `maxResultados [1,50] → 10`.
- DTOs sin `strPWD`/`str2FASecreto`; caché `cache:usuario:*` 60s `$"..."`; `AsNoTracking` + `OrderBy(id)`; `ConfigureAwait(false)`.

3. Guardarraíles

- Password hasheado Argon2id/BCrypt; nunca en cache ni logs.
- **Nunca** `strPWD`/`str2FASecreto` en DTOs, respuestas, caché ni logs (falla el test de ausencia); no exponer entidad; no tocar `AdminPolicy`; `400` siempre `{ error }`.
- `dotnet build` tras Stryker antes de `--no-build`.

4. Pruebas

- Unit: CRUD + search (pagina/ordena/trim/caché) + autocomplete (top-N/bordes/forma) + ausencia de secretos.
- Integration (`TestAuthHandler`): 200 (+cuerpo sin secretos)/400/403 en CRUD y ambas rutas.
- Security: 401 anónimo en CRUD + `search` + `autocomplete`.
- Stryker ≥80% en el servicio.

5. Secuencia

1. CRUD base (DTOs/validators/servicio/controller + hashing).
2. Search/Autocomplete (DTO mínimo → servicio → controller).
3. Unit → Integration → Security → build Release → Stryker.
4. Conciliar spec/plan con archivos reales y resolver aprobación.
