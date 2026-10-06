# plan.md — 01-03-startup-health

1. Mapeo

- Modificar: Program.cs (health endpoints, assembly integrity, migraciones tolerantes).
- Crear: WebAPIDevSecOps/Services/AssemblyIntegrityService.cs (si aplica).

2. Decisiones

- /health, /health/ready, /health-ui; fallo de migración no tumba la app.

3. Guardarraíles

- No exponer stack traces en prod.
- Assembly integrity: warning en Dev, error en Prod si mismatch.

4. Pruebas

- IntegrationTest/Health/HealthTests.cs.
- SecurityTest/Startup/AssemblyIntegrityTests.cs.

5. Secuencia

1. Health endpoints.
2. Assembly integrity.
3. Migraciones tolerantes + warmup.
