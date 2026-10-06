# plan.md — 01-01-setup-config

1. Mapeo

- Crear: WebAPIDevSecOps.slnx, Directory.Build.props, nuget.config, WebAPIDevSecOps/appsettings.Example.json.
- Modificar: WebAPIDevSecOps/WebAPIDevSecOps.csproj (flags).

2. Decisiones

- Flags: UseInMemoryDatabase, SkipMigration, EnableProviderStates, Transport, StackName.

3. Guardarraíles

- No hardcodear connection strings ni keys.
- Directory.Build.props centraliza analyzers y audit.

4. Pruebas

- Build: dotnet build -c Release 0 errores.
- UnitTest/Common/BuildSmokeTests.cs.

5. Secuencia

1. Crear archivos base.
2. Restaurar y compilar.
