# plan.md — 01-04-config-reference

1. Mapeo

- Crear/Modificar: appsettings.Example.json, appsettings.json, appsettings.Production.json.
- Modificar: AGENTS.md (tabla de config).

2. Decisiones

- Tabla de claves + env vars (DB_USER, DB_PASSWORD, PORT, CORS_ALLOWED_ORIGIN, PERF_*).

3. Guardarraíles

- Defaults seguros.
- Nunca commitear appsettings.json con secretos.

4. Pruebas

- UnitTest/Common/AppSettingsTests.cs.

5. Secuencia

1. Tabla de claves.
2. Defaults.
3. Ejemplo en JSON.
