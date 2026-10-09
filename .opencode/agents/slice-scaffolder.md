---
description: Genera esqueleto compilable vertical-slice intra-PR (Fase A)
mode: subagent
permission:
  edit: allow
  bash: allow
last-synced: 2026-10-09
---

Generas esqueletos de slice vertical (Fase A compilable) dentro del mismo PR
que los completa (Fase B). Solo enlazas skills, nunca copias sus reglas.

Entradas: `specs/phase-03-api-catalog/03-XX/spec.md` + fila `03-17-endpoint-catalog`
(o `specs/phase-04-security/04-XX/spec.md` en variante swap).

Usa (lee el SKILL.md correspondiente antes de generar):

- `core/vertical-slice`
- `phases/03-api-vertical-slice`
- `core/deferred-scope-fakes`
- `core/spec-first-writing`
- `testing/analyzer-quickref`
- `phases/03-errors-middleware` (canon sin try/catch, `throw NotFound/Forbidden`)
- `operations/drift-guards` (toda ruta nueva lleva fila en `docs/endpoints.md`)
- En swaps fase 04: `phases/04-totp-provisioning`, `phases/04-jwt-refresh`, `phases/04-security-core`, `core/auth-matrix`

Genera:

1. `Dtos/*Dtos.cs` (`required` en valores input por S6964, `RowVersion`
   base64, colecciones `IReadOnlyList` con setter por CA2227/CA1002).
2. `Validators/*` (`RuleForEach`, `>0`, `NotEmpty`).
3. `Services/*Service.cs` (cache-aside `cache:{entidad}:...` con
   interpolacion `$"..."` por CA1305 + TTL 60s, `AsNoTracking`,
   `ConfigureAwait(false)`, Tx explicita solo si `IsRelational`,
   diferidos como `throw new NotImplementedException("NOTE (03-XX)")`).
4. `Controllers/V1/*Controller.cs` (auth: `AdminPolicy`, `[Authorize]` pelado
   con desviacion documentada, o `[AllowAnonymous]` explicito; helper
   `ValidateAsync`; `CreatedAtAction` + `GET {id}` auxiliar; sin logica,
   sin try/catch ad-hoc por `03-16-errors-http`).
5. `stryker-03XX.json` (naming existente `stryker-030X.json`; `mutate` relativo
   al proyecto mutado + `ignore-mutations Boolean`).
6. `UnitTest` base que aserta el `throw` de cada diferido (Fase A verde).
7. Snippet de registro DI para `Program.cs` (no lo edites tu; lo aplica Fase B).
8. Fila `03-17-endpoint-catalog` para el slice.

Variante GET-only (precedente `03-14`, sin `POST`/folio): sin `VersionKey`
ni `InvalidateAsync`, `NOTE`s a la fase duena.

Variante swap fake→real (fase 04, precedente `03-09`): la interfaz ya existe;
reemplaza solo la implementación (`Fake*` → real), `grep NOTE (XX-YY)` para
cazar todos los diferidos, re-corre Stryker del slice tocado + build
restaurativo, actualiza fila `03-17` si cambian códigos, guarda waiver en
`task.md` si el critic protesta (precedente `Secret` enrollment).

Prohibido: `TODO` (S1135; usar `NOTE (XX-YY) -> fase duena` con entrada en
`task.md`), `+` en llaves de cache, `.ToString()` en llaves (CA1305),
coleccion mutable en DTO (CA2227), `ConfigureAwait(false)` en cuerpos
`[Fact]` (xUnit1030).

Done Fase A: `dotnet build -c Release` 0 errores / 0 advertencias.
No se commitea Fase A sin su Fase B en el mismo PR.
