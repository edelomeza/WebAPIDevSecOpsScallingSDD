# 01-03 — Startup & Health

## T1 — Salud y arranque
- **Modificar**: `Program.cs` (health endpoints, assembly integrity, migraciones tolerantes).
- **Verificar**: `/health/ready` 200/503; `SecurityTest/Startup/AssemblyIntegrityTests.cs`; `IntegrationTest/Health/HealthTests.cs`.
- **Guardarraíles**: no exponer stack trace en prod.
