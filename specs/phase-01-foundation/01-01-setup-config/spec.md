# 01-01 — Setup y configuración

## Contexto
Dejar compilando la solución base. **Depende de**: `00-01`, `00-02`.

## Requisitos
1. Estructura de solución `.slnx` válida.
2. `Directory.Build.props` con NuGetAudit, SonarAnalyzer, AnalysisMode All.
3. `nuget.config` y `appsettings.Example.json` presentes.
4. Flags soportados: `UseInMemoryDatabase`, `SkipMigration`, `EnableProviderStates`, `Transport`, `StackName`.
5. `dotnet restore` y `dotnet build` funcionales.

## Diseño
- Solución: `WebAPIDevSecOpsScallingSDD.slnx` (formato XML nuevo, no `.sln`).
- Flags leídos desde configuración/variables de entorno; nunca secretos en archivos.

## Contratos
- `appsettings.Example.json` documenta llaves configurables; `UseInMemoryDatabase=true` permite levantar sin SQL Server.

## Tests
- `UnitTest/Common/BuildSmokeTests.cs` — smoke de compilación y configuración.

## Criterios
- `dotnet build -c Release` → 0 errores.
- `dotnet test UnitTest --no-build` → BuildSmokeTests en verde.

## Límites
No cubre migraciones ni seed (ver 01-02+); no levanta infraestructura (docker compose queda en fase 09).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrado a `specs/_template.md`; contenido y rutas de tests conservados.
