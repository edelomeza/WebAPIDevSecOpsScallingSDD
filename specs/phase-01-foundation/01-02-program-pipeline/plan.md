# plan.md — 01-02-program-pipeline

1. Mapeo

- Modificar: WebAPIDevSecOps/Program.cs.
- Crear: WebAPIDevSecOps/Middleware/* (si no existen).

2. Decisiones

- Orden exacto de middleware (Serilog→…→MapControllers).
- DI de servicios, Kestrel limits, Serilog sinks, Observability:ConsoleExport gate.

3. Guardarraíles

- No reordenar middleware sin documentar.
- HSTS solo fuera de Development.

4. Pruebas

- IntegrationTest/Middleware/MiddlewareTests.cs.
- UnitTest/DI/ServiceRegistrationTests.cs.

5. Secuencia

1. DI de servicios.
2. Orden de middleware.
3. Serilog/Kestrel/CORS/HSTS.
