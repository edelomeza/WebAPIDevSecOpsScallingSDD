# 01-01 — Setup & Config

## T1 — Solución base compila
- **Crear**: `WebAPIDevSecOpsScallingSDD.slnx`, `Directory.Build.props`, `nuget.config`, `WebAPIDevSecOpsScallingSDD/appsettings.Example.json`.
- **Modificar**: `WebAPIDevSecOpsScallingSDD/WebAPIDevSecOpsScallingSDD.csproj` (flags).
- **Verificar**: `dotnet build -c Release` 0 errores; `UnitTest/Common/BuildSmokeTests.cs`.
- **Guardarraíles**: no secretos; flags explícitos.
