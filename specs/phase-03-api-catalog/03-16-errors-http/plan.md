# plan.md — 03-16-errors-http

1. Mapeo

- Crear/Modificar: Middleware/ExceptionHandlingMiddleware.cs.

2. Guardarraíles

- Un solo middleware; sin try/catch ad-hoc.
- 500 sin stack trace en prod.

3. Pruebas

- IntegrationTest/Errors/ErrorHandlingTests.cs.

4. Secuencia

1. Middleware.
2. Mapeo.
3. Tests.
