# plan.md — 02-02-migrations-seed

1. Mapeo

- Modificar: WebAPIDevSecOps/Migrations/.
- Crear: WebAPIDevSecOps/Context/DatabaseSeeder.cs.
- Modificar: IntegrationTest/Common/* (Testcontainers).

2. Decisiones

- Seed idempotente con IDENTITY_INSERT.
- /provider-states para Pact.

3. Guardarraíles

- No ejecutar seed sin IDENTITY_INSERT en SQL Server.
- Rollback a "0" debe dejar __EFMigrationsHistory vacío.

4. Pruebas

- DatabaseTest/MigrationTests.cs.
- IntegrationTest/Common/ProviderStatesTests.cs.

5. Secuencia

1. Migraciones.
2. Seed.
3. provider-states.
