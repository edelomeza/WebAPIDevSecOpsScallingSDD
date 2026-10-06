# plan.md — 07-02-test-quality

1. Mapeo

- Crear: MutationTest/stryker-config.json, UnitTest/PropertyBased/*.

2. Guardarraíles

- Stryker ≥80% (break 60).
- FsCheck determinista con .Trim().
- Cobertura línea ≥45%.

3. Pruebas

- CI: Stryker nightly 180min; FsCheck seeds.

4. Secuencia

1. Config Stryker.
2. Property-based tests.
3. Coverage runsettings.
