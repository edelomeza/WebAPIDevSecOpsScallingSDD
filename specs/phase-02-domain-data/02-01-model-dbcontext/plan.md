# plan.md — 02-01-model-dbcontext

1. Mapeo

- Crear: WebAPIDevSecOps/Models/*, WebAPIDevSecOps/Context/AppDbContext.cs.

2. Decisiones

- 12 entidades, naming, prefijos, RowVersion byte[]{1} en InMemory.

3. Guardarraíles

- No hardcodear connection strings; usar DI.
- RowVersion obligatorio para audit hash chain.

4. Pruebas

- UnitTest/DbContext/DbContextTests.cs.
- DatabaseTest/MigrationTests.cs (12 tablas).

5. Secuencia

1. Entidades.
2. DbContext.
3. Migraciones.
