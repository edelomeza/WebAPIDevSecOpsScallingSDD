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
5. Entidades saga (`VenPedidoPago/Factura`) pre-existen desde fase 02: prohibido recrearlas en slices (seed `F-SEED-1`); índice único filtrado `IS NOT NULL` admite múltiples `NULL` (test `NullTransaccionAllowsDuplicates`); InMemory NO impone índices únicos → el servicio exige pre-chequeo + `DbUpdateException→409` como red para SQL real.
6. `04-02`: migración `SegBloqueo0402` sin `_` (CA1707); `dotnet ef` sin `OnConfiguring` falla → factory design-time temporal + borrar; `nuget.config` `Konscious.*`/`BCrypt.Net*` + `PackageReference` directa en UnitTest (BCrypt no fluye transitivo); seed fail-closed ante placeholder/fake.

## Checklist

13 tablas aplican; rollback a "0" OK; seed 2ª ejecución idempotente.

## Criterios de done

`DatabaseTest/MigrationTests.cs` y `/provider-states` 200.

## Límites/trampas

No ejecutar seed sin `IDENTITY_INSERT` en SQL Server.

## Referencias

`02-02`, `DatabaseTest`, `IntegrationTest/Common/ProviderStatesTests.cs`.
