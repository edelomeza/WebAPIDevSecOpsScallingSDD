# 00-03 — NFR

## Contexto
Requisitos no funcionales del sistema. **Depende de**: `00-01`.

## Requisitos
1. Umbrales medibles y herramienta de medición por NFR.
2. Cada NFR verificable con comando o script de verificación.

## Diseño

| NFR | Categoría | Métrica / Umbral | Estado del Umbral | Herramienta | Comando / Script de Verificación |
|---|---|---|---|---|---|
| Latencia Login p95 | Performance | ≤ 500 ms | 🎯 Objetivo (Por medir) | NBomber | `dotnet run --project PerformanceTest/` 🚧 fase 10-testing |
| Latencia Producto | Performance | ≤ 200 ms | 🎯 Objetivo (Por medir) | NBomber | `dotnet run --project PerformanceTest/` 🚧 fase 10-testing |
| Latencia Venta | Performance | ≤ 1000 ms | 🎯 Objetivo (Por medir) | NBomber | `dotnet run --project PerformanceTest/` 🚧 fase 10-testing |
| Escenario Mixto | Performance | ≤ 800 ms | 🎯 Objetivo (Por medir) | NBomber | `dotnet run --project PerformanceTest/` 🚧 fase 10-testing |
| Error rate | Performance | ≤ 0.5% | 🎯 Objetivo (Por medir) | NBomber | `dotnet run --project PerformanceTest/` 🚧 fase 10-testing |
| Timeout de request | Performance | 60 s | 📈 Medido real (configuración) | Middleware | `RequestTimeoutMiddleware` en `WebAPIDevSecOpsScallingSDD/` |
| Kestrel | Performance | 1000 conexiones / 1 MB | 📈 Medido real (configuración) | Kestrel limits | `appsettings.json` / `Program.cs` |
| Redis caído fallback | Disponibilidad | ≤ 500 ms | 🎯 Objetivo (Por medir) | CacheService+IMemoryCache | `SecurityTest`/`IntegrationTest` fallback 🚧 |
| Chaos redis-kill | Resiliencia | HTTP 200 / `/health` 503 | 🎯 Objetivo (Por medir) | Chaos runner | `run-chaos.ps1 redis-kill` 🚧 fase 09-cicd-ops |
| Chaos sql-kill | Resiliencia | circuit breaker / `/health/ready` 503 | 🎯 Objetivo (Por medir) | Chaos runner | `run-chaos.ps1 sql-kill` 🚧 fase 09-cicd-ops |
| Security ASVS | Seguridad | ASVS L2 | 🎯 Objetivo (Por medir) | Checklist | `04-05` ASVS checklist 🚧 fase 04 |
| Timeout HTTP cliente | Performance | 60 s | 📈 Medido real (configuración) | HttpClient | `Program.cs` |
| Mutation Score | Mantenibilidad | ≥ 30% (break 60) | 🎯 Objetivo (Por medir) | Stryker | `dotnet stryker` 🚧 fase 07-quality |
| Cobertura de código | Mantenibilidad | ≥ 45% | 🎯 Objetivo (Por medir) | Coverage | `python scripts/check_coverage.py` 🚧 fase 07-quality |
| Calidad ISO 25010 | Calidad | Quality Gate | 🎯 Objetivo (Por medir) | SonarCloud | SonarCloud PR gate (informativo) 🚧 fase 07 |
| Exposición de métricas | Observabilidad | `/metrics` OK | 📈 Medido real | OTel/Prometheus | `curl -s http://localhost:5000/metrics` |
| Estado del servicio | Salud | `/health/ready` OK | 📈 Medido real | ASP.NET Health | `curl -s http://localhost:5000/health/ready` |

## Contratos
N/A — spec constitucional de umbrales.

## Tests
NBomber (PerformanceTest, pendiente fase 10), chaos runner (pendiente fase 09), Stryker (pendiente fase 07), SecurityTest (ASVS L2), IntegrationTest.

## Criterios
- Cada fila cita comando o script de verificación concreto.
- Filas 📈 Medido real tienen comando ejecutable hoy; 🎯 Objetivo declaran su fase objetivo.

## Límites
Los umbrales marcados 🎯 Objetivo se promoverán a 📈 Medido real de forma iterativa al desplegar los proyectos correspondientes. No se fijan timeouts/umbrales sin medición real (principio 00-01).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrado a `specs/_template.md`; tabla ampliada con Categoría, Estado del Umbral y Comando/Script; pendientes marcados con su fase objetivo.
