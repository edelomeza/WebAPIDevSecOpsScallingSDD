# 03-16 — Errors & HTTP

## T1 — Middleware de errores ✅ ejecutado 2026-10-09
- **Crear/Modificar**: `Middleware/ExceptionHandlingMiddleware.cs`,
  `Dtos/ErrorResponseDto.cs`, `Services/NotFoundException.cs`,
  `Services/ForbiddenAccessException.cs`, sondas probe gated en `Program.cs`
  (pipeline: middleware primero, fuera `UseExceptionHandler` no-Dev).
- **Verificar**: mapeo 404/403/409/422/400/408/500 con cuerpo uniforme PascalCase
  (`Error/Status/TraceId`, `Detail` solo no-prod); sin try/catch ad-hoc (critic
  PASS); 21 `NotFound()`→throw; EmpEmpleado FK 400→422. Unit 289/289, Security
  62/62, Integration Errors 8/8 (+37/37 y 55/56 en suites migradas), Stryker
  100% (break 80, `stryker-0316.json`).
