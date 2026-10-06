# 02-02 — Migraciones y seed

## Contexto
Migraciones EF Core y datos semilla mínimos sobre SQL Server real. **Depende de**: `02-01`.

## Requisitos
1. Generar migración `InitialCreate` con las 13 tablas contra provider SQL Server.
2. Verificar rollback a `"0"` deja `__EFMigrationsHistory` vacío.
3. Implementar `Context/DatabaseSeeder.cs`: seed mínimo idempotente con `IDENTITY_INSERT` por tabla, en transacción (lecturas EF + inserts SQL crudo).
4. Exponer `POST /provider-states` con `{ "state": "..." }` → 200/400, gateado por `EnableProviderStates` y bloqueado en prod.
5. Promover flags `SkipMigration`/`EnableProviderStates` a `appsettings.Example.json` y matriz 01-04.

## Diseño
- Tooling `dotnet-ef` 10.0.12 + paquete `Microsoft.EntityFrameworkCore.Design`; migraciones con env vars dummy (sin conexión real).
- Migraciones solo en tests (sin `Migrate()` en arranque, por decisión).
- Seeder: catálogos + 1 fila por tabla dependiente (ids explícitos); segunda ejecución no-op.
- Estados `base` (=seed); `race`/`saga`/`perf` definidos en `02-04`.
- `DatabaseTest` con `Testcontainers.MsSql` en puerto fijo; `IntegrationTest/Common` para provider-states.

## Contratos
- `POST /provider-states` `{ "state": "nombre_del_estado" }` → 200 `{state}` / 400 estado desconocido.
- Flags: `SkipMigration=false`, `EnableProviderStates=false` (ver 01-04).

## Tests
- `DatabaseTest/MigrationTests.cs`: migra, 13 tablas, seed ×2 idempotente, rollback a `"0"` vacío.
- `IntegrationTest/Common/ProviderStatesTests.cs`: `POST` base → 200 + seed visible; desconocido → 400.

## Criterios
- 13 tablas aplican; 2ª seed idempotente.
- `dotnet test DatabaseTest` y `dotnet test IntegrationTest` verdes.

## Límites
- Seed masivo de pruebas en `02-04`; valores de estados saga en fase 06.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** `InitialCreate`, seed idempotente `IDENTITY_INSERT`, `/provider-states`, flags promovidos.
