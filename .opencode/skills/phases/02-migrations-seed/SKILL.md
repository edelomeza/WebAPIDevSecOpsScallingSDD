---
name: 02-migrations-seed
description: Migraciones EF, rollback y seed idempotente para tests/proveedor
---

## Propósito

Migraciones y datos semilla.

## Cuándo usarla

Fase 2.

## Precondiciones

`02-01` definido.

## Pasos

1. Crear migraciones con `dotnet ef migrations add`.
2. Rollback a "0" debe dejar `__EFMigrationsHistory` vacío.
3. Implementar seed idempotente con `IDENTITY_INSERT`.
4. Exponer `/provider-states` para Pact.

## Checklist

12 tablas aplican; rollback a "0" OK; seed 2ª ejecución idempotente.

## Criterios de done

`DatabaseTest/MigrationTests.cs` y `/provider-states` 200.

## Límites/trampas

No ejecutar seed sin `IDENTITY_INSERT` en SQL Server.

## Referencias

`02-02`, `DatabaseTest`, `IntegrationTest/Common/ProviderStatesTests.cs`.
