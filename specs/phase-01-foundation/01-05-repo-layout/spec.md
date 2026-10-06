# 01-05 — Estructura del repositorio

## Contexto
Árbol objetivo del repositorio. **Depende de**: `01-01`.

## Requisitos
1. Carpetas de proyectos de test + auxiliares documentadas.
2. `.dockerignore`/`.gitignore` defensivos.
3. Build de la solución compila con 0 errores.

## Diseño

| Ruta | Tipo | Estado 01-05 | Nota / fase target |
|---|---|---|---|
| `WebAPIDevSecOpsScallingSDD/` | API principal | ✅ | — |
| `UnitTest/` | xUnit | ✅ | Unit tests + BuildSmoke + AppSettings + DI |
| `IntegrationTest/` | xUnit + WebApplicationFactory | ✅ | Middleware + Health |
| `SecurityTest/` | xUnit + WebApplicationFactory | ✅ | AssemblyIntegrity |
| `DatabaseTest/` | xUnit | ✅ (esqueleto) | Testcontainers en Fase 02 |
| `ContractTest/` | xUnit | ✅ (esqueleto) | Pact en Fase 10 |
| `MutationTest/` | biblioteca | ✅ (esqueleto) | Stryker en Fase 07 |
| `PerformanceTest/` | xUnit | ✅ (esqueleto) | NBomber en Fase 10 |
| `ChaosTest/` | xUnit | ✅ (esqueleto) | Chaos runner en Fase 09 |
| `deploy/` | docs+IaC | ✅ README | Compose/CloudFormation en Fase 09 |
| `scripts/` | utilidad | ✅ stub | check_coverage.py (Fase 07), run-chaos.ps1 (Fase 09) |
| `.semgrep/` | SAST | ✅ stub | semgrep.yaml Fase 07 |
| `fuzzing/` | docs | ✅ README | RESTler Fase 07 |
| `.github/` | meta | ✅ dependabot+PR template | workflows Fase 09 |

- `.gitignore`: bin/obj/.vs/secretos locales/reportes/perf-reports/coverage.
- `.dockerignore`: `.git/`, `.vs/`, `**/bin/`, `**/obj/`, `specs/`, reportes.

## Contratos
N/A — estructura de archivos.

## Tests
- N/A directo; verificación con `dotnet build -c Release` y los suites existentes.

## Criterios
- `dotnet build -c Release` 0 errores 0 advertencias sobre la slnx (9 proyectos).
- `dotnet test` Unit 8/8, Integration 5/5, Security 4/4 verdes.
- `MutationTest/`, `PerformanceTest/`, `ChaosTest/` compilan en la slnx.

## Límites
Los proyectos esqueleto no ejecutan sus suites aún; los scripts/IaC de despliegue viven en sus fases objetivo.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrada a plantilla `_template.md`; stubs compilables, slnx con 9 proyectos, .dockerignore nuevo.
