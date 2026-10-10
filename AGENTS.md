# Agents.md — Guía operativa para agentes y desarrolladores

## 1. Forma de trabajo

> **Principios y stack obligatorio**: ver `specs/phase-00-constitution/00-01-principles-stack/spec.md`. No introducir tecnologías fuera del stack sin la regla de excepción allí definida.

- Setup: copiar `appsettings.Example.json` → `appsettings.json`; opcional
  `UseInMemoryDatabase: true`; `dotnet restore`; `docker compose -f deploy/docker-compose.local.yml up -d`.
- Comandos: `dotnet restore`, `dotnet build -c Release --no-restore`,
  `dotnet test <csproj> -c Release --no-build` (Unit, Integration, Security,
  Contract), `dotnet run --project WebAPIDevSecOpsScallingSDD/WebAPIDevSecOpsScallingSDD.csproj`.
- Orden CI real (`ci-pr.yml`): restore → build → unit → integration → security → critic → endpoints → contract → semgrep. Tests con `--no-build`. (`database`/mutation/perf/chaos: solo nightly/fase futura.)
- Ramas/PRs: un PR = una feature/fix; checklist `CHECKLIST_PR.md` obligatorio;
  reviewer verifica sin regresiones de cobertura.
- Naming: código en inglés; tablas/columnas mantienen `Ven*`, `Cli*`, `Emp*`,
  `Pro*`, `Seg*` con prefijos `str/int/dec/dte/bln`. JSON: convención legacy
  medida (prefijos minúsculos + resto PascalCase + `id`/sufijo; ver
  `IsConventional` en `ContractTest`), NO PascalCase puro.
- Sin secretos, sin TODOs sin issue, sin usings no usados, sin código muerto.
- Al terminar, resume qué has cambiado y cualquier decisión que deba revisar

## 2. Memoria

> **Gobernanza SDD**: plantilla obligatoria en `specs/_template.md` y DoD en
> `specs/phase-00-constitution/00-02-sdd-governance/spec.md`. Toda spec nueva
> sigue la plantilla; criterios siempre verificables por comando o test.

- `AGENTS.md`: manual operativo vivo.
- `Memoria.md`: estado, decisiones, lecciones aprendidas por fase y metodología
  de diagnóstico de chaos. Actualizar al cerrar cada fase.
- Al empezar, lee `Memoria.md` para conocer el estado del proyecto y las decisiones tomadas.
- Al terminar una tarea, actualízalo: estado actual, decisiones importantes (con su porqué) y errores a evitar.
- Al cerrar una fase, concilia su `spec.md`/`plan.md` con lo implementado (rutas de tests, criterios, nombres reales de archivos) y resuelve el bloque de aprobación: Borrador con evidencia, o firmado solo si el usuario indicó revisor.
- Mantenlo breve (máximo ~10000 líneas): resume o elimina lo que ya no aporte.
- Si algo se convierte en una regla permanente, propón moverlo a AGENTS.md en lugar de dejarlo en la memoria.
- No guardes nunca datos sensibles (claves, tokens, datos personales).

## 3. Límites

- No asumir que InMemory genera `[Timestamp]`; usar `byte[]{1}` o subclass.
- No depender de `TokenBlacklist` estático entre tests sin `Reset()`.
- No usar coverage de línea como gate; el gate real es mutation score.
- Nunca hardcodear connection strings, keys ni credenciales.
- No confiar en `Invoke-WebRequest` sin `-UseBasicParsing` en PowerShell 5.1.
- Las imágenes `aspnet:10.0` no traen wget/curl; no sondear desde dentro sin verificar.
- No matar procesos `dotnet` MSBuild persistentes salvo limpieza real.
- No reemplazar Argon2id por hash débil; fallback BCrypt solo para migración.
- No tocar `Program.cs` pipeline sin respetar el orden de middleware.
- Rate limits relajados solo vía env `PERF_*` o config explícita; no en prod.
- No asumir PascalCase puro en JSON (convención legacy medida, §1).
- No duplicar tablas vivas en specs (el canónico vive en `docs/`, el spec enlaza).
- No leer config eager en el registro DI si hay overrides WAF en tests (usar `IOptionsMonitor` lazy; 3ª instancia Redis/DbContext/RateLimit).
- `S1135` caza `todo` incluso en minúsculas; migraciones sin `_` (CA1707); estados saga solo con canónico `06-04`.
- Siempre: actualizar Memoria.md al terminar cada tarea.

## 4. Verificación

- Build Release 0 errores antes de test.
- Unit, Integration, Security, Contract con `--no-build`; 100% verdes local.
- `powershell -File scripts/critic-guardrails.ps1` → `PASS exit 0`.
- `powershell -File scripts/check_endpoints.ps1` → `OK exit 0` (toda ruta con fila en `docs/endpoints.md`).
- `python scripts/check_coverage.py` (umbral real 45%).
- `dotnet stryker` para mutation (nightly, timeout 180 min).
- Tras Stryker, siempre `dotnet build` antes de cualquier test `--no-build` (Stryker deja binarios mutantes en `bin/`).
- Chaos nightly: `run-chaos.ps1`, exit 0 PASS / 1 FAIL / 2 suite error.
- Contract: fixtures por captura real en `ContractTest/Fixtures` (WAF + InMemory; regenerar con `$env:CONTRACT_CAPTURE="1"`); Pact contra proceso real queda para fase 10.
- Validar YAML CI localmente (`python -c "import yaml; yaml.safe_load(...)"`).
- Verificar nombres reales de métricas con `curl /metrics` antes de dashboards.

### 4.2 Verificación de Requerimientos No Funcionales (NFR)

Toda entrega de código debe validarse contra los umbrales de calidad,
rendimiento y resiliencia definidos en la arquitectura.

- Tabla completa y comandos: `specs/phase-00-constitution/00-03-nfr/spec.md`.
- Guardarraíl: si modificas o introduces un componente que impacte un umbral
  objetivo (p. ej. latencia o mutation score), ejecuta su comando de
  verificación antes de solicitar aprobación del cambio.

### 4.3 Referencia de configuración

La tabla canónica de claves (`appsettings.*.json`, env vars) y su estado por
fase vive en `specs/phase-01-foundation/01-04-config-reference/spec.md`.
`appsettings.Example.json` solo documenta claves con código real; secretos
fuera de Git (`.gitignore` cubre `appsettings.Local.json`).

## 5. Convenciones técnicas

> Fase 03 terminada y firmada 19/19 (09-Oct-2026). Fase 04 terminada y firmada
> 04-01…04-05 (10-Oct-2026). Fase 06 en curso: 06-01 firmada (transporte MassTransit
> + 7 eventos + 4 consumers, 10-Oct-2026); 06-02…06-04 pendientes.
> Fuente viva de rutas: `docs/endpoints.md`
> (verificado por `scripts/check_endpoints.ps1`); rate-limit: `docs/rate-limit-matrix.md`
> (49 filas); ASVS L2: `docs/asvs-l2-checklist.md` (10 capítulos, 6×Cubierto + 4×Parcial).

- Rutas `api/v{version}/[controller]`; DTOs + FluentValidation; nombres según convención legacy (§1).
- Health `/health`, `/health/ready`, `/health-ui`; `/metrics`; `/scalar` solo Dev.
- Redis keys: `blacklist:{jti}`, `attempts:{user}`, `lockout:{user}` (solo 2FA/TOTP
  + blacklist; el lockout de login migró a tabla persistente `SegBloqueo` 5→15min en 04-02),
  `cache:{entidad}:...` con TTL; password nunca en cache.
- Eventos saga: `PedidoCreadoEvent`, `StockValidadoEvent`, `StockRechazadoEvent`,
  `PagoProcesadoEvent`, `PagoRechazadoEvent`, `FacturaGeneradoEvent`,
  `FacturaRechazadaEvent`. Consumers con MassTransit; SQS FIFO + DLQ.
- Migrations: `dotnet ef migrations add/update/remove --project WebAPIDevSecOpsScallingSDD`.

## 6. Seguridad y DevSecOps

> Fase 04 implementada y firmada 04-01…04-05 (10-Oct-2026).

- JWT HS256 real tras flag `Authentication:UseJwtBearer` (key ≥32B, ClockSkew=Zero,
  ValidAlgorithms; `OnTokenValidated` anti-`blacklist:{jti}`; refresh tokens hasheados con rotación).
- Argon2id 64MB/3 iter + `NeedsRehash` (BCrypt solo migración); lockout persistente `SegBloqueo`
  5→15min (`TimeProvider`, fail-closed); 2FA TOTP real (Otp.NET ±1 + DataProtection);
  anti-enumeration; CORS single origin.
- `SecurityHeadersMiddleware` outermost (4 headers + CSP nonce 16B; exención `scalar`/`openapi`) + HSTS 365d
  vía `AddHsts` solo no-Dev.
- Rate-limit vigente (5 policies SlidingWindow + 429 `ErrorResponse` uniforme; `UseRateLimiter`
  antes de `Auth`; relajación solo vía `PERF_RATELIMIT_MULTIPLIER`); matriz viva
  `docs/rate-limit-matrix.md`; ASVS L2 `docs/asvs-l2-checklist.md`.
- Assembly integrity check; audit hash chain; request timeout 60s.
- SAST: Semgrep (`semgrep scan --config=auto --config=.semgrep/semgrep.yaml --error`; `scan` rechaza `--metrics=off`).
- Contenedores: Trivy + dockle (HIGH/CRITICAL falla); ZAP en PR y main.
- SonarCloud Quality Gate informativo; new code coverage ≥80%.

## 7. Testing quirks

> 🚧 [FASE 10 - PENDIENTE] Esta funcionalidad esta especificada pero aun no implementada en el repositorio real.

- xUnit + FluentAssertions + Moq + FsCheck; WebApplicationFactory para
  Integration/Security.
- `ContractTest`: captura con `CONTRACT_CAPTURE=1` (sin la variable valida sin
  escribir); `xunit.runner.json` en serie; convención medida en
  `IsConventional` (NO PascalCase puro). Tabla regla→fix:
  `.opencode/skills/testing/analyzer-quickref/SKILL.md`.
- SecurityTest referencia helpers de `UnitTest/Common/` (`TokenHelper`,
  `TestDataFactory`).
- `IntegrationTest` usa `Testcontainers.MsSql` con puerto fijo para restart.
- NBomber borra su carpeta de reportes al arrancar: separar `reports/` (caos)
  y `perf-reports/`.
- Pact: pactSpecification 3.0.0 con reglas planas `{"match":"type"}`.
- Stryker: globs `mutate` relativos al proyecto mutado; safe mode puede ocultar
  métodos sin test (asignar en todas las ramas).

## 8. Observabilidad

> 🚧 [FASE 08 - PENDIENTE] Esta funcionalidad esta especificada pero aun no implementada en el repositorio real.

- Serilog JSON rolling + consola; filtros para no loggear secretos.
- OTel metrics/tracing; meter `WebAPIDevSecOpsScallingSDD.QualityMetrics` resuelto eager.
- Prometheus `/metrics` + Grafana dashboard; nombres con sufijos del exporter.
- Chaos/recovery validados con logs como prueba de vida y grupos control.

## 9. Despliegue y operación

> 🚧 [FASE 09 - PENDIENTE] Esta funcionalidad esta especificada pero aun no implementada en el repositorio real.

- AWS Free Tier temporal: CloudFormation, ALB 80/443, EC2, RDS, SQS, DLQ.
- Destruir/recrear para costos; cron schedule-deploy/destroy.
- Redis: multiplexer afinado (`AbortOnConnectFail=false`, timeouts cortos,
  retry exponencial) → fallback ≤500 ms; `/health` refleja caída de Redis.
- Docker images sin shell/curl garantizado; verificar herramientas antes de sondear.

## 10. Reglas Operativas Basadas en Lecciones Aprendidas

Las 13 reglas constitucionales derivadas de fallos históricos y optimizaciones del proyecto están centralizadas de forma canónica en la especificación de gobernanza. Ningún agente debe contradecirlas o ignorarlas.

- **Fuente canónica obligatoria**: `specs/phase-00-constitution/00-04-lessons-learned/spec.md`.

## 11. Sub-agentes (híbrido agentes → skills)

- `slice-scaffolder`: Fase A (esqueleto compilable vertical-slice intra-PR,
  con test que aserta cada diferido) + variante swap fake→real (fase 04)
  + variante saga/consumer (fase 06: eventos, consumers, MassTransit, diagrama);
  nunca se commitea sin su Fase B.
- `security-reviewer`: gate pre-push (critica sin editar, sin Stryker;
  + checks fase 04: JWT, hash, rate-limit, headers, secretos en logs
  + checks fase 06 WARN→FAIL: secretos en eventos, SQS, idempotencia/retry/DLQ,
  compensación, auth del bus, schemas, estados).
- `traceability-clerk`: matrices vivas 04-04/04-05 + saga 06-01…06-04
  (diagrama, schemas, transiciones, NOTEs) + addenda (reporta drift,
  no edita).
- Invocación manual en dev (`@slice-scaffolder`, `@security-reviewer`,
  `@traceability-clerk`); en CI solo corre el job `critic` ligero
  (`scripts/critic-guardrails.ps1`, cubre solo bloqueantes 1-4; 5-14 manuales).
- Fuente de reglas: `.opencode/skills/` (48 con `drift-guards`; los agentes
  solo enlazan SKILL.md, nunca copian). Identidad y convención:
  `.opencode/agents/README.md`.


---

## Solution structure

`WebAPIDevSecOpsScallingSDD.slnx` uses the new `.slnx` XML format (not `.sln`).

| Project | Path | Type |
|---|---|---|
| `WebAPIDevSecOpsScallingSDD` | `WebAPIDevSecOpsScallingSDD/` | Web API (entrypoint: `Program.cs`) |
| `UnitTest` | `UnitTest/` | xUnit unit tests |
| `IntegrationTest` | `IntegrationTest/` | xUnit + `WebApplicationFactory` + Testcontainers |
| `SecurityTest` | `SecurityTest/` | xUnit + `WebApplicationFactory` |
| `DatabaseTest` | `DatabaseTest/` | Testcontainers for migrations/seed |
| `ContractTest` | `ContractTest/` | xUnit: fixtures por captura real + convención de nombres (Pact en fase 10) |
| `MutationTest` | `MutationTest/` | Stryker mutation tests |
| `PerformanceTest` | `PerformanceTest/` | NBomber scenarios |
| `ChaosTest` | `ChaosTest/` | Chaos experiments + nightly runner |
| `fuzzing/` | `fuzzing/` | RESTler DAST config |
| `deploy/` | `deploy/` | docker-compose, CloudFormation, Grafana |
| `scripts/` | `scripts/` | Coverage check, quality metrics, drift guards, deploy/destroy |
| `docs/` | `docs/` | Catálogo canónico de endpoints (`endpoints.md`, 56 filas) + rate-limit (`rate-limit-matrix.md`, 49 filas) + ASVS L2 (`asvs-l2-checklist.md`, 10 capítulos) |
| `.opencode/` | `.opencode/` | Sub-agentes (`agents/`) + skills (`skills/`, 48 con `drift-guards`) |
| `.github/` | `.github/` | CI/CD workflows, Dependabot, PR template |
| `.semgrep/` | `.semgrep/` | Custom SAST rules |
