# 01-03 — Arranque y health

## Contexto
Checks de salud y arranque resiliente. **Depende de**: `01-02`.

## Requisitos
1. Endpoints `/health` (liveness) y `/health/ready` (readiness).
2. Check de integridad del assembly con SHA-256 vs valor esperado.
3. Sin stack traces en producción.
4. Migraciones tolerantes (diferido a Fase 02).

## Diseño

| Componente | Estado 01-03 | Meta / Destino |
|---|---|---|
| `/health` (liveness) | ✅ Activado | Siempre OK si el proceso corre |
| `/health/ready` (readiness) | ✅ Activado (200 estático) | + check DB en Fase 02 |
| AssemblyIntegrityCheck | ✅ Activado | Warning Dev / Unhealthy Prod |
| Exception handler JSON genérico | ✅ Activado (no-Dev) | Sin stack trace |
| DeveloperExceptionPage | ✅ Solo Dev | — |
| Health UI | 🚧 Fase 08 | Monitoreo externo |
| Migraciones tolerantes + health DB real | 🚧 Fase 02 | EF Core + SQL Server |

## Contratos
- Config: `AssemblyIntegrity:ExpectedSha256` (env o appsettings); si ausente, `Degraded` en Dev y `Healthy` en Prod con nota.
- `PORT` env respetado por Kestrel; `DB_USER`/`DB_PASSWORD` se definen en Fase 02.

## Tests
- `SecurityTest/Startup/AssemblyIntegrityTests.cs` (4): hash correcto → Healthy; incorrecto → Unhealthy en Prod / Degraded en Dev; ausente → Degraded Dev / Healthy Prod.
- `IntegrationTest/Health/HealthTests.cs` (2): `/health` 200 con contenido, `/health/ready` 200 en Fase 01.

## Criterios
- `dotnet build -c Release` 0 errores 0 advertencias.
- `dotnet test IntegrationTest` 5/5 verdes; `dotnet test SecurityTest` 4/4 verdes.
- Mismatch de hash en Prod → `Unhealthy` (probado en SecurityTest).

## Límites
Readiness hoy no valida DB (su slot de Fase 02 queda documentado); health UI se omite por minimización de superficie de ataque; migraciones tolerantes migran a Fase 02.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrada a plantilla `_template.md`; health endpoints y assembly integrity testeables.
