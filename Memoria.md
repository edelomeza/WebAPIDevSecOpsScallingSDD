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

## Cierre PR #1 Phase03 mergeado a main (2026-10-06)
- `edelomeza` mergeó 10 commits (`f290815`) con 10 checks verdes tras eliminar la regla fantasma `build & test` de las rules (check requerido sin productor: ningún workflow define ese nombre; los jobs reportan `ci-pr / <job>`).
- `main` local sincronizado (`7194992`→`f290815`, fast-forward, 31 archivos) + build 0/0 + UnitTest 176/176 en `main`.
- Specs firmadas: `03-08` y `03-10` → ✅ Aprobado (@usuario, 06-Oct-2026).

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

## Fix flake paralelo + semgrep metrics (2026-10-06)
- CI del PR #1: `VenCatEstado.CrudFlow` falló (`TotalCount==1` no hallado) + `semgrep scan` exit 2 (`--config=auto` incompatible con `--metrics=off`).
- Flake: 10 asserts exactos `TotalCount==1` en 5 clases Integration preexistentes sobre store InMemory compartido en paralelo; nuestros tests de Venta ensanchan la ventana de colisión. Fix sistémico sin tocar tests: `IntegrationTest/xunit.runner.json` (`parallelizeTestCollections: false`, registrado en csproj) — item pendiente del checklist `10-testing-strategy`.
- Semgrep: quitado `--metrics=off` (desviación registrada vs literal AGENTS.md §6, pensado para `ci`; `scan` lo rechaza).
- Verificado: IntegrationTest 62/62 en serie (4m35, dentro del timeout CI 30min); Unit 176/176; Security 49/49.

## SAST pinneado de actions (2026-10-06)
- Primer `semgrep scan` real en CI: 0 hallazgos en C# (118 archivos), 10 bloqueantes `github-actions-mutable-action-tag` todos en `ci-pr.yml`.
- Fix: las 10 refs a SHAs inmutables (`checkout`/`setup-dotnet`/`setup-python` resueltos vía API al momento) + comentario `# v4/# v5` (Dependabot los sigue actualizando).
- Lección: pinear actions desde el día 1; el SAST ya pagó en su primera corrida.

## T1+T2 — 03-11 VentaDetalle ejecutado (2026-10-06)
- Slice: `Dtos/VenVentaDetalleDtos.cs` (Create `{idProProducto, intPiezaVenta}` required + Delete `{id, RowVersion}`), `Validators/VenVentaDetalleValidators.cs` (`>0`), `Dtos/ProProductoDtos.cs` +`ProProductoAutocompleteDto {id, strNombreProducto}` (propiedad 03-03, primera reutilización cross-slice), `Services/VentaDetalleService.cs` (`GetById/AddDetalle/RemoveDetalle/AutocompleteProductoAsync`), `Controllers/V1/VentaDetalleController.cs` (base `ventas/detalles` + `POST ~/ventas/{idVenta}/detalles` absoluto + `DELETE {id}` + `GET {id}` + `GET autocomplete-productos`, `[Authorize]` Bearer), DI en `Program.cs`, `stryker-0311.json`.
- Ownership: `venta.idSegUsuario` vs claim `NameIdentifier/sub` (`NOTE 04-01`); sin claim o ajeno → `UnauthorizedAccessException`→`Forbid` 403; `TestAuthHandler` + header opcional `X-Test-UserId` (sin header = comportamiento intacto).
- Stock: alta descuenta y baja restaura en Tx explícita solo `IsRelational`; insuficiente → 409; `RowVersion` añadido a `VenVentaDetalleDto` (+`VentaService.ToDto`) para DELETE concurrente.
- Autocomplete: `Trim`+`Contains` en `strNombreProducto`, `OrderBy(id)`, `max∉[1,50]→10`, llave `producto:autocomplete:{version}:{clean}:{n}` TTL 60s sobre `producto:version`.
- Bug cazado por test nuevo: el alta dejaba stale `producto:{id}` (60s) entre dos lecturas de existencia → el servicio invalida también `producto:{id}` + `producto:version` en add/remove.
- Tests: `UnitTest/VentaDetalle` (23: T1 + borde exact-stock + mensajes + valores-versión + autocomplete top-N/trim/bordes/borde-50/vacío/caché), `IntegrationTest/VentaDetalle` (5: flujo 201/204/restore + 403 ajeno + 404 + autocomplete 200/400/bordes + `User→200` + 400 mismatch), `SecurityTest/VentaDetalle` (4×401).
- Stryker 86.05% primer run (6: mensaje-`$""` producto, `<→<=` borde, mensaje-`""` ownership, `+1→-1` versión) → 90.70% tras asserts de mensaje/borde-exacto/valores-versión; resto (2 Tx relacional + 6 NoCoverage relacional) solo cubrible en MsSql, fuera del scope UnitTest — gate ≥80% cumplido con margen.
- Verificacion: build Release 0/0; UnitTest 199/199; SecurityTest 53/53; IntegrationTest 67/67 (rerun tras arrancar Docker Desktop detenido, como en 03-02); `dotnet build` restaurativo tras cada Stryker.
- Estado spec: ✅ Aprobado (@arquitecto-principal, 07-Oct-2026, firma en `phase03.2` sin cambios sobre la evidencia).

## T1 — 03-12 VentasPedido mínimo ejecutado (2026-10-07)
- Slice acotado sin bus real: `Dtos/PedidoDtos.cs` (Create `{idCliCliente, Detalles[{idProProducto, intCantidad}]}` required + Response `{Guid id, decTotal, strEstadoSaga, RowVersion}`), `Validators/PedidoValidators.cs` (`>0`, detalles `NotEmpty`), `Events/PedidoCreadoEvent.cs` (POCO `{PedidoId, ClienteId, Total}` + `NOTE 06-03`), `Services/PedidoEventPublisher.cs` (`IPedidoEventPublisher` + `FakePedidoEventPublisher` + `NOTE 06-01`), `Services/VentasPedidoService.cs` (`Create/GetById`, total servidor `decPrecio×cantidad`, SIN descuento de stock, estado temporal `"Creado"` + `NOTE 06-04`, `Guid` cliente → único `SaveChanges` en Tx solo `IsRelational`, caché `cache:pedido:{id}` + `pedido:version` TTL 60s), `Services/StockValidatorConsumer.cs` (stub solo-lectura `HasStockAsync` + `NOTE 06-02`), `Controllers/V1/VentasPedidoController.cs` (`POST/GET {id:guid} api/v1/ventas/pedido`, `AdminPolicy` + `NOTE 04-04`, `422 {error}` FKs + `409` concurrencia, `try/catch` manual hasta `03-16`), DI en `Program.cs`, `stryker-0312.json`.
- Diferencia clave vs venta legacy: el pedido NO muta `intNumeroExistencia` (verificado por test `CreateDoesNotDiscountStock`); el stock lo valida el consumer en `06-02`.
- Tests: `UnitTest/VentasPedido` (13: nulos, vacíos, FKs con mensaje + sin publish, total+evento+caché/TTL, orden-2-detalles, stale-caché, consumer true/false/sin-pedido, validators), `IntegrationTest/Saga` (5: flujo 201/200 + stock intacto + 422 FKs + 400 + 404 + `User→403`), `SecurityTest/Saga` (2×401).
- Stryker 76.00% primer run (9: llave-`$""`, `return cached`, `OrderBy`+`==` en GetById, `IsRelational`, 1er `SaveChanges` equivalente, `OrderBy` en Create, `RemoveAsync`+llave) → 85.71% tras test stale-caché + asserts de llave + reestructura a único `SaveChanges` (posible por `Guid` cliente) → 87.76% tras orden-2-detalles en GetById; resto (2 compile-error `Count`, 1 `IsRelacional`, 3 NoCoverage relacional, 2 `RemoveAsync` equivalente en `Guid` fresco) fuera del scope UnitTest — gate ≥80% cumplido con margen.
- Lecciones: S3267 prohíbe bucle solo-validación (fusionar validación+cómputo en un `foreach` con `TryGetValue`, espejo `VentaService`); Stryker foreground excede timeout 20min → correr detached (`Start-Process -WindowStyle Hidden`) y sondear log; Docker Desktop estaba detenido otra vez (misma regla 03-02/03-11).
- Verificacion: build Release 0/0; UnitTest 212/212; SecurityTest 55/55; IntegrationTest 72/72 (rerun tras arrancar Docker + re-run tras reestructura); `dotnet build` restaurativo tras cada Stryker.
- Estado spec: ✅ Aprobado (@arquitecto-principal, 07-Oct-2026, firma en `phase03.2` sin cambios sobre la evidencia; eliminada línea duplicada de `Desviaciones` en `spec.md`; trabajo futuro → `06-01/06-02/06-03/06-04`, `04-04`, `03-16` vía `NOTE`s).

## Skills actualizadas con experiencia 03-01…03-12 (2026-10-07)
- 17 SKILL.md tocados + 2 nuevos (PR #3 mergeado a `main`): el análisis encontró skills genéricas/desactualizadas frente a 12 lecciones probadas.
- Updates: `testing-mutation` (reescritura: thresholds 80/80/90, configs por slice, catálogo mata-mutantes, detached + build restaurativo); `testing-webappfactory` (headers `X-Test-Role/UserId`, matriz 403/200/401, DELETE-limpieza, serie); `testing-testcontainers` (puertos fijos + daemon); `testing-strategy` (serie, conteos reales 212/72/55); `07-static-analysis` (apunta a quickref); `05-cache-redis` (esquema canónico + stale `03-11`); `03-api-vertical-slice` + `core/vertical-slice` (checklist probado); `03-saga-endpoints` (fake-first permitido); `06-events-saga` (Fase 8→06 + reemplazo fake→real); `06-saga-state-machine` (conflicto nombres `Pendiente` vs `Creado/Registrado`, canónico en `06-04`); `03-auth-endpoints` (realidad opaca + doubles); `core/auth-matrix` (`AdminPolicy` real + `NOTE 04-04`); `traceability` + `spec-first-writing` (Borrador-vs-Aprobado, bitácora 4 capas); `09-ci-matrix` (SHA, semgrep scan, NU1100 pristino); `07-quality-supply-chain` (Stryker real + source mapping).
- Nuevas: `core/deferred-scope-fakes` (Fake+`NOTE`, bitácora 4 capas, `grep NOTE` al llegar a fase dueña) y `testing/analyzer-quickref` (tabla 14 reglas S6964/CA2227/S3267/CA2007/xUnit1030/CA1305/CA1308/CA1861/xUnit2013/S101/S1135/pragmas/S1541).
- Verificacion: solo markdown (sin código); revisado y autorizado por el usuario (07-Oct-2026).

## Cierre PR #3 phase03.1 mergeado a main (2026-10-07)
- `edelomeza` mergeó 1 commit (`d8a3fb8`) vía PR #3 (`8ab5209`): 03-11 VentaDetalle T1+T2 + 03-12 VentasPedido mínimo + 17 skills + 2 nuevas.
- `main` local sincronizado (fast-forward a `8ab5209`) + build 0/0 + UnitTest 212/212 en `main`.
- Specs `03-11` y `03-12` firmadas a ✅ Aprobado (`@arquitecto-principal`, 07-Oct-2026) en `phase03.2` (`0d7ff7f`, solo local).
- Nota: `gh` local sin auth (401) — el PR se creó por web; el push usó credential helper de git.
## T1 — 03-13 VentasPago mínimo ejecutado (2026-10-07)
- Slice acotado sin bus/consumer (decisión del usuario): `Dtos/PagoDtos.cs` (`PagoCreateDto` `{idVenPedido required Guid, decMonto required >0, strMetodoPago?(Max50), strIdTransaccion?(Max100)}` + `PagoResponseDto`), `Validators/PagoValidators.cs` (`NotEmpty/>0/Max50/Max100`), `Services/VentasPagoService.cs` (`IVentasPagoService.Create/GetById/GetByPedidoId`, estado temporal `"Procesado"` + `NOTE 06-04`, `dteFechaPago=UtcNow`, `Trim`+vacío→`null`, duplicado no-nulo pre-chequeo + `DbUpdateException`→`ConcurrencyConflictException`→409, caché `cache:pago:{id}` + `cache:pago:pedido:{version}:{pedidoId}` + `pago:version` TTL 60s), `Controllers/V1/VentasPagoController.cs` (`POST/GET {id:int}/GET pedido/{pedidoId:guid} api/v1/ventas/pago`, `AdminPolicy` + `NOTE 04-04`, `422 {error}` FKs + `409` duplicado/concurrencia, `try/catch` manual hasta `03-16`), DI en `Program.cs`, `stryker-0313.json`.
- `VenPedidoPago` ya existía (fase 02); índice único filtrado admite múltiples `NULL` (verificado por test `NullTransaccionAllowsDuplicates`).
- Tests: `UnitTest/VentasPago` (14: nulos, FK con mensaje + sin insert, duplicado→409 con mensaje, `NULL`/blanco→`null`, create+estado+caché/TTL/llaves, versión 2 tras 2 creates, stale-caché, por-pedido ordenado+caché, vacío, validators, 2 rutas concurrencia vía `ThrowingContext`), `IntegrationTest/Saga/VentasPagoTests.cs` (7: flujo 201/200 + por-pedido + 409 + 422 + 400 + 404 + vacío + 403 `User`; TX única por test vía `Guid` por store InMemory compartido), `SecurityTest/Saga/VentasPagoSecurityTests.cs` (3×401).
- Stryker 86.36% primer run (2 survived `RemoveAsync` en id fresco — equivalentes, mismo criterio que 03-12 — + 4 NoCoverage en `catch` concurrencia) → 95.45% tras 2 tests `ThrowingContext` (subclase `AppDbContext` con `ThrowOnSave`; matan statement+string de ambos `catch`); resto 2 survived equivalentes fuera del scope — gate ≥80% cumplido con margen.
- Lecciones: CA2007 rechaza `new` inline en `await using` dentro de `[Fact]` (extraer helper `CreateThrowingContext`, complementa xUnit1030); `node -e` para parsear `mutation-report.json` cuando PowerShell enreda la navegación; Docker Desktop estaba detenido otra vez (misma regla 03-02/03-11/03-12).
- Verificacion: build Release 0/0; UnitTest 226/226; SecurityTest 58/58; IntegrationTest 79/79; DatabaseTest 2/2; `dotnet build` restaurativo tras cada Stryker.
- Estado spec: ✅ Aprobado (@usuario, 08-Oct-2026, firma sin cambios sobre la evidencia; trabajo futuro → `06-01/06-02/06-03/06-04`, `04-04`, `03-16` vía `NOTE`s; fila `03-17` actualizada con nombre real `PagoResponseDto`).
## T1 — 03-14 VentasFactura mínimo ejecutado (2026-10-08)
- Slice de solo lectura sin POST/folio/consumer (decisión del usuario en plan): `Dtos/FacturaDtos.cs` (`VenPedidoFacturaResponseDto`, nombre real según catálogo `03-17`), `Services/VentasFacturaService.cs` (`IVentasFacturaService.GetByIdAsync`, caché `cache:factura:{id}` TTL 60s, `NOTE`s 06-02/06-04/04-04), `Controllers/V1/VentasFacturaController.cs` (`GET {id:int} api/v1/ventas/factura`, `AdminPolicy`, 200/404 sin `try/catch` de dominio), DI en `Program.cs`, `stryker-0314.json`.
- `VenPedidoFactura` ya existía (fase 02); motivo técnico del diferimiento: `CacheService` solo expone `Get/Set/Remove` sin `Increment` atómico y TTL máx 120s → folio `F-{año}-{seq}` a fase 06.
- Tests: `UnitTest/VentasFactura` (5: nulos, miss-sin-caché, persistencia+caché/llave/TTL, stale-caché, mapeo RFC/fecha), `IntegrationTest/Saga/VentasFacturaTests.cs` (3: flujo 200 con inserción vía `AppDbContext` scoped + 404 + 403 `User`; limpieza cliente/producto vía API), `SecurityTest/Saga/VentasFacturaSecurityTests.cs` (1×401).
- Stryker 100.00% al primer run (11 killed, 6 ignored por `ignore-mutations Boolean` + block-removal; servicio de solo lectura sin ramas de escritura) — gate ≥80% cumplido con margen.
- Lecciones: servicio de solo lectura no necesita `VersionKey` ni `InvalidateAsync` (menos mutantes, 100% al primer intento); inserción en Integration vía `factory.Services.CreateScope()` + `GetRequiredService<AppDbContext>()` comparte el store InMemory (precedente 03-07 para 2FA); fila `03-17` ya coincidía (sin cambios).
- Verificacion: build Release 0/0; UnitTest 231/231; SecurityTest 59/59; IntegrationTest 82/82; DatabaseTest 2/2; `dotnet build` restaurativo tras Stryker + re-verificación Unit/Security.
- Estado spec: 🚧 Borrador con evidencia T1 (pendiente firma; `POST`/folio/consumer → fase 06).
## Cierre PR #4 phase03.2 mergeado a main (2026-10-08)
- Merge `96dc6e7` (PR #4 `phase03.2` → `main`): 03-13 VentasPago mínimo (`7e348bb`) + spec/plan/task y fila `03-17`.
- `main` local sigue en `8ab5209` (sin sincronizar); `origin/main` ya va en `96dc6e7`.
- Nota: firma 03-13 a Aprobado (`48ae579`) pusheada a `origin/phase03.2` tras el merge — aún no está en `main`; entrará con el próximo PR.
## Cierre PR #5 phase03.2 mergeado a main (2026-10-08)
- Merge `78113c0` (PR #5 `phase03.2` → `main`): firma 03-13 a Aprobado (`48ae579`) + cierre PR #4 en Memoria (`6bc1241`).
- `main` local sincronizado en `78113c0` + build 0/0 + UnitTest 226/226 en `main`.
- Spec `03-13` firmada y publicada en `main`; slice 03-13 cerrado por completo.

## Skills actualizadas con experiencia 03-13/03-14 (2026-10-08)
- 11 SKILL.md tocados, 0 nuevos (todo encajaba en existentes; `03-legacy-sales` y `testing-testcontainers` sin gap), directo en rama por decisión del usuario.
- Updates: `analyzer-quickref` (CA2007: `new` inline en `await using` en `[Fact]` → helper; tope …03-14); `testing-mutation` (catálogo `ThrowingContext` 03-13, regla GET-only sin `VersionKey` 03-14, precedentes + refs 0313/0314); `testing-webappfactory` (seeding vía `AppDbContext` scoped sin `POST`, `Guid` por test; eliminado `## Límites/trampas` duplicado); `testing-strategy` (conteos reales 231/82/59/2 @08-Oct + reglas scoped/`Guid`); `powershell-quirks` (script `.js` + `node` para `mutation-report.json`); `03-api-vertical-slice` (slice acotado con `NOTE`s, `ThrowingContext`, refs 03-07/03-13/03-14); `03-saga-endpoints` (`Trim`/pre-chequeo→409, `NULL` múltiple, folio diferido + matriz (a)/(b)/(c), entidades pre-existentes); `05-cache-redis` (sin `Increment` + TTL máx, excepción GET-only); `deferred-scope-fakes` (alternativa GET-only sin fake, rama "catálogo ya correcto"); `06-events-saga` (deuda POST/folio/consumer 03-14); `02-migrations-seed` + `02-domain-data` (nota de únicos filtrados en ambas).
- Verificacion: solo markdown (`git status`: 12 SKILL.md + entrada Memoria; resto del diff es trabajo 03-14 pendiente de commit).
## Cierre PR #6 phase03.3 mergeado a main (2026-10-08)
- Merge `97a6658` (PR #6 `phase03.3` → `main`): 03-14 VentasFactura GET-only mínimo (`9ed4ec9`) + actualización de 11 SKILL.md con experiencia 03-13/03-14 (`204ff92`).
- `main` local sincronizado en `97a6658` + build 0/0 + UnitTest 231/231 en `main`.
- Spec `03-14` firmada a ✅ Aprobado (@usuario, 08-Oct-2026) en `phase03.4` (firma sin cambios sobre la evidencia; `POST`/folio/consumer → fase 06).
- Nota: `phase03.3` se conserva por decisión del usuario (no eliminar).
## Cierre PR #7 phase03.4 mergeado a main (2026-10-08)
- Merge `ae73198` (PR #7 `phase03.4` → `main`): firma 03-14 a Aprobado (`438e1ac`) + cierre PR #6 en Memoria.
- `main` local sincronizado en `ae73198` + build 0/0 + UnitTest 231/231 en `main`.
- Spec `03-14` firmada y publicada en `main`; slice 03-14 cerrado por completo (queda `POST`/folio/consumer → fase 06).
## T1 — 03-15 VentasDashboard con filtros ejecutado (2026-10-08)
- Slice con filtros (decisión del usuario en plan): `Dtos/DashboardDtos.cs` (`DashboardFilterDto {Desde, Hasta, EstadoSaga}` + `DashboardDto {TotalPedidos/Pagos/Facturas, MontoTotalPedidos/Pagos/Facturas, PorEstadoSaga[{Estado, Total}], ProfundidadCola}` sin secretos/RFC/folios), `Validators/DashboardValidators.cs` (`Hasta>=Desde` + `EstadoSaga Max50`), `Services/DashboardService.cs` (`IVentasDashboardService.GetAsync`, `AsNoTracking`, `Trim`+vacío→`null`, `OrderBy(Estado)`, cola `0` + `NOTE 06-01`, caché `cache:dashboard:*` TTL 60s sin `VersionKey` + sanitiza `password/secret/token/":"`), `Controllers/V1/VentasDashboardController.cs` (`GET api/v1/ventas/dashboard`, `AdminPolicy`, `[FromQuery]` sueltos, `400` vía `ValidationProblem`), DI en `Program.cs`, `stryker-0315.json`.
- Rango sobre fecha propia de cada entidad (`dteFechaPedido/Pago/Emision`); estado solo filtra pedidos; fila `03-17` actualizada a `200/400/401/403` con query params.
- Tests: `UnitTest/VentasDashboard` (13: servicio 8 + validador 5, borde inclusivo en 3 tablas, llaves exactas `Creado`/`_-_-_-x`, ausencia sensibles), `IntegrationTest/Saga/VentasDashboardTests.cs` (3: flujo + filtros/400 + 403 `User`; asserts `>=1` por store InMemory compartido, limpieza cliente/producto vía API), `SecurityTest/Saga/VentasDashboardSecurityTests.cs` (1×401).
- Stryker 72% primer run (13: 8 `Equality` fecha pagos/facturas, 4 `String "_"→""`, 1 `IsNullOrWhiteSpace→!=`) → 100.00% tras borde en pagos/facturas + asserts exactos de llave (47 killed, 1 compile-error `Count→Sum`, 28 ignored `Boolean`); `dotnet build` restaurativo tras cada run.
- Lecciones: Docker Desktop detenido otra vez (misma regla 03-02/03-11/03-12/03-13); `dotnet stryker` v5 usa `-f|--config-file` (`--config` es `Unrecognized option`); S3041/S3241 vigentes en tests (`InsertFactura` → `void` al no usar el id).
- Verificacion: build Release 0/0; UnitTest 244/244; SecurityTest 60/60; IntegrationTest 85/85; DatabaseTest 2/2; `dotnet build` restaurativo tras Stryker + re-verificación Unit/Security.
- Estado spec: 🚧 Borrador con evidencia T1 (pendiente firma; bus/compensación → fase 06, métricas → `08-01`).
## Cierre PR #8 phase03.5 mergeado a main (2026-10-08)
- Merge `5ccf73c` (PR #8 `phase03.5` → `main`): 03-15 VentasDashboard con filtros (`aa64fa5`).
- `main` local sincronizado en `5ccf73c` + build 0/0 + UnitTest 244/244 en `main`.
- Spec `03-15` firmada a ✅ Aprobado (@usuario, 08-Oct-2026) en `phase03.6` (firma sin cambios sobre la evidencia; bus/compensación → fase 06, métricas → `08-01`).
- Nota: `phase03.5` se conserva por decisión del usuario (no eliminar).
## Cierre PR #9 phase03.6 mergeado a main (2026-10-08)
- Merge `3b89e40` (PR #9 `phase03.6` → `main`): firma 03-15 a Aprobado (`0743789`) + cierre PR #8 en Memoria + skills 03-15.
- `main` local sincronizado en `3b89e40` + build 0/0 + UnitTest 244/244 en `main`.
- Spec `03-15` firmada y publicada en `main`; slice 03-15 cerrado por completo (queda bus/compensación → fase 06, métricas → `08-01`).
- Nota: `phase03.6` se conserva por decisión del usuario (no eliminar).

## Fase 1 Sub-agentes hibridos ejecutado (2026-10-08)
- Nuevos: `.opencode/agents/slice-scaffolder.md` (Fase A compilable, test que aserta cada diferido, snippet DI + fila 03-17; sin campo `name:` — el filename es el nombre segun docs OpenCode; `permission` distingue roles), `.opencode/agents/security-reviewer.md` (`edit: deny`, diff-scoped vs `origin/main`, 4 bloqueantes + avisos), `.opencode/agents/README.md` (convencion: enlazan SKILL.md, nunca copian; `traceability-clerk` diferido a Fase 2), `scripts/critic-guardrails.ps1` (PS 5.1, exit 0/1).
- Critic mixto por decision: bloqueante solo 4 estructurales diff-scoped (TODO, secretos Dtos/cache salvo `blacklist:{jti}`, auth 1-de-3 patrones `AdminPolicy|[Authorize]|[AllowAnonymous]`, catch nuevo en Controllers); 401/S1541/Stryker van a tests via PR template. Full-repo grep descartado: la deuda pre-03-16 legitima fallaria cada PR.
- Modificados: `pull_request_template.md` (3 -> 8 checks), `ci-pr.yml` (+job `critic` paralelo sin `needs`, mismo SHA checkout, `shell: pwsh`), `AGENTS.md` §11.
- Self-test critic: scratch con TODO+sin-auth+catch+password -> FAIL 4/1 aviso, exit 1; tras borrar -> PASS exit 0; PASS sobre el diff real del PR. Leccion: untracked no salen en `git diff HEAD` (anadido `ls-files --others`); `# Todo` en prosa dispara el regex (reword a `# Ambito`); untracked sin diff requieren scan directo para B4.
- Bugs del script cazados al resolver el merge a `main` (hardening real, no teorico): (1) `git fetch` escribe `From https://...` a stderr y con `$ErrorActionPreference='Stop'` eso es terminating **aunque haya `2>$null`** — el `try/catch` lo tragaba y el PASS era vacuo; fix `Invoke-Git` (`2>&1` + filtrar `ErrorRecord`, EAP `Continue` localizado). (2) El fallback local solo corria con diff vacio, asi que un untracked pasaba si el diff no era vacio; fix union de ambos ambitos. (3) El mensaje PASS no decia cuantos ficheros escaneo; ahora `PASS (N files scanned)`. Re-verificado: PASS 9 ficheros exit 0 + FAIL 2/1 aviso exit 1 con scratch.
- Verificacion: YAML parse `npx js-yaml` OK; build Release 0/0; UnitTest 244/244; SecurityTest 60/60; IntegrationTest 85/85; 7 skills referenciadas existen. Semgrep queda a CI (sin C# nuevo). Dry-run scaffolder en `exp/` pendiente como follow-up.
- Estado: resuelto conflicto de merge con `origin/main` (Cierre PR #9) en `phase03.6`; pendiente PR a `main`.

## README actualizado al estado 03-15 + sub-agentes (2026-10-08)
- `README.md` (ingles) reconciliado con la realidad 08-Oct: lede (saga minima + dashboard verdes), tabla 8 -> 16 controllers con 3 patrones de auth + matriz viva 03-17, `Measured state` 244/85/60/2 + gate Stryker >=80% (corrige "100%" que contradecia 03-10/03-12), configs `0301..0315`, `Key decisions` +3 (refresh/logout opaco, venta legacy, saga fake-first), `Rules learned` +7 (serie InMemory, NU1100 pristino, semgrep scan, SHA pineados, `IsRelational`, `ThrowingContext`, `NOTE` vs S1135), roadmap (aprobados 03-00..03-08 + 03-10..03-15; pendientes 03-09/03-16/03-17 viva) y gaps (workflows ya existen: 6 jobs + `critic`; coverage/semgrep stubs).
- Verificacion: solo markdown, critic PASS exit 0; sin cambios de codigo (sin build/tests requeridos).

## Cierre PR #11 phase03.6 mergeado a main (2026-10-08)
- Merge `4ef3be9` (PR #11 `phase03.6` ÔåÆ `main`): Fase 1 sub-agentes hibridos (`07bb89f` merge + `e16d840` agentes/critic/README + `4bb2689` hardening critic).
- `main` local sincronizado en `4ef3be9` + build 0/0 + UnitTest 244/244 en `main` (sin push: solo fast-forward local).
- Fase 1 cerrada por completo en `main`: 2 sub-agentes, `critic` en CI, PR template 8 checks, AGENTS ┬º11, README al dia.

## Push phase03.7 a origin (2026-10-08)
- Incluye `0bf4a41` 03-09 T1 espec real (`task.md` 5+/3-: OtpNet +-1, DTOs/validators, `stryker-0309.json`, `SegUsuario`/`Program.cs`, diferidos NOTE 04-01/04-02/04-04) + 6 previos (Fase 1 + hardening critic + cierres PR #10/#11).
- Solo markdown/spec, sin cambios de codigo (sin build/tests requeridos).

## T1 — 03-09 TwoFactor ejecutado en rama `error` (2026-10-08)
- Fases A–D con las 5 correcciones del análisis en plan mode: `ITotpProvisioner`
  nueva (Fake intacto), prefijos `attempts:`/`lockout:` + llave `2fa:{userId}`,
  `Login2FaService` desprotege antes de Verify, setup sin DTO/validador,
  stubs controlables en unit + TOTP real solo en integración.
- Nuevos: `OtpNetTotpService` (Otp.NET 1.4.1, `GenerateRandomKey(20)`, ventana
  ±1), `TwoFactorSecretProtector` (DataProtection `"TwoFactor"`),
  `TwoFactorService`, `TwoFactorController` (`api/v1/two-factor`), DTOs +
  único `TwoFactorVerifyRequestValidator`, `stryker-0309.json`.
- Decisiones: payload medido 155 < `nvarchar(200)` (sin migración); `SetupAsync`
  retorna null ante id inválido/ausente (sin try/catch en controller, canónico
  03-16; 401 idéntico); `RemoveAsync`
  de `lockout:` en success eliminado (inaccesible: con lockout verify retorna
  antes); `new TwoFactorResult{}` en éxito es equivalente conocido
  (`Enabled=0`); `catch (FormatException)` eliminado (OtpNet solo lanza
  `ArgumentException`, probado empíricamente).
- Verificación: build 0/0; Unit 275/275 (+31), Security 62/62 (+2), Integration
  TwoFactor 6/6 + Login2Fa 4/4 (Docker ausente: Testcontainers no ejecutables
  aquí); Stryker 68.42% → 79.57% → 94.62% → **93.41%** final tras refactor
  post-critic (break 80, 4 runs).
- Spec/plan/task 03-09 retro-portados (Borrador con evidencia); `nuget.config`
  suma `Otp.*`. Commiteado y pusheado en rama `error` (2026-10-08).
- Post-cierre: critic `scripts/critic-guardrails.ps1` 1 FAIL restante
  (`Secret` en `TwoFactorDtos.cs:5`) con waiver: el secreto de enrollment se
  devuelve UNA vez por requerimiento 03-09 T1 (flujo TOTP estándar); jamás va
  a logs/caché/DB en claro (tests `DoesNotContain` + protegido en reposo).
  Refactor post-critic: `SetupAsync` retorna null (sin try/catch en controller,
  canónico 03-16) + NOTE `(prod)` fusionado a `(03-09)` → critic solo ese FAIL;
  Stryker re-corido tras el refactor.

## Cierre PR #13 error mergeado a main (2026-10-09)
- Merge `0b1dc99` (PR #13 `error` → `main`): 03-09 TwoFactor T1 real (`82b13fb`).
- `main` local sincronizado en `0b1dc99` + build 0/0 + UnitTest 275/275 en `main`.
- Spec `03-09` firmada a ✅ Aprobado (@usuario, 09-Oct-2026); slice 03-09 cerrado
  por completo (waiver critic `Secret` enrollment documentado; diferidos NOTE
  04-01/04-02/04-04).
- Ramas `error` local y `origin/error` eliminadas tras el merge (a petición del
  usuario; las `phase03.*` se conservan).

## T1 — 03-16 Errors & HTTP ejecutado en rama `phase03.9` (2026-10-09)
- `ExceptionHandlingMiddleware` (único try/catch) + `ErrorResponse`
  (`Error/Status/TraceId`, `Detail` solo no-prod omitido en prod) + tipos
  `NotFoundException`/`ForbiddenAccessException`; pipeline: middleware primero,
  fuera `UseExceptionHandler` no-Dev (orden 01-02 intacto).
- Purga try/catch en 8 controllers + 21 `NotFound()`→throw + `EnsureOwner`→403;
  EmpEmpleado FK servicio 400→422 (único outlier); 10 factories de integración
  a `Staging` (en Dev la página de excepciones devolvería HTML);
  `Production` exige override `UseInMemoryDatabase=true`.
- Sondas `GET /api/v1/probe/{timeout,error,forbidden}` gated
  `EnableProviderStates` + no-prod.
- Verificación: build 0/0; Unit 289/289 (+14), Security 62/62, Integration
  Errors 8/8 (+37/37, 55/56 con solo Redis-Docker ambiental); Stryker **100%**
  (break 80, 1 run, `stryker-0316.json`); critic PASS (1 aviso GET-only).
- Spec/plan/task 03-16 retro-portados. Commiteado y
  pusheado en rama `phase03.9` (2026-10-09).
- Estado spec: ✅ Aprobado (@usuario, 09-Oct-2026, firma sin cambios sobre la evidencia; merge PR #15).

## Cierre PR #15 phase03.9 mergeado a main (2026-10-09)
- Merge `738ae13` (PR #15 `phase03.9` → `main`): 03-16 Errors T1 real (`8c484a5`).
- `main` local sincronizado en `738ae13` + build 0/0 + UnitTest 289/289 en `main`.
- Spec `03-16` pendiente de firma (Borrador con evidencia).
- Ramas `phase03.9` local y `origin/phase03.9` eliminadas tras el merge (a
  petición del usuario).

## Skills actualizadas con experiencia 03-15/03-09/03-16 + Fase 1 (2026-10-09, rama `phase03.10`)
- 23 SKILL.md actualizados + 3 nuevos, en un solo batch en la rama actual (decisión del usuario).
- Nuevas: `operations/critic-guardrails` (dueña de `scripts/critic-guardrails.ps1`: 4 bloqueantes diff-scoped, `Invoke-Git 2>&1` + EAP `Continue`, unión diff+untracked, `PASS (N files)`, waiver `Secret` 03-09); `phases/04-totp-provisioning` (OtpNet ±1, DataProtection `TwoFactor`, `ITotpProvisioner`, `2fa:{userId}`, `SetupAsync→null`, Stryker 93.41%); `phases/03-errors-middleware` (canon `ErrorResponse` + middleware único + sondas `probe` gateadas + factories `Staging`, Stryker 100%).
- Updates Auth/2FA (5): `03-auth-endpoints`, `04-jwt-refresh` (realidad opaca + 03-09), `04-security-core`, `core/auth-matrix` (fila two-factor), `core/deferred-scope-fakes` (swap 03-09 ejecutado).
- Updates Slice/Errores/Fase 1 (7): `03-api-base`, `03-api-vertical-slice`, `core/vertical-slice`, `03-saga-endpoints`, `03-legacy-sales` (canon `EnsureOwner→throw`), `core/traceability`, `core/spec-first-writing` (gates Fase A + critic + evidencia 0316); higiene: Checklist duplicados eliminados en `03-api-vertical-slice`/`traceability`/`07-static-analysis`/`05-cache-redis`/`testing-testcontainers`, `## Pasos` duplicado de `03-saga-endpoints` a `Detalle por endpoint` + `AdminOnly` corregido a `AdminPolicy`.
- Updates Testing/Cache/CI (11): `testing-mutation` (refs 0309/0316 + tope …03-16), `testing-webappfactory` (`Staging`/`probe`/TwoFactor/`>=1`), `testing-strategy` (conteos 289/85/62/2 + estrategias TwoFactor/Errors), `analyzer-quickref` (filas S3041 + waiver `Secret` + canon 03-16), `powershell-quirks` (hardening `Invoke-Git`), `09-ci-matrix` (job `critic` + 8 checks), `09-cicd-ops` (sincronizado con ci-matrix), `07-static-analysis` + `07-quality-supply-chain` (tope + scores), `05-cache-redis` (dashboard sin `VersionKey` + `2fa:{userId}`), `testing-testcontainers` (solo nota ambiental).
- Verificación: solo markdown (sin código); `scripts/critic-guardrails.ps1` → PASS antes del commit.

## Cierre PR #16 phase03.10 mergeado a main (2026-10-09)
- Merge `86af0ac` (PR #16 `phase03.10` → `main`): 3 skills nuevas + 23 updates (`f0c508c`).
- `main` local sincronizado en `86af0ac` (fast-forward, working tree limpio).
- Skills cerradas por completo en `main`: 47 `SKILL.md` (44 + 3 nuevas) con evidencia 03-15/03-09/03-16 + Fase 1.

## Cierre PR #17 phase03.11 mergeado a main (2026-10-09)
- Merge `8a497a6` (PR #17 `phase03.11` → `main`): 03-17 Endpoint Catalog opción B (`1008a10`: `docs/endpoints.md` 56 filas + `check_endpoints.ps1` + job `endpoints` + spec/task/plan).
- Rama `phase03.11` local recreada desde el `main` actualizado (la anterior estaba 100% mergeada; working tree limpio, nada que arrastrar).

## T1 — 03-17 Endpoint Catalog ejecutado, opción B (2026-10-09, rama `phase03.11`)
- `docs/endpoints.md` nuevo (56 filas: 7 auth/misc + 1 ping raíz + 3 `probe` solo no-prod + 31 CRUD/search/autocomplete + 14 ventas/saga; rate-limit como `NOTE 04-04`; nota `ErrorResponse` post-`03-16`).
- Hallazgo del inventario: `GET /ping` raíz (texto `"pong"`, `Program.cs:25`) existe además de `GET /api/v1/ping` del controller; las sondas `probe/*` son minimal APIs en `Program.cs:217-223` (no hay `ProbeController`), gate `EnableProviderStates` + no-prod.
- `spec.md` conciliado (6 correcciones: `AdminOnly→AdminPolicy`, `TwoFactorSetupRequest` eliminado — `setup` sin DTO —, `Usuario*→SegUsuario*`, rutas faltantes, rate `NOTE`, códigos `409/422/403`); `task.md`/`plan.md` reescritos a formato estándar; skill `09-ci-matrix` +1 job.
- `scripts/check_endpoints.ps1` nuevo (PS 5.1, sin dependencias): extrae `[HttpX]`+`[Route]` de los 17 controllers + `MapGet` de `Program.cs`, respeta placeholders `{idVenta:int}` al normalizar, exige fila `| VERBO | ruta |`; primer run cazó 3 gaps reales (placeholders + `/ping` no inventariado) → `OK (56 rutas)` tras fixes; job `endpoints` en `ci-pr.yml` (paralelo, `shell: pwsh`).
- Verificación: script `OK exit 0` + `critic PASS (8 files) exit 0` + YAML parse OK; sin código de producto (sin build/tests).
- Estado spec: ✅ Aprobado (@usuario, 09-Oct-2026, firma sin cambios sobre la evidencia).

## T1 — 03-18 JSON Contracts ejecutado, fixtures + test (2026-10-09, rama `phase03.11`)
- `ContractTest/Fixtures/*.json` (14, fuente única): captura real del wire con `CONTRACT_CAPTURE=1` (`FixtureCaptureTests`: ping → usuarios/clientes/productos/estados → paged/autocomplete → login → refresh vía `IRefreshTokenService.CreateAsync` scoped → pedido → pago → venta → dashboard → 404; `IntegrationTest/Common/` no existe y no se crea).
- `JsonContractTests` (existencia + convención + sin secretos): el primer run tumbó el "PascalCase estricto" del spec (`id`, `strNombre`, `bln2FAHabilitado`, `idCliCliente`) → convención real codificada en `IsConventional` (prefijos legacy + `id`/sufijo + resto PascalCase).
- Correcciones: `VenPedidoPagoResponseDto`→`PagoResponseDto`, `Login2faVerifyResponse`→`Login2FaVerifyResponse`; PactNet + provider real + fixture 2FA diferidos a fase 10 (sin paquete `Pact*` hoy; TOTP real sin fake determinista).
- Cableado: `ContractTest.csproj` (Mvc.Testing + TestHost + ProjectReference API + `NoWarn CA1515`), `ContractAuthHandler`, `xunit.runner.json` en serie; analizadores que mordieron: CA1515 + xUnit1030 (fixes del quickref).
- CI: job `contract` nuevo en `ci-pr.yml` (InMemory, sin Docker); skill `testing-pact` engordada con la fase previa.
- Verificación: build 0/0; ContractTest 4/4; critic PASS.
- Estado spec: ✅ Aprobado (@usuario, 09-Oct-2026, firma sin cambios sobre la evidencia; Pact/`Login2FaVerifyResponse` → fase 10).

## Firmas 03-16/03-17/03-18 + cierre addenda T2 (2026-10-09, rama `phase03.11`)
- Firmados sin cambios: `03-16` (merge PR #15), `03-17` (merge PR #17), `03-18` (en `phase03.11`, PR pendiente) → ✅ Aprobado (@usuario, 09-Oct-2026).
- Addenda T2 cerrados como fusionados: `03-01/spec-search-autocomplete.md`, `03-02/spec-search.md`, `03-03/spec-search-by-name.md` → ✅ Aprobado (fusionado y cerrado); línea T2 añadida al Detalle de cada `spec.md` principal (puntero al addendum + `docs/endpoints.md`). Los T2 de `03-05/03-10/03-11` ya vivían integrados en sus principales Aprobados (sin addendum separado por decisión del usuario).
- Fase 03 al 100%: 19/19 specs principales ✅ + 3 addenda cerrados. Verificación: solo `.md`; `critic PASS` antes del commit.

## Skills actualizadas con experiencia 03-17/03-18/cierres (2026-10-09, rama `phase03.11`)
- 10 SKILL.md tocados + 1 nueva, solo `.md` (sin push por decisión del usuario).
- Fix contradicción: `03-api-base` exigía "PascalCase puro" y el test de `03-18` lo tumbó → convención legacy medida (`IsConventional`) + fuentes únicas (`docs/endpoints.md`, `ContractTest/Fixtures`).
- Sync CI/testing: `09-ci-matrix` (+job `contract`), `09-cicd-ops` (jobs `endpoints`+`contract`), `testing-pact` (cross-links), `testing-strategy` (contract 4 + serie `ContractTest` + excepción flujo único de captura).
- Patrones metodología: `traceability` (firma masiva + cierre addenda con línea T2), `spec-first-writing` (no duplicar tablas vivas), `powershell-quirks` (guards sin dependencias + normalización de placeholders), `analyzer-quickref` (fila `ContractTest` 03-18 + `NoWarn CA1515`; tope …03-18 en `07-static-analysis`).
- Nueva: `operations/drift-guards` (dueña del patrón canónico + extractor + job CI; guards vigentes: `check_endpoints`).
- Verificación: `critic PASS` antes del commit.

## AGENTS.md sincronizado con la realidad 03-18 (2026-10-09, rama `phase03.11`)- 10 fixes: convención legacy medida en §1 (NO PascalCase puro), banner fase 03 retirado, `ContractTest` en comandos, orden CI real (`critic→endpoints→contract→semgrep`), `check_endpoints`+`critic` en §4, Contract por captura (Pact→fase 10), semgrep sin `--metrics=off`, quirks `ContractTest` + `analyzer-quickref`, §11 con 3 agentes + `drift-guards`, structure (`docs/`, `.opencode/`, `ContractTest` real).
- §3 +2 límites (no PascalCase puro, no duplicar tablas vivas). `constitution.md` raíz no se crea (opción A: la canónica vive en `specs/phase-00-constitution/`).
- Verificación: solo `.md`; `critic PASS` antes del commit.

## Sub-agentes para fase 04: 2 updates + 1 nuevo (2026-10-09, rama `phase03.11`)
- `slice-scaffolder`: `last-synced` al día + variante swap fake→real (precedente `03-09`: `grep NOTE`, re-Stryker, waiver) + enlaces a skills nuevas (`04-totp-provisioning`, `03-errors-middleware`, `drift-guards`).
- `security-reviewer`: `last-synced` al día + 5 checks fase 04 (JWT estrictos, anti-degradación de hash, rate-limit con fila `04-04`, headers/CORS, secretos en logs); WARN hasta ejecutar su spec, FAIL desde su merge.
- Nuevo `traceability-clerk` (adelantado de Fase 2): matrices `04-04`/`04-05` + addenda verificados contra código, `edit deny`, solo reporta gaps; evita checklist ASVS ficticio.
- `README.md` de agentes actualizado (tabla + skills enlazadas).
- Verificación: solo `.md`; `critic PASS` antes del commit.

## README.md reconciliado + areas SDD/sub-agentes (2026-10-09, rama `phase03.11`)
- 7 falsedades corregidas (03-09/03-16/03-17 pendientes, ContractTest placeholder, Stryker hasta 0315, PascalCase, 16 controllers, middleware sin ExceptionHandling, CI sin endpoints/contract, counts 08-Oct, TOTP fake) + matriz canonica a `docs/endpoints.md`.
- Nuevas secciones `## SDD components` (10 componentes con puntero) y `## Sub-agentes` (tabla 3 agentes + convencion hibrida); roadmap (fase 03 19/19 aprobada, pendiente PR #18) y `Key decisions` (TOTP real, `IsConventional`).
- Verificacion: solo `.md`; `critic PASS` antes del commit.

## Cierre PR #19 phase03.11 mergeado a main (2026-10-09)
- Merge `5306119` (PR #19 `phase03.11` → `main`): 03-18 (`1309dc2`: 14 fixtures + ContractTest 4/4 + job `contract`) + firmas/addenda (`a5530f0`) + skills (`bd8a5eb`) + sub-agentes fase 04 (`2a2ec14`) + AGENTS.md (`56874c7`) + README (`5ce6979`).
- `main` local sincronizado en `5306119` (fast-forward, working tree limpio).
- Fase 03 cerrada por completo en `main`: 19/19 specs ✅ + 3 addenda cerrados + catálogo vivo + fixtures + docs operativos (AGENTS.md, README.md) al día.

## Reconciliación .md 04-01-jwt-refresh (2026-10-09, solo docs)
- `spec/plan/task.md` reescritos como delta JWT sobre `03-08` ✅ (no recrean slice opaco): `Program.cs` Anonymous → JwtBearer HS256 + swap `NOTE (04-01)` en `RefreshTokenService.cs`; blacklist `NOTE (04-02)` intacta; claves `01-04` solo env; catálogo en `docs/endpoints.md` sin duplicar.
- Skills enlazadas (no copiadas): `phases/04-jwt-refresh`, `phases/03-errors-middleware`, `operations/drift-guards`, `operations/critic-guardrails`, `core/traceability`.
- Estado: 🚧 Borrador (pendiente ejecución, sin revisor); gates definidos (Fase A `slice-scaffolder` + `@security-reviewer` + `critic`/`endpoints`).
- Verificación: solo `.md`; `critic PASS` antes del commit.

## T1 — 04-01 JWT & Refresh ejecutado (2026-10-09)
- Alcance acotado a Refresh/Logout (decisión usuario): Login/Login2Fa/TwoFactor/Venta conservan `NOTE (04-01)` opaco para `04-02/04-03`.
- Nuevo `Services/JwtTokenService.cs` (`IJwtTokenService.CreateAccessToken`: `sub/jti/role`, HS256, exp 15min, key ≥32B fail-fast con fallback placeholder si ausente) + `Program.cs` JwtBearer tras flag `Authentication:UseJwtBearer` (default `true`; `false` = `Anonymous` legacy; Integration/Contract fuerzan `Test` vía `ConfigureTestServices`) con `TokenValidationParameters` estrictos + `OnTokenValidated` anti-`blacklist:{jti}`; `RefreshTokenService` inyecta `IJwtTokenService` en `Create/Rotate` (hash/rotación/revocación intactos).
- Paquete autorizado `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12`; `nuget.config` sin cambios (`Microsoft.*` ya mapea); bins unificados IdentityModel 8.19.2 (MSB3277 solo ruido).
- Config: `Authentication:UseJwtBearer` en tabla `01-04` + `appsettings.Example.json` (sin firma nueva en `01-04`, solo fila + Detalle).
- Tests: `UnitTest/Jwt` (5) + `SecurityTest/Jwt` (5: alg-none/firma/expirado/reúso/logout-blacklist-replay); `RefreshTokenServiceTests` 9 con 1 test renombrado por formato JWT (ver `Desviaciones` en `spec.md`).
- Verificación: build 0 + Unit 294/294 + Security 67/67 + Integration 92/92 (7 Docker-only excluidos: sin daemon local) + Contract 4/4 + `critic PASS (11)` + `endpoints OK (56)`; Stryker slice → nightly (tope local 25 min, umbral ≥83.93%).
- Lecciones: `ClaimTypes.Role` no sobrevive al mapa outbound (payload sin `role`) → literal `"role"` en emisión (inbound lo eleva a `Role` para `AdminPolicy`); `EndsWith(char)` + `string.Concat/AsSpan` exigidos por CA1865/CA1845; CS8601 en indexer `IConfiguration` → local + ternaria.
- Estado spec: 🚧 Borrador con evidencia T1 (pendiente firma; `security-reviewer` pre-push en curso).

## Cierre PR #20 phase04.1 mergeado a main (2026-10-09)
- Merge `1f33c2d` (PR #20 `phase04.1` → `main`): T1 `04-01` (`2c0a295`: JwtBearer tras flag + swap opaco→JWT + `UnitTest/Jwt` + `SecurityTest/Jwt` + `01-04`/`Example.json` + tríada `.md`).
- `main` local sincronizado en `1f33c2d` (fast-forward, working tree limpio) + build 0 + `critic PASS`; creada `phase04.2` desde `main` (hereda lo comiteado; sin pendientes sin commitear).
- Spec `04-01` → ✅ Aprobado (@usuario, 09-Oct-2026, firma sobre PR #20 mergeado).

## T1 — 04-02 Credential Protection ejecutado (2026-10-09)
- Hasher: `Argon2IdSegUsuarioPasswordHasher` (`Konscious.Security.Cryptography.Argon2 1.3.1`, 64MB/3iter, `DegreeOfParallelism=Min(4,CPU)` interno, PHC `$argon2id$v=19$...`) + `BCrypt.Net-Next 4.0.3` solo `Verify`; interfaz `+= NeedsRehash`; `Fake` intacto `=>false`; `PasswordHasherOptions` (`PasswordHasher:` sin secretos); DI factory en `Program.cs`; `nuget.config` `Konscious.*`/`BCrypt.Net*`.
- Lockout opción (b): tabla `SegBloqueo` (`strNombre` único, `intIntentosFallidos`, `dteBloqueoHasta`, migración `SegBloqueo0402`) + `ILoginLockoutStore`/`EfLoginLockoutStore` (`TimeProvider`, 1 reintento `DbUpdateConcurrencyException` fail-closed, rearme limpio); `LoginService` migrado (ctor 4 args), `CacheService` 30–120s intacto; `Login2Fa/TwoFactor`/blacklist en caché como deuda.
- Rehash-escritura diferida (`AsNoTracking`+`RowVersion`); `strPWD nvarchar(200)` basta (PHC ≤120c; salt >32B ⇒ 256); seed placeholder/fake ⇒ reset password (fail-closed).
- Tests: `PasswordHasherTests` 10 (OK/KO, BCrypt legacy, fresco/desconocido, nulos, Fake-nulos, coste-cero, opciones custom, `p=` fijado, batería 14 malformados) + `LoginServiceTests` 14 (expiración +14:59/+15:01, borde exacto 15:00, rearme sin recastigo, re-bloqueo tras 5 más) + `LoginLockoutStoreTests` 6 (reintento fresco/existente/expirado/doble-fallo con `FlakyDbContext`, reset con conflicto, reset limpio) + `AntiEnumerationTests` 3 (timing <10s) + `NoLeakTests` 4 casos; ctor 4 args en `Login2fa/*Tests`.
- Verificación: build 0 + Unit 316/316 + Security 74/74 + Integration 92/99 (7 Docker-only excluidos: sin daemon local, igual que `04-01`) + Contract 4/4 + `critic PASS` + `endpoints OK (56)`; Stryker local `stryker-0402.json` (UnitTest + `test-case-filter FullyQualifiedName~UnitTest.` para excluir Docker) → **90.85%** (142 mutantes; 13 equivalentes: guardas misma-excepción, bloques fail-closed, guarda `<=0`, estado inalcanzable).
- Lecciones: S1135 caza `todo` en minúsculas dentro de comentarios (redecir "cada hash…"); S101 exige `Argon2Id` (D mayúscula); `dotnet ef` sin factory falla (DbContext sin `OnConfiguring`) → factory temporal + borrar; migración con `_` rompe CA1707 → renombrar a `SegBloqueo0402`; BCrypt no fluye transitivo a UnitTest → `PackageReference` directa; bloquear por `SegUsuario` no cubre desconocidos → tabla por nombre; Stryker corre los 5 proyectos de test (478 tests, 9 Docker-fallos) → `test-case-filter FullyQualifiedName~UnitTest.` (5 min vs 75 min); mutante string `"m="→""` sobrevive porque `AsSpan(2)` compensa (longitud del prefijo): matarlo exige hashes crafteados con prefijo contrabandeado; `Verify` fail-closed con `catch when` ante hashes corruptos (nunca 500).
- Estado spec: ✅ Aprobado (@usuario, 10-Oct-2026).

## Reconciliación .md 04-02 (2026-10-09, solo docs)
- `README.md` (lockout persistente `SegBloqueo` 15min; 2FA-temp 120s como deuda), `01-04/spec.md` (+`PasswordHasher:MemoryKBytes/Iterations`, aditivo sin cambio de estado, precedente `04-01`), skills `04-security-core`/`03-auth-endpoints`/`deferred-scope-fakes` (realidad `04-02` + swap como precedente). Specs firmados de fase 03 y `05-01` intactos (siguen factuales: `attempts:/lockout:` viven en 2FA).
- Verificación: `AppSettingsTests` 3/3 + `critic PASS`.

## Cierre PR #21 phase04.2 mergeado a main (2026-10-09)
- Merge `418eefc` (PR #21 `phase04.2` → `main`, mergeado vía web): T1+T2 `04-02` (`ccfe5bb`: Argon2id + `NeedsRehash` + lockout `SegBloqueo` 15min + tests + Stryker 90.85%) + conciliación `.md` (`b01b211`: cifras finales + bullet Argon2id).
- `main` local sincronizado en `418eefc` (fast-forward, working tree limpio).
- Nota: `gh` local tenía token expirado (401) → PR creado/mergeado vía web; re-autenticado con `gh auth login` tras el merge. Spec `04-02` → ✅ Aprobado (@usuario, 10-Oct-2026).

## T1 — 04-03 HTTP Hardening ejecutado (2026-10-10)
- Middleware único `SecurityHeadersMiddleware.cs` (`NonceItemKey="CspNonce"`, 4 headers siempre + CSP con nonce 16B Base64, exención `scalar`/`openapi` early-exit sin nonce); inserción outermost absoluta (antes de `DeveloperExceptionPage`, cubre 403/500; orden `01-02` aditivo); HSTS 365d solo `MaxAge` vía `AddHsts` (`UseHsts()` sin overload con options en esta versión; gate no-Dev intacto, sin `IncludeSubDomains`/preload).
- Sin `OnStarting` a propósito: `DefaultHttpContext` no lo dispara (solo servidor real) y el set síncrono outer ya sobrevive a `WriteErrorAsync`; evitó mutantes supervivientes. HSTS en wire no testeable con `WebApplicationFactory` (TestServer siempre http; `X-Forwarded-Proto` no aplicado por `RemoteIpAddress` ante `ForwardedHeadersOptions` inline) → assert `HstsOptions` en factory `Production`.
- Tests: `SecurityHeadersTests` 10 (2 nulos, headers+CSP+nonce 16B, unicidad, `Theory` 5 exentas, set-antes-de-`_next`) + `HeaderTests` 3 (`/health` con headers, 403 sonda `Staging` con headers, `HstsOptions` 365d); 403 ownership intacto (`VentaDetalleService.cs:203` + sonda, sin tocar `ExceptionHandlingMiddleware`).
- Verificación: build 0/0 + Unit 326/326 (316+10) + Security 77/77 (74+3) + Integration 92/99 (7 Docker-only: Docker Desktop detenido, `com.docker.service` no arranca en sesión, igual que `04-02`, 0 regresiones) + Contract 4/4 + `critic PASS` + `endpoints OK (56)`; Stryker local `stryker-0403.json` (solo slice, `ignore-mutations Boolean`, break 80) → **100.00%** (18 mutantes) + `dotnet build` restaurativo + re-test filtro verde.
- Lecciones: `UseHsts(Action<HstsOptions>)` no existe → configurar por `AddHsts`; S3358 prohíbe ternaria anidada en helpers de test (extraer `GetHeader`); `HstsOptions` vive en `Microsoft.AspNetCore.HttpsPolicy` (no `Builder`); `Start-Service com.docker.service` falla sin permisos → baseline Docker-only documentado como en `04-02`.
- Estado spec: ✅ Aprobado (@usuario, 10-Oct-2026).

## Reconciliación .md 04-03 (2026-10-10, solo docs)
- `README.md` (orden de middleware: `SecurityHeadersMiddleware` outermost + HSTS 365d vía `AddHsts`); skill `04-security-core` (+realidad `04-03`: middleware único outermost sin `OnStarting`, HSTS por opciones en factory `Production`).
- Intactos con motivo: `01-02/spec.md` (✅ Aprobado — specs firmados no se tocan, precedente `04-02`; el slot 🚧 Fase 04 lo cubre el delta `04-03`); `01-04/spec.md` (`AddHsts` es solo código, sin claves nuevas en `appsettings`); `04-05/spec.md` (ya declara HSTS 365d + CSP nonce como Cubierto); `docs/endpoints.md` (sin rutas nuevas); `AGENTS.md` §6 (resumen vigente); `.specify/memory/constitution.md` (orden aspiracional con `CspNonce` separada — artefacto del flujo spec-kit, no usado en esta rama; reconciliar aparte si se adopta).
- Verificación: `critic PASS`.

## Cierre PR #23 phase04.3 mergeado a main (2026-10-10)
- Merge `4d7c08f` (PR #23 `phase04.3` → `main`, mergeado vía web): T1—T4 `04-03` (`8b620f6`: `SecurityHeadersMiddleware` + HSTS 365d + tests + Stryker 100%) + reconciliación `.md` (`README`, skill `04-security-core`, `Memoria`).
- `main` local sincronizado en `4d7c08f` (fast-forward, working tree limpio).
- Spec `04-03` → ✅ Aprobado (@usuario, 10-Oct-2026).

## T1 — 04-04 Rate Limit & Auth Matrix ejecutado (2026-10-10)
- Alcance ampliado cerrado con usuario (task pedía solo doc): doc + código + tests; SlidingWindow por IP + 429 JSON uniforme; relajación vía `PERF_RATELIMIT_MULTIPLIER`.
- Código: `Services/RateLimitOptions.cs` (defaults spec + `ApplyMultiplier` con clamp + `*PolicyName` consts para atributos); `Program.cs` (`AddRateLimiter` 5 policies `QueueLimit=0` + `OnRejected` 429 `ErrorResponse` sin Detail + `Retry-After` best-effort; `UseRateLimiter` antes de `UseAuthentication`); `[EnableRateLimiting]` en 16 controllers (9×`Admin` a nivel clase, auth anónimas/Bearer con `Login`/`Login2faVerify`/`Global`, `Venta`/`VentaDetalle` clase `Global` + override `ConcurrentWrites` en POST/DELETE); sección `RateLimiting` en `appsettings.Example.json` (env `PERF_*` fuera del JSON por `AppSettingsTests`).
- Doc: `docs/rate-limit-matrix.md` nueva (49 filas + exclusiones ping/health/probe/provider-states + evidencia); `endpoints.md` enlaza (columna ahora = policy vigente); spec/plan/task `04-04` conciliados (corregido `AdminOnly` → `AdminPolicy`); `01-04/spec.md` +7 filas (`RateLimiting:*`, `PERF_RATELIMIT_MULTIPLIER`).
- Tests: `SecurityTest/RateLimit` (4: 429 en Login/Global/Admin + estructural reflection toda-action-con-policy salvo `PingController`) + `UnitTest/RateLimit` (4: defaults/multiplier/clamp/nombres) + asserts `RateLimiting` en `AppSettingsTests`; colaterales con límite elevado vía config (`IntegrationTest/Login.FiveFailuresThenLockedOut`, `SecurityTest/Login.RepeatedUnknownFailuresLockOut`, aserciones intactas).
- Bugs cazados por tests nuevos: (1) lectura eager de `builder.Configuration` en registro no ve overrides de `WebApplicationFactory` (el 429 no disparaba con límites de test; el lockout-test con límite 100 devolvía 429 = prueba) → `IOptionsMonitor` lazy en el particionador (tercera instancia de la lección Redis 05-01/DbContext 02-02); (2) metadata `RetryAfter` ausente en rechazo SlidingWindow → header best-effort, sin assert.
- Verificación: build 0/0; Unit 330/330; Security 81/81; Integration 99/99 (92+7 tras arrancar Docker Desktop detenido, como en 03-02/03-11); Contract 4/4; Database 2/2; `critic PASS`; `endpoints OK (56)`.
- Lecciones: CA2000 en `new WebApplicationFactory<Program>().WithWebHostBuilder(...)` inline → pragma como en `CreateAdminFactory`; `EnableRateLimiting` de action prevalece sobre el de clase (verificado empíricamente por los 429).
- Estado spec: ✅ Aprobado (@usuario, 10-Oct-2026).

## T1 — 04-05 OWASP ASVS L2 ejecutado (2026-10-10)
- Doc-only sin código: `docs/asvs-l2-checklist.md` nueva (10 capítulos V1–V9+V14, 6×Cubierto + 4×Parcial con deuda explícita de 04-01/04-02/04-03; cada fila con evidencia archivo+test resoluble; exclusiones ping/health/probe/L3).
- Correcciones al spec: `Contratos` ya no duplica la tabla (enlaza al doc canónico, `drift-guards`); `AdminOnly+AdminPolicy` → `AdminPolicy` real; `spec/task` firmados a ✅ Aprobado (@usuario, 10-Oct-2026, sin cambios sobre la evidencia).
- Parciales honestos: V2 (`NeedsRehash` sin escritura, temp-2FA 120s), V3 (blacklist TTL 120s), V7 (`audit hash chain` fase 08), V9 (HSTS wire no verificable en `WebApplicationFactory`).
- Verificación: build 0/0 + `critic PASS` + `endpoints OK (56)`; suites heredadas de 04-04 (Unit 330/330, Security 81/81, Integration 99/99, Contract 4/4, Database 2/2) sin regresión posible (sin código nuevo).
- Estado spec: ✅ Aprobado (@usuario, 10-Oct-2026).

## Cierre PR #25 phase04.5 mergeado a main (2026-10-10)
- Merge `7be3cee` (PR #25 `phase04.5` → `main`): 04-05 OWASP ASVS L2 (`1c84329`: `docs/asvs-l2-checklist.md` 10 capítulos + firma spec/task).
- `main` local sincronizado en `7be3cee` (fast-forward, working tree limpio) + build 0/0 + UnitTest 330/330 en `main`.
- Spec `04-05` publicada en `main`; fase 04 cerrada por completo en `main` (04-01…04-05 ✅).

## Skills sincronizadas con fase 04 (2026-10-10, rama `phase06.preview`, solo `.md`)
- 26 SKILL.md + 3 agentes, 0 nuevas (todo encajaba en existentes): P0 `auth-matrix` (rate 04-04 vigente + literal `role`/flag/`OnTokenValidated` + ASVS canónico), `03-auth-endpoints` (delta JWT + checklist duplicada eliminada), `03-errors-middleware` (SecurityHeaders outermost), `02-domain-data`/`02-migrations-seed` (`SegBloqueo`, `SegBloqueo0402` sin `_`, seed fail-closed), `01-foundation` (orden post-04 + claves `UseJwtBearer`/`PasswordHasher:`/`RateLimiting`), `drift-guards` (2º/3er canónico `rate-limit-matrix` 49 + `asvs-l2-checklist` 10 caps); P1 `04-jwt-refresh` (delta 04-01 + CA1865/CA1845/CS8601), `04-security-core` (taller 04-02/04-03 + Stryker 90.85%/100%), `testing-mutation` (`test-case-filter` + `AsSpan(2)`/`catch when`), `testing-webappfactory` (flag JWT + `IOptionsMonitor` lazy + HSTS assert), `testing-strategy` (conteos 330/99/81/4/2), `analyzer-quickref` (CA1865/CA1845/CS8601/S101/CA1707/S1135-minúsculas/S3358/CA2000-04-04) + tope en `07-static-analysis`, `09-ci-matrix`/`09-cicd-ops` (baseline + `PERF_*` + canónicos), `05-cache-redis` (login→`SegBloqueo`, puente lazy); P2 `traceability` (matrices vivas), `spec-first-writing` (no-duplicar extendido), `vertical-slice`/`03-api-vertical-slice`/`03-api-base`/`03-saga-endpoints`/`empirical-verification`/`deferred-scope-fakes`/`critic-guardrails`.
- Agentes `last-synced → 2026-10-10`: `slice-scaffolder` (`EnableRateLimiting` + orden headers), `security-reviewer` (checks 04 FAIL vigente), `traceability-clerk` (paths `rate-limit-matrix`/`asvs-l2-checklist`).
- Verificación: solo `.md` (sin código); `critic PASS (30 files)` + `endpoints OK (56)`; sin commit por decisión del usuario (rama actual).

## Sub-agentes mejorados para fase 06 (2026-10-10, rama `phase06.preview`, solo `.md`)
- Veredicto: sí requerían actualización — los 3 estaban anclados a fase 03/04 e incapaces de generar/verificar el bus real (`06-01…06-04` en Borrador, `docs/saga-state-machine.md` inexistente, 1/7 eventos con mismatch `Guid vs int`, `Consumers/` vacío, 8 `NOTE 06-*` pendientes desde `03-12…03-15`).
- `slice-scaffolder`: +3ª entrada `06-0X`, +enlaces `06-events-saga`/`06-saga-state-machine`, +puntos 9-12 (`Events` 7 POCO inmutables `06-03`, `Consumers IConsumer<T>` StockValidator/Pago/Factura/Compensation idempotente+retry+DLQ 3, snippet MassTransit `Transport=InMemory/SQS` + paquetes, `docs/saga-state-machine.md` sin huérfanos con canónico `Pendiente vs Creado/Registrado`), +variante saga/consumer (`grep NOTE 06-*`, `stryker-06XX`, `IntegrationTest/Saga/` + `ChaosTest/`), swap ampliado a folio `F-{año}-{seq}`+`FacturaConsumer`, Done saga.
- `security-reviewer`: duplicado `auth-matrix` corregido a `docs/rate-limit-matrix.md` + fuentes 06; +checks 10-14 WARN→FAIL (secretos/PII en `Events/` + SQS hardcodeado/transporte pinned + idempotencia/retry/DLQ + compensación 2 niveles + bus sin auth/logs/catch-traga/schemas sin versión/estados huérfanos); aclara que `critic-guardrails.ps1` cubre solo 1-4 (5-14 manuales).
- `traceability-clerk`: +fuentes `saga-state-machine.md` + 4 specs 06; +reportes 4-6 (estados/diagrama, schemas vs `Events/` + transiciones vs consumers, `grep NOTE 06-*` + anti-cobertura-ficticia extendida a 06).
- `README.md` de agentes: roles con fase 06 + skills `06-*` enlazadas.
- Verificación: solo `.md`; `critic PASS (31 files)` + `endpoints OK (56)`; sin commit (rama actual).

## AGENTS.md sincronizado con fase 04 + sub-agentes 06 (2026-10-10, rama `phase06.preview`, solo `.md`)
- §5: banner Fase 04 ✅ + lockout login migrado a `SegBloqueo` (solo 2FA/blacklist en Redis).
- §6: eliminado banner falso `[FASE 04 - PENDIENTE]`; reescrito como implementado (JWT flag + `OnTokenValidated`, Argon2id + `SegBloqueo`, `SecurityHeadersMiddleware` outermost + `AddHsts`, rate-limit 5 policies + 429, matrices vivas).
- §11: variante saga/consumer + checks 06 + clerk 06 + critic cubre 1-4; §3 +3 límites (`IOptionsMonitor` lazy, `S1135` minúsculas/CA1707, canónico `06-04`); structure `docs/` con 3 canónicos.
- Verificación: `critic PASS (32 files)` + `endpoints OK (56)`; sin commit (rama actual).

## Constitution global sincronizada (2026-10-10, rama `phase06.preview`, solo `.md`)
- `.specify/memory/constitution.md` (artefacto spec-kit, 8 falsedades): §2.1 lockout login→`SegBloqueo` (solo 2FA/blacklist en Redis); §4 JSON legacy medida (no PascalCase puro) + orden medido (`SecurityHeadersMiddleware` outermost, sin `CspNonce` separada, `UseRateLimiter` antes de `Auth`) + TTL 0–120s; §6 `ExpectedSha256`; §7 gate Stryker ≥80% + filtro UnitTest; §8 orden CI con `critic→endpoints→contract→semgrep`; §10 `agent.md` muerto→`Memoria.md` + `00-04` canónico.
- No tocados (requieren revisor): `00-01`/`00-04` ✅ firmados (promoción de evidencias `Por Validar` → 04-05 queda como follow-up con revisor).
- Verificación: `critic PASS (33 files)` + `endpoints OK (56)`; sin commit (rama actual).

## README.md reconciliado con fase 04 (2026-10-10, rama `phase06.preview`, solo `.md`)
- Lede + status: fase 04 ✅ (JWT real, Argon2id, headers, rate-limit, ASVS) en vez de "pendiente"; stack +4 paquetes (`JwtBearer`, `Argon2`, `BCrypt`, `Otp.NET`) + configs `stryker-0402/0403`; config +4 claves (`UseJwtBearer`, `PasswordHasher:`, `RateLimiting:`, `PERF_*`); refresh/logout JWT + `OnTokenValidated`; middleware +`UseRateLimiter`; medido 10-Oct **330/99/81/2/4** + 3 canónicos + Stryker 04-02/04-03; +5 reglas (`test-case-filter`, literal `role`, `IOptionsMonitor` lazy, `UseHsts`/`HSTS-WAF`, action>clase); decisiones JWT/rate/headers reescritas como vigentes; componentes + sub-agentes fase 06; roadmap sin `PR #18` ni fase 04 pendiente.
- Verificación: `critic PASS (34 files)` + `endpoints OK (56)`; sin commit (rama actual).

## T1 — 06-01 Transporte y eventos ejecutado (2026-10-10, rama `phase06.preview`)
- Paquetes: `MassTransit` + `MassTransit.AmazonSQS` **8.5.11** (Apache-2.0 + net10.0; 9.2.3 descartado por licencia comercial Massient — regla 00-01 excepción); `nuget.config` +`MassTransit*`/`AWSSDK.*`; restore sin NU1100.
- Bus en `Program.cs`: `AddConsumers` + if/else `UsingAmazonSqs` (colas `saga-*.fifo`, región fail-fast, retry 5×) / `UsingInMemory` (retry 5× 500ms→10s, `ConfigureEndpoints`); `Transport`/`Sqs:*` en `Example.json` + fila `01-04` a Implementado; credenciales SQS solo IAM.
- Contratos: 7 `record` inmutables `SchemaVersion = 1` (desviación `Guid` vs `int` 06-03); `SagaStates` con 8 estados; `IEventBus` + publishers MassTransit (fakes intactos para UnitTest).
- Consumers: StockValidator reserva al validar; Pago valida cobro; Factura folio determinista `F-{año}-{pedido:N}`; Compensation anula + restaura solo con reserva; `VenEventoProcesado` UNIQUE + migración `EventosProcesados0601` (factory temporal borrada).
- `VentasPedidoService` responde antes de publicar (POST determinista); `VentasPagoService` publica `PagoProcesadoEvent` (ctor +1, tests actualizados).
- `docs/saga-state-machine.md` mínimo (diagrama + matriz transición→consumer + reglas); tríada 06-01 a Borrador con evidencia.
- Tests: `UnitTest` 386/386 (+56: Events 4, Consumers 40, Transport 3, pago 5, AppSettings 4), `IntegrationTest` 101/101 (+2 Transport: feliz hasta Facturado, rechazo hasta Cancelado), Security 81/81, Contract 4/4, Database 2/2; `critic PASS` + `endpoints OK (56)`.
- Lecciones: MassTransit una sola fábrica (segundo `SetBusFactory` lanza); marca de idempotencia CON la mutación (marcar-antes rompe el reintento: redelivery ve marca y salta); `folio` mandado no secreto (fix check 10 reviewer); cobro fuera de orden → `PagoRechazadoEvent`; competencia Pago/Factura exige retry amplio (5× 500ms→10s); llaves cache como `const` (B2b); `S125` caza comentarios con pinta de código; `CA1861` en migración → pragma; `CA1002` en fakes públicos compartidos; `CA1859` prefiere `List<>` en dobles; `IPublishEndpoint` tiene 8 overloads (stub completo); `ignore-methods: [Consume]` para adaptadores finos (cubiertos en Integration).
- Estado spec: ✅ Aprobado (@usuario, 10-Oct-2026, firma sin cambios sobre la evidencia T1: Stryker **90.77%**, residuo clasificado; SQS vivo + caos → fase 09; compensación total → 06-02).
- Follow-up: `02-03` schema sigue en 13 tablas (faltan `SegBloqueo` 04-02 y `VenEventoProcesado` 06-01;spec ✅, requiere revisor para conciliar).

## Cierre commit 06-01 en `phase06.preview` (2026-10-10, local sin push)
- Commit `98d4c5a`: T1 06-01 + firma (@usuario) + skills 04/06 + AGENTS/constitution/README sincronizados (84 ficheros, working tree limpio, sin push a GitHub por decisión del usuario).
