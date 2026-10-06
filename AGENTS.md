# Agents.md — Guía operativa para agentes y desarrolladores

## 1. Forma de trabajo

> **Principios y stack obligatorio**: ver `specs/phase-00-constitution/00-01-principles-stack/spec.md`. No introducir tecnologías fuera del stack sin la regla de excepción allí definida.

- Setup: copiar `appsettings.Example.json` → `appsettings.json`; opcional
  `UseInMemoryDatabase: true`; `dotnet restore`; `docker compose -f deploy/docker-compose.local.yml up -d`.
- Comandos: `dotnet restore`, `dotnet build -c Release --no-restore`,
  `dotnet test <csproj> -c Release --no-build` (Unit, Integration, Security),
  `dotnet run --project WebAPIDevSecOpsScallingSDD/WebAPIDevSecOpsScallingSDD.csproj`.
- Orden CI: restore → build → unit → integration → security → database → contract → mutation (nightly) → performance (nightly) → chaos (nightly). Tests con `--no-build`.
- Ramas/PRs: un PR = una feature/fix; checklist `CHECKLIST_PR.md` obligatorio;
  reviewer verifica sin regresiones de cobertura.
- Naming: código en inglés; tablas/columnas mantienen `Ven*`, `Cli*`, `Emp*`,
  `Pro*`, `Seg*` con prefijos `str/int/dec/dte/bln`.
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
- Siempre: actualizar Memoria.md al terminar cada tarea.

## 4. Verificación

- Build Release 0 errores antes de test.
- Unit, Integration, Security con `--no-build`; 100% verdes local.
- `python scripts/check_coverage.py` (umbral real 45%).
- `dotnet stryker` para mutation (nightly, timeout 180 min).
- Tras Stryker, siempre `dotnet build` antes de cualquier test `--no-build` (Stryker deja binarios mutantes en `bin/`).
- Chaos nightly: `run-chaos.ps1`, exit 0 PASS / 1 FAIL / 2 suite error.
- Contract/Pact contra proceso real en puerto libre + `/health` + kill en `finally`.
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

> 🚧 [FASE 03 - PENDIENTE] Esta funcionalidad esta especificada pero aun no implementada en el repositorio real.

- Rutas `api/v{version}/[controller]`; JSON PascalCase; DTOs + FluentValidation.
- Health `/health`, `/health/ready`, `/health-ui`; `/metrics`; `/scalar` solo Dev.
- Redis keys: `blacklist:{jti}`, `attempts:{user}`, `lockout:{user}`,
  `cache:{entidad}:...` con TTL; password nunca en cache.
- Eventos saga: `PedidoCreadoEvent`, `StockValidadoEvent`, `StockRechazadoEvent`,
  `PagoProcesadoEvent`, `PagoRechazadoEvent`, `FacturaGeneradoEvent`,
  `FacturaRechazadaEvent`. Consumers con MassTransit; SQS FIFO + DLQ.
- Migrations: `dotnet ef migrations add/update/remove --project WebAPIDevSecOpsScallingSDD`.

## 6. Seguridad y DevSecOps

> 🚧 [FASE 04 - PENDIENTE] Esta funcionalidad esta especificada pero aun no implementada en el repositorio real.

- JWT HS256, key ≥32B, ClockSkew=Zero, ValidAlgorithms; refresh tokens hasheados.
- Argon2id 64MB/3 iter; lockout; 2FA TOTP; anti-enumeration; CORS single origin.
- Headers de seguridad + CSP nonce; HSTS solo no-Dev.
- Assembly integrity check; audit hash chain; request timeout 60s.
- SAST: Semgrep (`semgrep ci --config=auto --config=.semgrep/semgrep.yaml --error --metrics=off`).
- Contenedores: Trivy + dockle (HIGH/CRITICAL falla); ZAP en PR y main.
- SonarCloud Quality Gate informativo; new code coverage ≥80%.

## 7. Testing quirks

> 🚧 [FASE 10 - PENDIENTE] Esta funcionalidad esta especificada pero aun no implementada en el repositorio real.

- xUnit + FluentAssertions + Moq + FsCheck; WebApplicationFactory para
  Integration/Security.
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
| `ContractTest` | `ContractTest/` | Pact contract tests |
| `MutationTest` | `MutationTest/` | Stryker mutation tests |
| `PerformanceTest` | `PerformanceTest/` | NBomber scenarios |
| `ChaosTest` | `ChaosTest/` | Chaos experiments + nightly runner |
| `fuzzing/` | `fuzzing/` | RESTler DAST config |
| `deploy/` | `deploy/` | docker-compose, CloudFormation, Grafana |
| `scripts/` | `scripts/` | Coverage check, quality metrics, deploy/destroy |
| `.github/` | `.github/` | CI/CD workflows, Dependabot, PR template |
| `.semgrep/` | `.semgrep/` | Custom SAST rules |
