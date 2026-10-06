# Memoria del proyecto

## T1 — 00-01 Principles & Stack (2026-10-04)
- spec.md enriquecido: principios con consecuencias prácticas, tabla de stack por capa/subcategoría, regla de excepción, bloque de aprobación.
- AGENTS.md §1 cita 00-01 como callout. 00-02 ya declaraba `Depende de: 00-01`.
- T1 cerrado: spec con 1 revisor registrado.

## T1 — 00-02 SDD Governance (2026-10-04)
- Plantilla formal en `specs/_template.md` (7 secciones + Aprobación y Control de Cambios + reglas de opcionalidad).
- DoD definido en `00-02/spec.md`: 1 revisor, criterios por comando/test, versión en cambios de principios, trazabilidad spec↔código↔test.
- Piloto migrado: 00-01, 00-03, 01-01 ahora siguen la plantilla.
- `agent.md` eliminado del mapa: lecciones aprendidas viven en esta Memoria; referencias actualizadas en AGENTS.md §2, 00-02 plan/task y 00-04 plan/task.

## T1 — 00-03 NFR (2026-10-04)
- `00-03/spec.md` migrada a plantilla: columnas Categoría, Estado del Umbral, Comando/Script.
- Todo comando concreto referenciado a scripts/proyectos reales (🚧 marca los pendientes con su fase objetivo).
- `AGENTS.md` §4.2 nueva subsección con guardarraíl de verificación NFR.
- Decisión: umbrales 🎯 Objetivo se promoverán a 📈 Medido real iterativamente al desplegar la infraestructura correspondiente (regla de medición empírica de 00-01).

## T1 — 00-04 Lessons Learned (2026-10-04)
- `00-04/spec.md` migrada a `specs/_template.md`: tabla de 13 reglas con Evidencia (Legado vs. Fase 4-6 Objetivo) y cross-link por spec aplicable.
- Duplicidad eliminada: AGENTS.md §10 ahora apunta a 00-04 como fuente canónica.
- T1 completada: centralización de lecciones en 00-04. Se eliminó la duplicidad en AGENTS.md para mitigar drift de contexto.

## T1 — 01-01 Setup & Config (2026-10-04)
- `Directory.Build.props` raíz: AnalysisMode=All, TreatWarningsAsErrors, NuGetAudit, SonarAnalyzer.CSharp 10.11.0.117924, Nullable on, ImplicitUsings off.
- `nuget.config` con Package Source Mapping (Microsoft.*/System.*/SonarAnalyzer.*/xunit*/coverlet.* → solo nuget.org).
- `appsettings.Example.json` con placeholders; `.gitignore` creado (bin/obj/.vs/reportes/secretos locales).
- Limpieza ImplicitUsings: `Program.cs` con usings explícitos; template WeatherForecast eliminado; `Program.cs` con `await app.RunAsync().ConfigureAwait(false)` y endpoint `/ping`.
- BuildSmokeTests.cs en `UnitTest/Common/` (assembly carga sin ReflectionTypeLoadException, tipo Program presente).
- AGENTS.md secciones 5-9 con marcador 🚧 [FASE NN - PENDIENTE].
- Verificación: `dotnet build -c Release` 0 errores/0 advertencias; `dotnet test UnitTest` 2/2 verde.
- Nota: se eliminó el template WeatherForecast; los tests de humo validan tipo `Program`.

## T1 - 01-02 Program Pipeline (2026-10-04)
- Program.cs reestructurado: AddWebApiDevSecOpsServices (CORS restringido + AddAuthorization) y UseWebApiDevSecOpsPipeline (ForwardedHeaders > HSTS no-Dev > HttpsRedirection > CORS > OpenApi dev > Authorization). public partial class Program para WebApplicationFactory.
- Kestrel:Limits y Cors:AllowedOrigins en appsettings.json/appsettings.Example.json (no hardcodeado).
- IntegrationTest/Middleware/MiddlewareTests.cs con Microsoft.AspNetCore.Mvc.Testing 10.0.12; UnitTest/DI/ServiceRegistrationTests.cs valida politica CORS.
- Guardarrailes: slots de fases 04/05/08/01-03 diferidos, sin stubs.
- Verificacion: build 0/0; UnitTest 5/5; IntegrationTest 3/3.
## T1 - 01-03 Startup & Health (2026-10-04)
- Program.cs: exception handler JSON generico (no-Dev) / DeveloperExceptionPage (Dev); /health y /health/ready con MapHealthChecks.
- AssemblyIntegrityCheck (IHealthCheck): SHA-256 vs AssemblyIntegrity:ExpectedSha256; Dev->Degraded, Prod->Unhealthy; valor ausente->Degraded(Dev)/Healthy(Prod).
- AddHealthChecks registrado en DI; SecurityTest con ProjectReference.
- Tests: AssemblyIntegrityTests (4) SecurityTest; HealthTests (2) IntegrationTest.
- Verificacion: build 0/0; SecurityTest 4/4; IntegrationTest 5/5.
- Diferidos: migraciones tolerantes y readiness real con DB -> Fase 02; health-ui -> Fase 08.
## T1 - 01-04 Config Reference (2026-10-04)
- appsettings.Example.json alineado al codigo real; AssemblyIntegrity:ExpectedSha256 unificado (documentacion corregida, no el codigo).
- appsettings.Production.json nuevo: UseInMemoryDatabase=false, CORS restringido, sin secretos (placeholder ExpectedSha256 vacio).
- 01-04/spec.md migrada: columna Estado/Fase; AssemblyIntegrity:ExpectedHash corregido a ExpectedSha256; claves futuras marcadas Pendiente.
- UnitTest/Common/AppSettingsTests.cs (3): Example.json valido+sin secretos, appsettings sin patrones sensibles, no claves futuras en Example.json.
- AGENTS.md nueva subseccion 4.3 Referencia de configuracion.
- Verificacion: build 0/0; UnitTest 8/8.
## T1 - 01-05 Repo Layout (2026-10-04)
- MutationTest/PerformanceTest/ChaosTest existian como proyectos autonomos sin entrar en la slnx, con stubs de template. Ahora: añadidos los 3 csproj a la slnx y stubs reemplazados por tests placeholder xUnit (Performance/Chaos) y MutationTest como biblioteca sin archivos de template.
- Nuevos directorios: deploy/ (README), scripts/ (check_coverage.py stub Fase 07 + README), .semgrep/semgrep.yaml, fuzzing/README, .github/ (dependabot.yml, pull_request_template.md, workflows vacio Fase 09).
- .dockerignore nuevo; .gitignore con perf-reports/, coverage/, *.coverage.
- 01-05/spec.md migrada a plantilla _template.md.
- Verificacion: build Release 0/0 sobre los 9 proyectos; UnitTest 8/8, IntegrationTest 5/5, SecurityTest 4/4.
## T1 - 05-01 Cache & Redis (2026-10-04)
- DI con IConnectionMultiplexer singleton (AbortOnConnectFail=false, ConnectTimeout=2000, SyncTimeout=1000, ExponentialRetry 5000) resuelto desde IConfiguration en runtime (post-override), no capturado temprano.
- CacheService: cache-aside Redis/IMemoryCache, write-through set, TTL 0-120s, prefijos obligatorios (blacklist:, attempts:, lockout:, cache:), guardarraíl anti password/secret/token.
- RedisHealthCheck solo en /health/ready (503 al caer); /health (liveness) always 200 via assembly-integrity tag live. Stderr corrige la spec original invertida.
- Config: Redis:ConnectionString en appsettings.Example.json/Production.json; 01-04/spec.md actualizado a[Fase 05 / Implementado].
- Tests: SecurityTest/Cache/LockoutTests (4); IntegrationTest/Cache/CacheFallbackTests con Testcontainers.Redis real; HealthTests con /health/ready 503 determinista.
- App integration issue clave: el singleton de Redis leía la config antes del override de WebApplicationFactory; se resolvión leyendo IConfiguration en el factory del DI.
- Paquetes: StackExchange.Redis 2.9.32, Testcontainers.Redis 4.13.0 (nuget.config packageSourceMapping ampliado); Docker Desktop arrancado para Testcontainers.
- Verificacion: build Release 0/0; UnitTest 8/8; SecurityTest 8/8; IntegrationTest 7/7.
## T1 - 02-01 Model & DbContext (2026-10-04)
- Implementado: Models/ con 13 entidades + IConcurrenteAuditable; Context/AppDbContext.cs con DbSets, OnModelCreating con IsRowVersion() para entidades auditables y mitigacion InMemory byte[]{1} en SaveChanges/SaveChangesAsync.
- Decisiones: 13 tablas (incluye VenPedidoPago); catalogos sin RowVersion; strPWD se mantiene; collections VenPedido como get-only inicializados.
- Program.cs: AppDbContext registrado con UseInMemoryDatabase (default true) o UseSqlServer(ConnectionStrings:Default) cuando false.
- Paquetes: Microsoft.EntityFrameworkCore.SqlServer 10.0.12 y InMemory 10.0.12; nuget.config ampliado con Azure.*/System.Security.*/System.ClientModel/Microsoft.IdentityModel.* para source mapping.
- Tests: UnitTest/DbContext/DbContextTests.cs (2): SqlServer dummy e InMemory con RowVersion forzado.
- Verificacion: build Release 0 errores/0 advertencias; UnitTest 10/10.
## T1 - 02-02 Migrations & Seed (2026-10-05)
- Migracion InitialCreate (13 tablas, rowversion, decimal(18,2)); precision decimal fijada en OnModelCreating para evitar warnings de truncado; NoWarn CA1062 para codigo generado.
- DatabaseSeeder (seed minimo): lecturas EF AnyAsync + inserts SQL crudo con IDENTITY_INSERT por tabla, todo en una transaccion; idempotente por chequeo de existencia; solo SQL Server.
- POST /provider-states {state} -> 200/400, gateado por EnableProviderStates y no-prod; estados conocidos: base.
- Flags SkipMigration/EnableProviderStates promovidos a appsettings.Example.json; matriz 01-04 a [Fase 02 / Implementado]; task/spec 02-02 corregidos a 13 tablas.
- Fix clave: UseInMemoryDatabase se leia eager antes del override de WebApplicationFactory (app usaba InMemory pese al override); se movio la lectura a IConfiguration dentro del factory de AddDbContext (misma leccion que Redis).
- Tests: DatabaseTest/MigrationTests (MsSql puerto fijo 14333: migra, 13 tablas, seed x2, rollback a 0 vacio); IntegrationTest/Common/ProviderStatesTests (puerto 14334: base->200+seed, desconocido->400).
- Paquetes: EF Design 10.0.12, Testcontainers.MsSql 4.13.0 (NoWarn NU1903 por SSH.NET como en Redis); dotnet-ef global 10.0.9->10.0.12; nuget.config suma dotnet-ef/Newtonsoft.Json.
- Nota: INFORMATION_SCHEMA en master incluye spt_* del sistema; el test aserta presencia de las 13 esperadas, no conteo exacto.
- Verificacion: build Release 0/0; UnitTest 10/10; SecurityTest 8/8; IntegrationTest 9/9; DatabaseTest 1/1.
## T1 - 02-03 Schema (2026-10-05)
- spec.md reescrito desde la migracion InitialCreate (era obsoleto: 12 tablas con columnas inexistentes): 13 tablas con tipos exactos, FKs con cascade, nota LegacyVentaId, seccion de indices; estados saga sin enumerar (difieren a fase 06).
- Migracion UniqueConstraints: UNIQUE en VenPedidoPago.strIdTransaccion (filtrado IS NOT NULL) y VenPedidoFactura.strFolioFactura.
- DatabaseTest/SchemaTests (MsSql puerto fijo 14335): 13 tablas, 13 PKs, 12 FKs, 11 rowversion, 6 decimal(18,2), 2 unicos; queries acotadas a las 13 tablas para evitar spt_* de master.
- Verificacion: build Release 0/0; DatabaseTest 2/2.
## T1 - 02-04 Test Data (2026-10-05)
- UnitTest/Common/TestDataFactory.cs (publica para reuse de SecurityTest): constantes de IDs seed + builders deterministas; TestDataFactoryTests (2).
- DatabaseSeeder: existencia producto 1->1; estados nuevos `race` (resetea existencia=1), `saga` (=base), `perf` (añade SegUsuario id=2 `perf`); `base` inalterado (MigrationTests intacto).
- IntegrationTest/Common/ProviderStatesTests +2 (race->existencia 1, perf->usuario perf).
- SecurityTest referencia a UnitTest (ProjectReference; AGENTS.md ya lo presuponía); NoWarn CA1515 en UnitTest.csproj.
- Specs: 02-04 actualizada (estados, perf id=2, credencial real solo env fase 10); 03-10/03-12 documentan IDs asumidos (race/saga).
- Verificacion: build Release 0/0; UnitTest 12/12; SecurityTest 8/8; DatabaseTest 2/2; IntegrationTest 11/11.
## T1 - 03-00 Routing & Versioning (2026-10-05)
- Program.cs: AddApiVersioning (Default 1.0, UrlSegmentReader, AssumeDefault=false estricto) + AddMvc + AddApiExplorer; AddControllers con PropertyNamingPolicy=null (PascalCase); MapControllers tras Authorization; MapScalarApiReference solo Dev junto a MapOpenApi.
- Controllers/V1/PingController.cs probe (api/v{version:apiVersion}/ping -> 200 {"Status":"Pong","Version":"v1"}).
- IntegrationTest/Routing/RoutingTests.cs (4): v1 resuelve + PascalCase, sin version -> 404, openapi/scalar 200 en Dev y 404 en Production.
- Paquetes: Asp.Versioning.Mvc + ApiExplorer 10.2.1, Scalar.AspNetCore 2.17.13; nuget.config suma Asp.*/Scalar.*.
- Nota: AV0029/AV0030 exigen AddOpenApi/WithDocumentPerVersion inexistentes en v10.2.1 -> NoWarn; Microsoft AddOpenApi + ApiExplorer genera /openapi/v1.json verificado por test.
- Verificacion: build Release 0/0; UnitTest 12/12; SecurityTest 8/8; DatabaseTest 2/2; IntegrationTest 15/15.
## T1 - 03-01 CliCliente (2026-10-05)
- Slice: Dtos (Dto/Create/Update/Delete + PagedResult, RowVersion base64, id required en Update/Delete por S6964), Validators FluentValidation, CliClienteService publico (CRUD+PagedResult+cache-aside, 409 via ConcurrencyConflictException), CliClienteController (api/v1/clientes, AdminPolicy).
- Cache: cache:cliente:{id} + cache:cliente:page:{version}:... TTL 60s; version rotada en writes (sin wildcard en CacheService); ICacheService/CacheService pasados a public por accesibilidad.
- Auth stub: AdminPolicy (rol Admin) + AnonymousChallengeHandler (401 sin esquemas); UseAuthentication antes de UseAuthorization; 403 probado en integration con rol no-admin, 401 en SecurityTest.
- Tests: UnitTest/CliCliente (14, fake cache con formato de llaves/TTL/NoTracking/nulos), IntegrationTest/CliCliente (3, TestAuthHandler X-Test-Role) + SecurityTest/CliCliente (5x401).
- Stryker: 100.00% en CliClienteService (45/45). Lecciones: CLI -m de v5.0.0 malparsea corchetes -> usar archivo de config (stryker-0301.json, con ignore-mutations Boolean porque ConfigureAwait(false->true) son equivalentes; nightly debe ampliar el mutate); mutantes `-` sobre string+int no compilan -> interpolar llaves de cache.
- Paquetes: FluentValidation 12.1.1, Mvc.Testing/TestHost en IntegrationTest, Mvc.Testing en SecurityTest (referencia a UnitTest para reuse futuro).
- Verificacion: build Release 0/0; UnitTest 26/26; SecurityTest 13/13; DatabaseTest 2/2; IntegrationTest 18/18; Stryker 100%.
## T1 - 03-02 EmpEmpleado (2026-10-05)
- Slice: Dtos (Dto/Create/Update/Delete, RowVersion base64, id required en Update/Delete), Validators FluentValidation (strNombre NotEmpty Max50, APaterno/AMaterno Max50, CURP Max18 + regex `^[A-Za-z]{4}[0-9]{6}[HhMm][A-Za-z]{5}[A-Za-z0-9][0-9]$` solo si no vacía, FK rango >0 si tiene valor), EmpEmpleadoService publico (CRUD+PagedResult+cache-aside, FK doble capa: validator rango + servicio verifica existencia → ValidationException→400, 409 via ConcurrencyConflictException), EmpEmpleadoController (api/v1/empleados, AdminPolicy, ValidationException→400).
- Cache: cache:empleado:{id} + cache:empleado:page:{version}:... TTL 60s; version rotada en writes (interpolación `$"..."`, no `+`).
- Tests: UnitTest/EmpEmpleado (18, FK nula/válida/inválida con assert de mensaje), IntegrationTest/EmpEmpleado (4: CRUD+409+400 CURP/FK/mismatch+403), SecurityTest/EmpEmpleado (5x401).
- Stryker: 100.00% en EmpEmpleadoService (51/51, stryker-0302.json con ignore-mutations Boolean). Iteración: primer run 98.04% con 1 superviviente (string-mutante en mensaje FK) → se mató asertando el mensaje en los 2 tests FK.
- Nota: Docker Desktop estaba detenido (Testcontainers fallaba con DockerUnavailableException); se arrancó el daemon y re-verde. `python` no existe en este entorno (check_coverage.py es stub Fase 07 de todos modos).
- Verificacion: build Release 0/0; UnitTest 44/44; SecurityTest 18/18; IntegrationTest 22/22; DatabaseTest 2/2; Stryker 100%.
## T1 - 03-03 ProProducto (2026-10-05)
- Slice: Dtos (Dto/Create/Update/Delete, RowVersion base64, `required` en id + intNumeroExistencia/decPrecio por S6964 anti under-posting), Validators (nombre NotEmpty Max50, URL Max300 sin formato — nullable por decisión, descripcion Max250, existencia/precio ≥0, creador Max50), ProProductoService (CRUD+PagedResult+cache-aside, sin FK), ProProductoController (api/v1/productos, AdminPolicy).
- Cache: cache:producto:{id} + cache:producto:page:{version}:... TTL 60s; interpolación `$"..."`.
- Tests: UnitTest/ProProducto (14), IntegrationTest/ProProducto (3: CRUD+409+400 precio/existencia/mismatch+403), SecurityTest/ProProducto (5x401).
- Stryker: 100.00% en ProProductoService (45/45, stryker-0303.json) al primer run.
- Lección nueva: Sonar S6964 exige `required`/nullable/`[JsonRequired]` en tipos valor de DTOs de input (int/decimal no-nullable → 4 errores); se marcó `required` al ser [Required] en la entidad. Slices previos no lo vieron (solo strings/nullable).
- Verificacion: build Release 0/0; UnitTest 58/58; SecurityTest 23/23; IntegrationTest 25/25; DatabaseTest 2/2; Stryker 100%.
## T1 - 03-04 VenCatEstado (2026-10-06)
- Slice sin RowVersion (catálogo no auditable): DTOs sin RowVersion (DeleteDto solo id), servicio sin bloque concurrencia, controller sin rama 409 (contratos 200/201/204/400/401/403/404). Sin unicidad en strValor (decisión, espejo del modelo).
- Cache: cache:estado-venta:{id} + páginas versionadas TTL 60s (verificado: no colisiona con patrones prohibidos password/secret/token).
- Tests: UnitTest/VenCatEstado (13: +UpdatePersistsAcrossContexts), IntegrationTest/VenCatEstado (3, CRUD sin paso stale), SecurityTest/VenCatEstado (5x401).
- Stryker: 100.00% (43/43, stryker-0304.json). Iteración: primer run 97.67% con 1 superviviente (eliminación de SaveChanges en Update) → las entidades sin RowVersion no tienen test de conflicto que lo mate; se mató con test cross-contexto (segundo contexto InMemory con mismo nombre de BD no ve cambios sin SaveChanges).
- Lección nueva: Stryker deja binarios mutantes en bin/ (compila la slnx con mutantes y no restaura el build genuino); todo `dotnet test --no-build` posterior falla con TypeLoadException (45 fallos fantasma). Regla: siempre `dotnet build` tras Stryker antes de cualquier test --no-build.
- Verificacion: build Release 0/0; UnitTest 71/71; SecurityTest 28/28; IntegrationTest 28/28; DatabaseTest 2/2; Stryker 100%.
## Estandarización SDD de specs (2026-10-05)
- Migradas 47 specs a `specs/_template.md` (Contexto/Requisitos/Diseño/Contratos/Tests/Criterios/Límites + Aprobación).
- 6 completadas con ✅ Aprobado (@arquitecto-principal, 04-Oct-2026): 02-01, 02-02, 02-03, 02-04, 03-00, 03-01 (con Detalle específico).
- 41 pendientes con 🚧 Borrador (sin revisor/fecha): 03-02…03-18 (17), 04-01…04-05, 06-01…06-04, 07-01…07-03, 08-01/08-02, 09-01…09-08, 10-testing-strategy/10-02.
- Verificacion: 57/57 spec.md con `## Aprobación y Control de Cambios` (16 ✅, 41 🚧); solo markdown, sin cambios de código.

## T2 — 03-01 Search/Autocomplete addendum (2026-10-06)
- Addendum en `specs/phase-03-api-catalog/03-01-cliente/` (`spec/plan/task-search-autocomplete.md`, 🚧 Borrador): mismo flujo `CliCliente`, sin tocar el ✅ T1.
- Decisiones: `page/pageSize` sueltos (sin `QueryParams`), DTO mínimo `{id, strNombreCliente}`, `Contains` basta; rutas `[HttpGet("search")]`/`[HttpGet("autocomplete")]`, `400` siempre `{ error }`, caché versionada 60s.
- Pendiente de implementar: DTO + servicio + controller + Unit/Integration/Security + Stryker ≥80%; al cerrar, fusionar Detalle en `spec.md` principal.

## T2 — 03-02 Search addendum (2026-10-06)
- Addendum en `specs/phase-03-api-catalog/03-02-empleado/` (`spec/plan/task-search.md`, 🚧 Borrador): mismo flujo `EmpEmpleado`, sin tocar el T1.
- Decisiones confirmadas: sin filtros → todo paginado; texto en nombre+apellidos con `Contains` (no CURP); `page/pageSize` sueltos sin `QueryParams`; `idTipoEmpleado<=0` → 400, id inexistente → vacío; ruta `[HttpGet("search")]`, caché versionada 60s.
- Pendiente de implementar: `SearchAsync` + controller + Unit/Integration/Security + Stryker ≥80%; al cerrar, fusionar Detalle en `spec.md` principal.

## T2 — 03-03 SearchByName addendum (2026-10-06)
- Addendum en `specs/phase-03-api-catalog/03-03-producto/` (`spec/plan/task-search-by-name.md`, 🚧 Borrador): mismo flujo `ProProducto`, sin tocar el T1.
- Decisiones confirmadas: `page/pageSize` sueltos sin `QueryParams`; `Contains` + `Trim()` en `strNombreProducto`; texto vacío → 400 `{ error }`; `[StringLength(50)]` espejo del modelo; ruta `[HttpGet("search")]`, caché versionada 60s.
- Pendiente de implementar: `SearchByNameAsync` + controller + Unit/Integration/Security + Stryker ≥80%; al cerrar, fusionar Detalle en `spec.md` principal.

## T1+T2 — 03-05 Search/Autocomplete integrados al CRUD base (2026-10-06)
- `specs/phase-03-api-catalog/03-05-usuario/` (`spec/plan/task.md` actualizados, 🚧 Borrador): SearchByName + Autocomplete incluidos en el CRUD base, sin addenda separados (decisión del usuario).
- Decisiones confirmadas: `page/pageSize` sueltos sin `QueryParams`; `Contains` + `Trim()` en `strNombre` (no correo, anti-enumeración); autocomplete `{id, strNombre}`; secretos jamás en DTOs/caché/logs (test de ausencia); rutas `[HttpGet("search")]`/`[HttpGet("autocomplete")]`.
- Pendiente de ejecutar: T1+T2 completos (CRUD con hashing + search/autocomplete + Unit/Integration/Security + Stryker ≥80%).

## T2 — 03-10 Search multifiltro (2026-10-06)
- `specs/phase-03-api-catalog/03-10-venta/` (`spec/plan/task.md` actualizados, 🚧 Borrador): T1 venta legacy intacto + T2 search (clave exacta + nombre `Contains` vía JOIN + rango inicio/fin).
- Decisiones confirmadas: `page/pageSize` sueltos sin `QueryParams`; `inicio>fin` → 400; sin filtros = todo paginado; `OrderBy(id)`; ruta `[HttpGet("search")]`, caché versionada 60s.
- Pendiente de ejecutar: T1+T2 (venta Tx + search + Unit/Integration/Security + Stryker ≥80%).

## T2 — 03-11 Autocomplete de productos (2026-10-06)
- `specs/phase-03-api-catalog/03-11-venta-detalle/` (`spec/plan/task.md` actualizados, 🚧 Borrador): T1 detalle legacy intacto + T2 autocomplete (`AutocompleteProductoAsync` sobre `ProProductos`).
- Decisiones confirmadas: DTO `{id, strNombreProducto}` propiedad de `03-03` y reutilizado (primera reutilización cross-slice); `Contains` basta sin filtro de stock; ruta `[HttpGet("autocomplete-productos")]`; caché `cache:producto:autocomplete:*` sobre `producto:version` existente.
- Pendiente de ejecutar: T1+T2 (detalle Tx + autocomplete + Unit/Integration/Security + Stryker ≥80%).

## T1+T2 — 03-05 SegUsuario ejecutado (2026-10-06)
- Slice: `Dtos/SegUsuarioDtos.cs` (Dto sin secretos + Create `{strNombre, strCorreo, strPasswordPlano}` required + Update `{id, RowVersion, strNombre, strCorreo}` sin password + Autocomplete `{id, strNombre}`), `Validators/SegUsuarioValidators.cs`, `Services/SegUsuarioPasswordHasher.cs` (`ISegUsuarioPasswordHasher` + `FakeSegUsuarioPasswordHasher` SHA256+sal fija, `NOTE (04-02)` — `TODO` rompe build por S1135), `Services/SegUsuarioService.cs` (CRUD+`SearchByNameAsync`/`AutocompleteAsync`, `Trim`+`Contains` en `strNombre`, `OrderBy(id)`, normalización `[1,50]→10`, caché `cache:usuario:*` 60s), `Controllers/V1/SegUsuarioController.cs` (`api/v1/usuarios`, `search`/`autocomplete` antes de `{id:int}`, `400 {error}`, `AdminPolicy`), DI en `Program.cs`, `stryker-0305.json`.
- Auto-set en `Create`: `dteFechaRegistro=UtcNow`, `bln2FAHabilitado=false`, `str2FASecreto=null`; `Update` nunca toca `strPWD` (verificado por test de hash inalterado).
- Tests: `UnitTest/SegUsuario` (26: CRUD+409+search pagina/trim/caché+autocomplete top-N/bordes `0/51/-5→10`/borde `50` no normalizado+ausencia secretos), `IntegrationTest/SegUsuario` (5: CRUD+search/autocomplete 200 sin secretos/400/403), `SecurityTest/SegUsuario` (7x401).
- Stryker: 100.00% (78/78, `stryker-0305.json` con `ignore-mutations Boolean`). Iteraciones: 97.44% (2 supervivientes: `Skip *→/` en search y `>50→>=50` en autocomplete) → 98.72% (1: `Skip`) → 100% (test `search` pág.2 `pageSize=2` vacía distingue `*2` vs `/2=0`; test borde `50` con 12 items distingue `>50` vs `>=50`).
- Lección nueva: S1135 convierte `TODO` en error de build (`TreatWarningsAsErrors`); usar `NOTE (XX-YY)` para stubs temporales con issue de reemplazo.
- Verificacion: build Release 0/0 (+`--no-incremental` restaurativo tras Stryker); UnitTest 97/97; SecurityTest 35/35; IntegrationTest 33/33; Stryker 100%.
- Estado spec: ✅ Aprobado (@arquitecto-principal, 06-Oct-2026); `spec.md` conciliado (nombres reales de archivos, hasher fake temporal, Stryker 100% real).

## T2 — 03-01 Search/Autocomplete ejecutado (2026-10-06)
- DTO `CliClienteAutocompleteDto {id, strNombreCliente}`; servicio `SearchByNameAsync`/`AutocompleteAsync` (`Trim`, `Contains` en `strNombreCliente`, `OrderBy(id)`, `AsNoTracking`, `ConfigureAwait(false)`); controller `[HttpGet("search")]`/`[HttpGet("autocomplete")]` con `400 { error = "El texto de búsqueda es requerido." }`, `texto>100→400`, `page/pageSize` inválido→400 solo en search.
- Decisiones cerradas: `AutocompleteAsync→IReadOnlyList<>` (igual que `SegUsuario`); llave autocomplete corregida y versionada `cache:cliente:autocomplete:{version}:{texto}:{max}`; `max∉[1,50]→10` en servicio (controller delega); mismo casing en tests InMemory.
- Tests: `UnitTest/CliCliente` (+9: search pagina/ordena/cachea/trim/vacío; autocomplete top-N/trim/bordes `0/51/-5→10`/borde `50`/vacío/caché; llaves `cache:cliente:search:1:…` y `autocomplete:1:…` + TTL 60s); `IntegrationTest/CliCliente` (+2: `SearchAndAutocompleteFlow` 200/`TotalCount`/400/`error`/`page=0`→400/top-5 sin PII/`0/51→10`OK + `SearchWithoutAdminRoleIsForbidden` 2×403); `SecurityTest/CliCliente` (+2×401).
- Lección: InMemory usa nombre fijo `WebApiDevSecOpsScallingSDD` compartido entre factories del mismo proceso; el test nuevo dejaba `Mariana` y rompía `CrudFlow` (`TotalCount` 1→2 según orden). Regla: todo test de integración que cree datos debe borrarlos al final (DELETE con `RowVersion` de la creación).
- Stryker: intentado con `stryker-0301.json` (`ignore-mutations Boolean`, ya cubre `CliClienteService.cs`); excede timeout interactivo 10 min (nightly 180 min) → queda para run nightly; `dotnet build` post-intento para limpiar binarios mutantes.
- Verificacion: build Release 0/0; UnitTest 106/106; IntegrationTest 35/35; SecurityTest 37/37.

## T2 — 03-02 Search ejecutado (2026-10-06)
- Servicio `SearchAsync(texto?, idTipoEmpleado?, page, pageSize)` (`Trim`, `Contains` OR en `strNombre/strAPaterno/strAMaterno` con null-guards, AND por tipo, `OrderBy(id)`, `AsNoTracking`, `ConfigureAwait(false)`); sin `EnsureTipoEmpleadoExistsAsync` en lectura (id inexistente `>0` → 200 vacío); sin cambios en `Dtos/` ni `Validators/`.
- Controller `[HttpGet("search")]` antes de `{id:int}`: `page/pageSize` inválido→400 (EN existente), `idTipoEmpleado<=0`→400 `"idTipoEmpleado debe ser mayor que 0."`, `texto>50`→400 `"El texto debe tener como máximo 50 caracteres."`; texto nulo/blanco se ignora (sin filtros = todo paginado).
- Llave canónica versionada `cache:empleado:search:{version}:{clean}:{tipo ?? "null"}:{page}:{pageSize}` TTL 60s; CA1305 obligó a interpolar (`$"{id}"`) en vez de `int.ToString()`.
- Tests: `UnitTest/EmpEmpleado` (+9: nombre/apellido paterno-materno con `strAMaterno=null`/combinado AND/tipo-solo/sin-filtros+cache/trim/blanco=todo/tipo-999 vacío/llave canónica/paginación pág.2 vacía; llaves `search:1:Key:null:1:20` y `search:0:CacheMe:1:1:20` + TTL); `IntegrationTest/EmpEmpleado` (+2: `SearchFlow` 200 nombre+apellido/sin-filtros/tipo-999→`TotalCount:0`/`0/-1`→400 `error`/`page=0`→400 con DELETE de limpieza + `SearchWithoutAdminRoleIsForbidden` 403); `SecurityTest/EmpEmpleado` (+1×401).
- Stryker: 98.70% primer run (1 superviviente aritmético `Skip (page-1)*pageSize → /pageSize`) → 100.00% tras test pág.2 (`page=2,pageSize=2` vacía distingue `*2` de `/2=0`, mismo patrón que 03-05). `stryker-0302.json` sin cambios (ya `ignore-mutations Boolean`).
- Verificacion: build Release 0/0; UnitTest 115/115; IntegrationTest 37/37; SecurityTest 38/38; Stryker 100%.

## T2 — 03-03 SearchByName ejecutado (2026-10-06)
- Servicio `SearchByNameAsync(texto, page, pageSize)` (`Trim`, `Contains` en `strNombreProducto`, `OrderBy(id)`, vacío defensivo sin tocar caché); controller `[HttpGet("search")]`: vacío→400 `"El texto de búsqueda es requerido."`, `>50`→400 `"El texto debe tener como máximo 50 caracteres."`, paginación→400 (EN heredado). Sin cambios en `Dtos/`/`Validators/` (validación inline, no atributo `[StringLength]` — el atributo daría `ValidationProblem`, no `{ error }`).
- Llave `cache:producto:search:{version}:{clean}:{page}:{pageSize}` TTL 60s.
- Tests: `UnitTest/ProProducto` (+5: pagina/ordena/cachea/trim/pág.2 vacía anti-`Skip *→/`/vacío sin caché/sin-coincidencias; llave `search:1:Key:1:20`); `IntegrationTest/ProProducto` (+2: `SearchByNameFlow` 200/sin-texto→400 `error`/`>50`→400/`page=0`→400 con DELETE de limpieza + `SearchWithoutAdminRoleIsForbidden` 403); `SecurityTest/ProProducto` (+1×401).
- Stryker: 100.00% al primer run (test pág.2 incluido desde el inicio por lección 03-02). `stryker-0303.json` sin cambios.
- Verificacion: build Release 0/0; UnitTest 120/120; IntegrationTest 39/39; SecurityTest 39/39; Stryker 100%.

## T1 — 03-06 Login ejecutado, alcance reducido con NOTEs (2026-10-06)
- Alcance cerrado con usuario: 401 genérico + lockout + validación ahora; 429 (04-04), JWT real (04-01) y rehash (04-02) diferidos. La propia `spec.md` Límites ya difería JWT y rate limiting; la task pedía los 4.
- Slice: `Dtos/LoginDtos.cs` (`LoginRequest {strNombre, strPasswordPlano}` por `strNombre`, `LoginResponse {Token}`), `Validators/LoginRequestValidator.cs` (NotEmpty + longitudes espejo), `Services/LoginService.cs` (`ILoginService.AuthenticateAsync` → `LoginResult {Authenticated|InvalidCredentials|LockedOut}`), `Controllers/V1/LoginController.cs` (`POST api/v1/auth/login`, `[AllowAnonymous]`), DI en `Program.cs` (sin `LoginPolicy`: la ruta es anónima; sin `UseRateLimiter`).
- Anti-enumeración: mismo body 401 para usuario inexistente y password mala (assert de igualdad en tests) + verify fake contra `_dummyHash` (`Hash("login-anti-enumeration-dummy")`, agnóstico al hasher futuro).
- Lockout: `attempts:{nombre}` contador TTL 120s + `lockout:{nombre}` al 5º fallo; 1–5 → 401, siguiente → 423. TTL 120s es el máximo de `CacheService` (lanza si mayor) + `NOTE (04-02)` para 15 min. Password jamás en llaves/logs.
- Token opaco 32B (`RandomNumberGenerator`) + `NOTE (04-01)` JWT HS256. Interacción 2FA → 03-07.
- Tests: `UnitTest/Login` (11: nulos, éxitotoken+limpia-intentos, idéntico-401, blancos/nulos sin caché, 5-fallos→423+intentos-0, desconocido-bloquea, llaves/TTL, NoTracking, semilla dummy vía `RecordingHasher`), `IntegrationTest/Login` (4: flujo 200+Token, 401 idénticos, 5-fallos→423, 400; nombres `Login*` anti-colisión + DELETE limpieza), `SecurityTest/Login` (2: 401 sin fugas, 400).
- Stryker: 80.65% primer run (6: `?? string.Empty` sin cubrir por no probar nulos, `RegisterFailure` desconocido sin test de bloqueo, `"locked"→""` y `RemoveAsync` post-lock sin assert, semilla dummy inobservable) → 100.00% tras: `!string.IsNullOrEmpty(locked)` (mata `""`), tests nulos/bloqueo-desconocido/intentos-0 y `RecordingHasher` que fija la semilla. `stryker-0306.json` nuevo.
- Lecciones nuevas: CA2007 sí aplica en helpers async de tests (los `[Fact]` no lo disparan); CA1861 prohíbe `new[]{...}` en asserts (`Assert.Single` + índice); CA1308 prohíbe `ToLowerInvariant` (pasar correo explícito).
- Verificacion: build Release 0/0; UnitTest 131/131; IntegrationTest 43/43; SecurityTest 41/41; Stryker 100%.
- Estado spec: ✅ Aprobado (@arquitecto-principal, 06-Oct-2026); `spec.md`/`plan.md` conciliados (nombres reales, sin `LoginPolicy`, 429/rehash/JWT diferidos, Stryker 100% real).

## T1 — 03-07 Login 2FA ejecutado, alcance reducido con NOTEs (2026-10-06)
- Alcance cerrado con usuario: temp opaco 120s + TOTP fake + token opaco ahora; JWT `2fa_temp` (04-01), TTL 5min/lockout 15min (04-02), TOTP real+setup (03-09), 429 (04-04) diferidos. Cache TTL máx 120s y `token` prohibido en llaves impedían el alcance completo.
- Slice: `Dtos/Login2FaDtos.cs` (`Login2FaVerifyRequest {TempToken, TotpCode}` + `Login2FaVerifyResponse {Token}`), `LoginDtos.LoginResponse` extendida (`Requires2fa`, `TempToken?`, compatible), `Validators/Login2FaValidators.cs` (NotEmpty + `^\d{6}$`), `Services/TotpService.cs` (`ITotpService` + `FakeTotpService` `123456` con secreto no vacío, `NOTE (03-09)`), `Services/Login2FaService.cs` (`ILogin2FaService.VerifyAsync` → `Authenticated|InvalidCredentials|LockedOut`), `Controllers/V1/Login2FaController.cs` (`POST api/v1/auth/login2fa/verify`, `[AllowAnonymous]`), DI en `Program.cs`.
- `LoginService` bifurca tras password OK: `bln2FAHabilitado` → `RequiresTwoFactor` + temp hex 64 (`ToHexString`, no Base64, para no tropezar con `password/secret/token` de `CacheService`) en `cache:login2fa:{hex}` TTL 120s (`NOTE 04-01/04-02`); `LoginController` mapea a 200 `{Requires2fa:true, TempToken}`. Rama solo se activa con flag true: tests 03-06 intactos.
- Defensa: gate nulo/vacío/no-hex sin tocar caché + `IsHexToken` (temps atacante con `token` → 401 limpio, no 500); dummy `_totp.Verify("login2fa-dummy")` anti-enumeración; temp un solo uso (replay 401); lockout `attempts:/lockout:` 5→423 igual que 03-06; secreto/password jamás en llaves/logs/DTOs.
- Tests: `UnitTest/Login2fa` (18: `Login2FaServiceTests` 14 + `LoginRequires2FaTests` 4), `IntegrationTest/Login2fa` (4: flujo, 401 idénticos, 423, 400; 2FA habilitado vía `AppDbContext` scoped + DELETE limpieza), `SecurityTest/Login2fa` (2: 401 sin fugas, 400).
- Stryker: 0307 87.76% (6: `?? string.Empty` en temp, 2 `_=Verify` dummy sin assert, `RemoveAsync` enmascarado por limpieza previa en issue, 2 `ThrowIfNull` fake) → 97.96% (1 lógico `||→&&`) → 100.00% tras: guard `is null` explícito, `RecordingTotp` en borrado, wrong-antes-de-éxito para intentos≠0, throws de nulos en fake, casos 1-solo-vacío. 0306 re-run 100.00% con rama 2FA. `stryker-0307.json` muta `Login2FaService.cs`+`TotpService.cs`.
- Lecciones nuevas: Sonar S101 exige `2Fa` (no `2fa`) en tipos; xUnit1030 prohíbe `ConfigureAwait(false)` en cuerpos `[Fact]` (solo helpers, complementa CA2007); `ignore-mutations Boolean` NO filtra `||→&&` (es `Logical`, hay que matarlo con tests); bug cazado por assert fuerte: test usaba `Eva2Fa` vs usuario `Eva2fa` (igualdad case-sensitive).
- Verificacion: build Release 0/0; UnitTest 150/150; IntegrationTest 47/47; SecurityTest 43/43; DatabaseTest 2/2; Stryker 100% x2 (+build restaurativo tras cada run).
- Estado spec: ✅ Aprobado (@arquitecto-principal, 06-Oct-2026); `spec.md`/`plan.md`/`task.md` conciliados (nombres reales `2Fa`, hex, NOTEs).

## README raíz creado (2026-10-06)
- `README.md` nuevo en inglés, nivel completo, sin tabla de fases (decisión del usuario): Overview con SDD explícito, stack real del `.csproj`, setup, configuración, tabla de 8 controllers, testing/Stryker con conteos medidos (150/47/43/2, 100% x2), decisiones clave con NOTEs, workflow SDD + roadmap y gaps honestos (deploy/workflows/checklist pendientes).
- Solo documentación; sin cambios de código ni tests.

## .gitignore endurecido DevSecOps (2026-10-06)
- Añadidos: `.env`/`.env.*`, certificados (`*.pfx/.pem/.key/.p12/.crt/.cer/.jks`), `secrets.json` bare (user secrets; `*.secrets.json` no lo matcheaba), `*.publishsettings`, cobertura extendida (`*.coveragexml/*.cobertura.xml/coverage.cobertura.xml/*.opencover.xml`), DBs locales (`*.db/.mdf/.ldf/.sdf/.sqlite`), residuos (`.DS_Store/Thumbs.db/*.orig/*.bak`).
- Auditoría previa: cero secretos reales (solo placeholders, dummies de test y credenciales Testcontainers efímeras); `nuget.config` y `Directory.Build.props` limpios; `stryker-030*.json` quedan trackeados a propósito.
- Decisión usuario: `appsettings.json` reales siguen trackeados con placeholders + `skip-worktree` local (documentado en `README.md`); refactor `AppSettingsTests` a env/`appsettings.Test.json` queda como pendiente mediano plazo.

## T1 — 03-08 Refresh & Logout ejecutado, alcance opaco con NOTEs (2026-10-06)- Alcance cerrado con usuario: opaco + NOTE (04-01), 2 controllers separados, logout fallback jti=claim ?? hash(refresh) + NOTE. JWT real/blacklist larga/429 quedan en 04-01/04-04.
- Slice: `Dtos/RefreshDtos.cs` (`RefreshRequest/RefreshResponse{Token,RefreshToken}/LogoutRequest`), `Validators/RefreshValidators.cs` (NotEmpty Max200 x2), `Services/RefreshTokenService.cs` (`IRefreshTokenService.Create/Rotate/Revoke/LogoutAsync` → `RefreshResult{Rotated|Invalid}`; SHA-256 hex 64 en `strTokenHash`, rotación vía `strReplacedByTokenHash`, expiración 7d NOTE, `DbUpdateConcurrencyException`→Invalid), `Controllers/V1/RefreshController.cs` (`POST refresh`, `[AllowAnonymous]`, 200/401 genérico) + `LogoutController.cs` (`POST logout`, `[Authorize]`, claim `jti`/`NameIdentifier` o fallback, 204), DI en `Program.cs` (servicio + 2 validadores, sin tocar auth scheme).
- Guardarraíles: sin substring `token` en llaves (solo `blacklist:{jti}`=`revoked` TTL 120s NOTE 04-02; filas refresh solo en DB, sin `cache:refresh`); sin `System.IdentityModel` (jti como literal, evita paquete nuevo); tokens hex 64 nunca en logs/bodies de error.
- Tests: `UnitTest/RefreshToken` (9: nulos-ctor, create-hash-solo, rotación+link, reúso→Invalid x2, desconocido/expirado/no-hex/nulo→Invalid, revoke-una-vez, logout-fallback-blacklist+TTL, logout-claim, logout-vacío-sin-caché), `IntegrationTest/Refresh` (5: rota+reúso-401+segunda-rotación-200, desconocido-401-sin-eco, 400, logout-204+luego-401, logout-sin-auth-401; `TestAuthHandler` + `IRefreshTokenService.CreateAsync` scoped + DELETE limpieza), `SecurityTest/Refresh` (3: 401-sin-fugas, 400, logout-anónimo-401).
- Lecciones: xUnit1030 prohíbe `ConfigureAwait(false)` en cuerpos `[Fact]` de Integration (solo helpers; las 3 líneas del primer build fallaron por esto); `SegUsuario.CrudFlow TotalCount==1` es frágil en paralelo con store InMemory compartido (1/52 falló con Docker recién arrancado, 52/52 al repetir; Refresh y SegUsuario 5/5 aislados) — no tocar fuera de slice, documentar flake.
- Verificacion: `dotnet restore` (faltaban assets en phase03) → build Release 0/0; UnitTest 159/159; SecurityTest 46/46; IntegrationTest 52/52 (rerun; primer run 51/52 flake `SegUsuario` paralelo); `check_coverage.py` no ejecutable (`python` ausente, stub fase 07).

## T1 — 03-10 Venta síncrona ejecutado (2026-10-06)
- Alcance cerrado con usuario: Bearer `[Authorize]`, `idSegUsuario` en body + `NOTE (04-01)`, 409-todo-conflicto (criterio `4×400`→`4×409` reescrito), race en MsSql Testcontainers.
- Slice: `Dtos/VenVentaDtos.cs` (`VenVentaDto`+detalle, `VenVentaCreateDto`+item; `IReadOnlyList` por CA2227/CA1002, `required` en valores por S6964; total servidor `decPrecio×piezas`, fecha servidor), `Validators/VenVentaValidators.cs` (FKs>0, clave NotEmpty Max10, detalles NotEmpty + item>0), `Services/VentaService.cs` (`IVentaService.Create/GetById`; FK triple-check→`ValidationException`→422, stock/concurrencia→`ConcurrencyConflictException`→409, Tx explícita solo `IsRelational`, `venta:version` TTL 60s), `Controllers/V1/VentaController.cs` (`POST` 201 `CreatedAtAction` + `GET {id}` auxiliar, fila añadida en `03-17`), DI en `Program.cs`. `UpdateDto/DeleteDto` diferidos (sin rutas en catálogo).
- Tests: `UnitTest/Venta` (10: servicio 8 + validadores 2), `IntegrationTest/Venta` (`VentaControllerTests` 5 con FKs vía API + DELETE limpieza + `IntegrationTest/Venta/RaceConditionTests` MsSql puerto 14336 + estado `race`: 5 paralelos → 1×201+4×409, stock 0), `SecurityTest/Venta` (2×401).
- Stryker `stryker-0310.json` (`VentaService`, Boolean-ignore): **83.93%** (47 killed/8 survived/1 no-coverage/2 compile-error; gaps: strings de llaves caché, `OrderBy` dirección, `!=` detalles) — gate ≥80% cumplido; build restaurativo tras run.
- Lecciones nuevas: EF InMemory eleva `TransactionIgnoredWarning` a error con warnings-as-errors → Tx explícita condicionada a `IsRelational` (precedente `DatabaseSeeder`); `await using` dispara CA2007 en IntegrationTest (usar try/finally como `ProviderStatesTests`); DTOs con colección mutable fallan CA2227+CA1002 → `IReadOnlyList` con setter (como `PagedResult`).
- Verificacion: build Release 0/0; UnitTest 169/169; SecurityTest 48/48; IntegrationTest 58/58 (incluye race 37s).
- Estado spec: 🚧 Borrador con evidencia T1 (pendiente firma; T2 search pendiente).

## T2 — 03-10 Search multifiltro ejecutado (2026-10-06)
- `Services/VentaService.cs` +`SearchAsync(clave?, nombre?, inicio?, fin?, page, pageSize)`: JOIN `CliCliente` (`idCliCliente`), clave exacta `Trim()==`, nombre `Contains` `Trim()`, rango sobre `dteFechaHoraCompra` (nulos excluidos solo con filtro), `OrderBy(id)`, detalles por venta en 2ª query, caché `cache:venta:search:{version}:{clave}:{nombre}:{o}:{o}:{page}:{size}` TTL 60s (reusa `venta:version` de T1).
- `Controllers/V1/VentaController.cs` `[HttpGet("search")]` (params sueltos, sin `QueryParams`): clave>10 → 400, nombre>100 → 400, `inicio>fin` → 400, paginación → 400; hereda `[Authorize]` Bearer.
- Tests: `UnitTest/Venta/VentaSearchTests.cs` (7: exacta/trim, JOIN, rango+nulos, AND, paginado, nulos-sin-filtro, caché/TTL/llave), `VentaControllerTests` +4 (multifiltro 200, rango invertido 400, paginación/filtros 400, rol `User` → 200), `SecurityTest/Venta` +1 (search anónimo 401).
- Desviación: `403` no aplica en search (Bearer sin policy; `User` → 200 verificado; espejo `03-06`); registrada en spec/task.
- Lección: xUnit2013 prohíbe `Assert.Equal` para tamaño de colección (`Assert.Single`); mi expectativa inicial en JOIN (2 vs 3 ventas de Ana) la cazó el propio test.
- Verificacion: build 0/0; UnitTest 176/176; SecurityTest 49/49; IntegrationTest 62/62; Stryker 82.11% (gate ≥80; T1 era 83.93%) + build restaurativo.
- Estado spec: 🚧 Borrador con evidencia T1+T2 (pendiente firma).

## CI PR mínimo para desbloquear checks del PR #1 (2026-10-06)
- Causa: no existía `.github/workflows/` en el repo → Checks "no jobs"; protección de rama pedía build/test/semgrep inexistentes.
- Nuevo `.github/workflows/ci-pr.yml` (commit `4891548` en `phase03`): triggers `pull_request→main` + `push→phase03`; jobs `build` → `unit`/`security`/`integration` (`--no-build`, orden 09-01) + `semgrep` (`semgrep ci --config=auto --config=.semgrep/semgrep.yaml --error --metrics=off`); `ubuntu-latest` (Docker para Testcontainers), `setup-dotnet 10.0.x`, timeouts 15/30min. Sin mutation/perf/chaos (solo nightly, fase 09 completa pendiente).
- Lección: sin `python` local se validó el YAML con `npx -y js-yaml` (parse OK).
- El workflow va en `phase03` a propósito: GitHub lee los workflows de la rama head, solo así corren en el PR #1 (nuevo commit puede requerir re-aprobación si el repo descarta reviews obsoletas).

## Fix NU1100 PackageSourceMapping en CI (2026-10-06)
- Síntoma: job `build` del PR #1 fallaba en `dotnet restore` exit 1 (runner pristino), local verde.
- Causa: `Testcontainers.*` (punto literal) no matchea el ID desnudo `Testcontainers`, y faltaban patrones para transitivos `Pipelines.Sockets.Unofficial`, `Humanizer.Core`, `Mono.TextTemplating` → NU1100 "source(s) were not considered".
- Fix en `nuget.config`: patrones exactos `Testcontainers` + los 3 transitivos; eliminada línea duplicada `Testcontainers.*`. Mapeo como control supply-chain intacto.
- Verificado con `dotnet restore --force` (re-resolución total como runner pristino) + build 0/0.

## Fix NU1100 iterativo hasta lista exhaustiva (2026-10-06)
- El fix de 4 patrones no bastó: el head `7a9c11d` seguía fallando en `restore` en CI.
- Lección clave: `restore --force` con caché tibia NO revalida el mapeo; reproducir runner pristino exige carpeta de paquetes vacía: `dotnet restore --force --no-cache /p:RestorePackagesPath=<temp>`.
- Enumeración iterativa: `SSH.NET` + `SharpZipLib` (transitivos Testcontainers.MsSql), luego `BouncyCastle.Cryptography` (transitivo de SSH.NET); verificación final con carpeta vacía → 0 NU1100.
- Tras el fix: `dotnet restore` normal + build Release 0/0 (workspace limpio).
