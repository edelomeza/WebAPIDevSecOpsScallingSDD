# 00-01 — Principios y stack

## Contexto
Primera spec de la constitución: registra principios no negociables y stack obligatorio. **Depende de**: —

## Requisitos
1. Listar principios con consecuencia práctica.
2. Tabla de stack por capa.
3. Regla de excepción para tecnologías fuera del stack.
4. Citado desde `AGENTS.md` y `00-02`.

## Diseño
- Principios: stateless, seguridad primero, fail-fast, empírico-antes-teórico, documentar límites.

| Principio | Consecuencia práctica |
|---|---|
| Stateless | Ningún estado de sesión en memoria de proceso; estado en SQL Server o Redis. |
| Seguridad primero | JWT HS256, Argon2id, lockout, 2FA TOTP, headers de seguridad y CSP (AGENTS.md §6); toda excepción de seguridad requiere issue. |
| Fail-fast | Errores uniformes, health checks reales, no tragar excepciones; Redis caído se refleja en `/health`. |
| Empírico antes que teórico | Medir antes de fijar umbrales, timeouts o nombres de métricas (skill `empirical-verification`). |
| Documentar límites | Límites conocidos en `Memoria.md`; prohibido workaround no documentado. |

- Stack por capa:

| Capa | Subcategoría | Tecnología |
|---|---|---|
| Runtime/API | Framework | .NET 10, ASP.NET Core |
| Datos | ORM / BD | EF Core, SQL Server |
| Cache | Distribuida / fallback | Redis, IMemoryCache |
| Mensajería | Saga / cola | MassTransit, SQS FIFO + DLQ |
| Auth | Tokens / hash / 2FA | JWT HS256, Argon2id, TOTP |
| Tests | Unitarios / integración / contrato | xUnit, FluentAssertions, Moq, FsCheck, Testcontainers, WebApplicationFactory, Pact |
| Tests | Calidad / performance / caos | Stryker, NBomber, Chaos experiments |
| SAST/DAST | Análisis | Semgrep, ZAP, RESTler |
| Contenedores | Seguridad | Trivy, dockle |
| Observabilidad | Logs / métricas / dashboards | Serilog, OpenTelemetry, Prometheus, Grafana |
| CI/CD | Pipeline / IaC / despliegue | GitHub Actions, CloudFormation, Docker Compose |
| Calidad continua | Gate | SonarCloud (informativo) |

## Contratos
N/A — spec constitucional. Referencia normativa en `AGENTS.md` §1.

## Tests
N/A — verificación manual de estructura (plantilla + aprobación).

## Criterios
- Cada principio cita consecuencia práctica (tabla anterior).
- Stack cubre todas las capas (tabla anterior).
- `AGENTS.md` y `00-02` citan `00-01`.

## Límites
No fija versiones exactas de paquetes ni umbrales; eso vive en specs de fase 01+.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrado a `specs/_template.md`; contenido conservado.
