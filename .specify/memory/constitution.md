# Constitución del Proyecto — WebAPIDevSecOpsScallingSDD

> Documento rector bajo Spec Driven Development (SDD). Define los principios,
> estándares y límites que todo spec, implementación y agente debe respetar.
> Ante conflicto entre este documento y otro artefacto, **manda esta constitución**.

## 1. Propósito y alcance

- Sistema: API REST stateless en ASP.NET Core 10, escalable horizontalmente,
  orientada a eventos (saga coreográfica), con DevSecOps en todo el ciclo.
- Metodología: Spec Driven Development. Toda funcionalidad nace de una spec en
  `/specs`; esta constitución gobierna la forma de las specs, su implementación
  y su verificación.
- Objetivo operativo: calidad reproducible, observabilidad, seguridad por
  defecto y despliegue temporal en AWS Free Tier.

## 2. Principios no negociables

1. **Stateless**: ningún estado de sesión en memoria de proceso; estado compartido
   en Redis (blacklist `blacklist:{jti}`, intentos/bloqueo 2FA `attempts:/lockout:`,
   cache) y en SQL Server (lockout de login en tabla persistente `SegBloqueo`
   5→15 min desde 04-02; solo 2FA/blacklist quedan en Redis).
2. **Seguridad primero**: validar en frontera, rechazar por defecto, mínimos
   privilegios, sin secretos en código ni en logs.
3. **Sin estado mutable oculto**: toda dependencia compartida se registra en DI;
   lo estático (p. ej. `TokenBlacklist`) expone `Reset()` y usa `lock`.
4. **Fail-fast y resiliente**: circuit breaker, timeouts, reintentos medidos;
   fallback explícito (Redis → IMemoryCache en ≤500 ms).
5. **Observabilidad desde el diseño**: métricas `/metrics`, salud `/health*`,
   audit hash chain, correlación por `X-Correlation-Id`.
6. **Empírico antes que teórico**: verificar nombres de métricas, formatos de
   reporte y umbrales midiendo, no asumiendo.
7. **Documentar límites conocidos** en lugar de parchea-hacks.

## 3. Stack y restricciones

| Capa | Tecnología obligatoria |
|---|---|
| Runtime | .NET 10 / ASP.NET Core 10 |
| Persistencia | SQL Server vía EF Core (migraciones), InMemory solo para tests |
| Estado distribuido / cache | Redis (StackExchangeRedis) + fallback IMemoryCache |
| Mensajería | MassTransit; transporte InMemory local, AmazonSQS en producción |
| Auth | JWT Bearer HMAC-SHA256, refresh tokens, 2FA TOTP, ValidAlgorithms=[HS256] |
| Hash de contraseñas | Argon2id (Konscious), fallback BCrypt `$2a$/$2b$` |
| Rate limiting | Global 1000/min; Login 5/5min; 2FA verify 10/5min; Admin 200/min; ConcurrentWrites 10 |
| Calidad | xUnit + FluentAssertions + Moq + FsCheck; Testcontainers; Pact; Stryker; NBomber; RESTler; ZAP; Semgrep; dockle; SonarCloud |
| Infra | Docker, docker-compose, CloudFormation, AWS ALB+EC2+SQS+RDS (temporal) |

No introducir librerías nuevas sin evaluar licencia, mantenimiento y seguridad.

## 4. Arquitectura

- API versionada por URL: `api/v{version:apiVersion}/[controller]`, v1.0.
- Respuesta JSON con convención legacy medida (prefijos minúsculos + resto
  PascalCase + `id`/sufijo, ver `IsConventional` en `ContractTest`), NO
  PascalCase puro (`PropertyNamingPolicy = null` solo desactiva el camelCase
  del serializador).
- Saga de ventas coreográfica: `VenPedido` + eventos `PedidoCreado → StockValidado
  / StockRechazado → PagoProcesado / PagoRechazado → FacturaGenerado /
  FacturaRechazada → compensaciones`. Consumidores: StockValidator, Pago,
  Factura, Compensation. Colas SQS FIFO + DLQ (maxReceiveCount 3).
- Cache-aside con Redis, claves con convención documentada y TTL 0–120 s
  (`CacheService` lo exige; páginas 60 s).
- Middleware en orden fijo (medido post-04-03/04-04): `SecurityHeadersMiddleware`
  outermost absoluta (antes de `DeveloperExceptionPage`, cubre 403/500; el nonce
  CSP vive dentro vía `NonceItemKey="CspNonce"`, sin middleware `CspNonce`
  separada) → `ExceptionHandlingMiddleware` → resto `01-02` → `UseRateLimiter`
  antes de `UseAuthentication` → `UseAuthorization` → Controllers
  (+ CORS, HSTS 365d vía `AddHsts` solo no-Dev, ForwardedHeaders, Serilog).
- Health: `/health`, `/health/ready` (DB), `/health-ui`. Métricas: `/metrics`.
- OpenAPI + Scalar solo en Development, ruta `/scalar`.

## 5. Estándares de código y API

- DTOs de entrada validados con FluentValidation; códigos HTTP correctos
  (201/200/204/400/401/403/404/409/422/429/500).
- Sin cadenas de conexión ni secretos hardcodeados (Semgrep + checklist).
- Inyección de dependencias y `IOptions`; nada de `new` para servicios clave.
- Complejidad ciclomática ≤ 10 por método (S1541) y cognitiva ≤ 15 (S3776).
- Naming DB con prefijos existentes (`str`, `int`, `dec`, `dte`, `bln`).
- `.editorconfig` con reglas CA3000+ como error; `SonarAnalyzer.CSharp` en build.

## 6. Seguridad (DevSecOps)

- Headers: X-Content-Type-Options, X-Frame-Options DENY, Referrer-Policy,
  X-XSS-Protection 0, HSTS 365d solo producción, CSP con nonce.
- JWT: clave ≥ 32 bytes, ClockSkew=Zero, issuer/audience validados,
  anti algorithm-confusion (`ValidAlgorithms`).
- Anti-enumeración en login (fake hash), lockout 5 intentos → 15 min.
- Token blacklist en Redis `blacklist:{jti}` + fallback memoria.
- Assembly integrity check en startup (`AssemblyIntegrity:ExpectedSha256`).
- Kestrel: 1000 conexiones, cuerpo 1 MB. CORS single origin.
- Sin logs de JWT ni credenciales; audit hash chain SHA-256 tamper-evident.

## 7. Testing y QA

- Seis suites obligatorias: Unit, Integration, Security, Database (Testcontainers),
  Contract (Pact), Mutation (Stryker). Perf con NBomber, fuzzing RESTler.
- Cobertura real medida (umbral CI 45%). Nuevo código ≥ 80% de
  cobertura en SonarCloud (`sonar.new.coverage.requirement=80`).
- Mutation score con gate Stryker ≥ 80% (scores medidos 90–100% por slice
  en 03-01…04-03; `test-case-filter FullyQualifiedName~UnitTest.` en local
  para excluir Docker-fallos); considerar mutantes Inmemory-unkillable
  documentados.
- Tests property-based FsCheck con comparación exacta (`.Trim()` en strings).
- Integration/Security usan `UseInMemoryDatabase=true`; `SegUsuario.RowVersion`
  `byte[]{1}`.
- Recovery: Testcontainers SQL con puerto fijo (`WithPortBinding(hostPort,1433)`).

## 8. CI/CD y calidad continua

Orden estricto: `restore → build → unit → integration → security → critic →
endpoints → contract → semgrep`, tests con `--no-build` Release. Nightly:
mutation (timeout 180 min), chaos. En PR: semgrep, ZAP, pr-quality-gate. SonarScanner con `/n:`. Majors de actions de
artifacts en pareja. Jobs agregadores tolerantes (`if: always()`,
`continue-on-error`, `if-no-files-found: warn`). Timeout/umbrales medidos
empíricamente antes de fijarse.

## 9. Observabilidad

- OpenTelemetry metrics + tracing (ASP.NET Core, HttpClient, EF Core).
- Prometheus exporter; nombres reales verificados con `curl /metrics`
  (p. ej. `test_coverage_percent`, `mutation_score_percent`, `p95_latency_ms_milliseconds`).
- Grafana dashboard de calidad (`deploy/grafana/quality-dashboard.json`).
- Logs Serilog JSON rolling; `Observability:ConsoleExport` off en tests.

## 10. Memoria y lecciones

- `Memoria.md` consolida lo aprendido por fase; se actualiza al cerrar cada fase.
- Reglas futuras vigentes (fuente canónica:
  `specs/phase-00-constitution/00-04-lessons-learned/spec.md`): verificar
  empíricamente, medir runtimes reales, resetear estado estático, chequear
  release de paquetes, documentar límites, tests de frontera exactos, socket
  real→proceso real, scripts con fallback y atomicidad, `.gitignore` defensivo,
  YAML CI validado.

## 11. Gobernanza SDD

- Las specs viven en `/specs` (o equivalente) y describen: contexto, requisitos,
  diseño, tareas, criterios de aceptación, y límites.
- Ninguna implementación modifica contratos sin actualizar spec + constitution
  si cambia un principio.
- `AGENTS.md` describe *cómo trabaja* el agente; esta constitución describe
  *qué no se negocia*. Ante duda, rige la constitución.
