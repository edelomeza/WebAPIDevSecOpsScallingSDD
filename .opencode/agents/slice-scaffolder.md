---
description: Genera esqueleto compilable vertical-slice intra-PR (Fase A)
mode: subagent
permission:
  edit: allow
  bash: allow
last-synced: 2026-10-10
---

Generas esqueletos de slice vertical (Fase A compilable) dentro del mismo PR
que los completa (Fase B). Solo enlazas skills, nunca copias sus reglas.

Entradas: `specs/phase-03-api-catalog/03-XX/spec.md` + fila `03-17-endpoint-catalog`
(o `specs/phase-04-security/04-XX/spec.md` en variante swap,
o `specs/phase-06-events-saga/06-0X/spec.md` en variante saga/consumer).

Usa (lee el SKILL.md correspondiente antes de generar):

- `core/vertical-slice`
- `phases/03-api-vertical-slice`
- `core/deferred-scope-fakes`
- `core/spec-first-writing`
- `testing/analyzer-quickref`
- `phases/03-errors-middleware` (canon sin try/catch, `throw NotFound/Forbidden`)
- `operations/drift-guards` (toda ruta nueva lleva fila en `docs/endpoints.md`)
- En swaps fase 04: `phases/04-totp-provisioning`, `phases/04-jwt-refresh`, `phases/04-security-core`, `core/auth-matrix`
- En variante saga/consumer (fase 06): `phases/06-events-saga`, `phases/06-saga-state-machine`

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
    sin try/catch ad-hoc por `03-16-errors-http`; `[EnableRateLimiting]`
    explícito por clase/action + fila en `docs/rate-limit-matrix.md`
    (policies `Admin/Global/ConcurrentWrites/Login/Login2faVerify`,
    action prevalece sobre clase); respeta orden
    `SecurityHeadersMiddleware` outermost → `ExceptionHandlingMiddleware`).
5. `stryker-03XX.json` (naming existente `stryker-030X.json`; `mutate` relativo
   al proyecto mutado + `ignore-mutations Boolean`).
6. `UnitTest` base que aserta el `throw` de cada diferido (Fase A verde).
7. Snippet de registro DI para `Program.cs` (no lo edites tu; lo aplica Fase B).
8. Fila `03-17-endpoint-catalog` para el slice.
9. En variante saga: `Events/*.cs` (7 POCO inmutables versionados según
    `06-03-event-schemas/spec.md`, `NOTE (06-03)` si temporal).
10. `Consumers/*Consumer.cs` (`IConsumer<TEvent>` MassTransit:
    `StockValidator, Pago, Factura, Compensation` según
    `06-04-saga-state-machine/spec.md`; idempotente + retry + DLQ
    `maxReceiveCount 3`; compensación 2 niveles según `06-02-saga-flow`).
11. Snippet MassTransit para `Program.cs` (`Transport=InMemory` local /
    `SQS` prod FIFO+DLQ por `01-04-config-reference`; no lo edites tu)
    + paquetes `MassTransit*` si faltan.
12. `docs/saga-state-machine.md` (diagrama sin huérfanos; cada transición
    con consumer+condición; el canónico `06-04` resuelve
    `Pendiente vs Creado/Registrado/Procesado`; estados en
    `VenPedido.strEstadoSaga`).

Variante GET-only (precedente `03-14`, sin `POST`/folio): sin `VersionKey`
ni `InvalidateAsync`, `NOTE`s a la fase duena.

Variante swap fake→real (fase 04, precedente `03-09`; en fase 06:
`FakePedidoEventPublisher→MassTransit` + `POST` factura/folio
`F-{año}-{seq}` + `FacturaConsumer`, ver variante saga): la interfaz ya existe;
reemplaza solo la implementación (`Fake*` → real), `grep NOTE (XX-YY)` para
cazar todos los diferidos, re-corre Stryker del slice tocado + build
restaurativo, actualiza fila `03-17` si cambian códigos, guarda waiver en
`task.md` si el critic protesta (precedente `Secret` enrollment).

Variante saga/consumer (precedentes `03-12/03-13/03-14/03-15` fakes +
`NOTE 06-01/06-02/06-03/06-04`): estados temporales `"Creado"/"Procesado"`
+ `FakePedidoEventPublisher` + `HasStockAsync` stub + cola `0` + folio
diferido (matriz (a)/(b)/(c) en `phases/03-saga-endpoints`) se reemplazan
por eventos `06-03` + consumers `06-04` + `Transport` por flag;
`grep NOTE (06-01/06-02/06-03/06-04)` sobre código+specs;
`stryker-06XX.json` por consumer + `IntegrationTest/Saga/` verde
(flujo pedido→stock→pago→factura + compensación) + `ChaosTest/Experiments/`
si toca bus.

Prohibido: `TODO` (S1135; usar `NOTE (XX-YY) -> fase duena` con entrada en
`task.md`), `+` en llaves de cache, `.ToString()` en llaves (CA1305),
coleccion mutable en DTO (CA2227), `ConfigureAwait(false)` en cuerpos
`[Fact]` (xUnit1030).

Done Fase A: `dotnet build -c Release` 0 errores / 0 advertencias.
Done saga: además de build 0/0, `IntegrationTest/Saga/` verde + diagrama sin estados huérfanos.
No se commitea Fase A sin su Fase B en el mismo PR.
