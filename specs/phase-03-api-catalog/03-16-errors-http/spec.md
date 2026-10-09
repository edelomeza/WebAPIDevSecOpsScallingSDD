# 03-16 — Errores HTTP

## Contexto
Manejo uniforme de excepciones sin try/catch ad-hoc. **Depende de**: `01-02`.
**Implementado en**: rama `phase03.9` (2026-10-09).

## Requisitos
1. `ExceptionHandlingMiddleware` (único try/catch) con mapeo:
   `NotFoundException`→404, `ForbiddenAccessException`→403,
   `ConcurrencyConflictException`→409, `ValidationException`→422,
   `ArgumentException`→400, `TimeoutException`/`OperationCanceledException`→408,
   resto→500.
2. Cuerpo de error uniforme PascalCase `ErrorResponse { Error, Status, TraceId,
   Detail? }`; `Detail` (mensaje interno) solo no-prod, omitido en prod vía
   `JsonIgnore(WhenWritingNull)`; sin trazas en prod.
3. Sin try/catch en controllers: purga en 8 controllers + `EnsureOwner` ahora
   lanza `ForbiddenAccessException`; 21 `return NotFound()` migrados a
   `throw new NotFoundException(...)`.
4. `EmpEmpleado`: `ValidationException` de servicio normalizada a 422 (era el
   único 400 ad-hoc; validadores siguen 400 vía `ValidationProblem`).

## Diseño
- Middleware primero en el pipeline (tras `DeveloperExceptionPage` en Dev, que
  queda como outermost; reemplaza a `UseExceptionHandler` en no-Dev); orden
  `01-02` intacto.
- Respuesta ya iniciada (`HasStarted`) → relanza sin tocar.
- Tests de integración corren en `Staging` donde asertan errores por excepción
  (en Dev la página de excepciones devolvería HTML); `Production` exige
  override `UseInMemoryDatabase=true` (`appsettings.Production.json` apunta a
  SQL).
- Sondas `GET /api/v1/probe/{timeout,error,forbidden}` gated por
  `EnableProviderStates` + no-prod (patrón provider-states).

## Contratos
- `ErrorResponse` uniforme para todos los errores por excepción; 401/423 por
  resultado y `ValidationProblem` 400 sin cambios.

## Tests
- `UnitTest/Errors/` (14: tabla de mapeo 8 casos, nulos, passthrough,
  `Detail` prod/dev, `HasStarted` relanza).
- `IntegrationTest/Errors/ErrorHandlingTests.cs` (8: 404/403/409/422 reales,
  408/500 por sonda, `Detail` ausente en prod, sonda deshabilitada en prod).
- `stryker-0316.json`: muta `ExceptionHandlingMiddleware.cs` → **100%**.

## Criterios
- Sin try/catch ad-hoc (critic PASS); cuerpo uniforme PascalCase.
- Unit 289/289, Security 62/62, Integration 37/37 + 55/56 (fallo restante solo
  Redis Docker, ambiental) + Errors 8/8; Stryker 100% (break 80).

## Límites
- Sin trazas internas en prod (cuerpo genérico + `TraceId`).
- Suites Testcontainers (Docker ausente aquí) pendientes en CI.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario
- **Fecha:** 2026-10-09
- **Detalle:** implementado en rama `phase03.9`, mergeado PR #15 (`738ae13`); firma sin cambios sobre la evidencia (middleware + purga, Unit 289/289, Stryker 100%).
