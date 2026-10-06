# 03-00 — Routing y versionado

## Contexto
Convención de rutas versionadas y serialización JSON para toda la API. **Depende de**: `01-02`.

## Requisitos
1. Configurar `AddApiVersioning` (`DefaultApiVersion=1.0`, `UrlSegmentApiVersionReader`, `AssumeDefaultVersionWhenUnspecified=false`).
2. Rutas `api/v{version:apiVersion}/[controller]`.
3. JSON PascalCase (`PropertyNamingPolicy=null`).
4. OpenAPI y Scalar solo en Dev; ocultos en Production.

## Diseño
- `Program.cs`: `AddApiVersioning().AddMvc().AddApiExplorer()` + `AddControllers().AddJsonOptions()`; `MapControllers()` tras `UseAuthorization()`; `MapOpenApi()` + `MapScalarApiReference()` solo Dev.
- `Controllers/V1/PingController.cs` como probe (`api/v1/ping` → 200 `{"Status":"Pong","Version":"v1"}`).
- Paquetes: `Asp.Versioning.Mvc` + `ApiExplorer` 10.2.1, `Scalar.AspNetCore` 2.17.13.

## Contratos
- `GET /api/v1/ping` → 200; sin versión → 404 (estricto).
- `/openapi/v1.json`, `/scalar/v1` solo Dev.

## Tests
- `IntegrationTest/Routing/RoutingTests.cs`: v1 resuelve + PascalCase; sin versión 404; openapi/scalar 200 en Dev y 404 en Production.

## Criterios
- Ruta v1 resuelve; JSON PascalCase.
- `dotnet test IntegrationTest` verdes.

## Límites
- Sin endpoints de negocio (slices `03-01`…); AV0029/AV0030 suprimidos (APIs inexistentes en v10.2.1; `/openapi/v1.json` verificado por test).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** versionado 1.0 estricto, PascalCase, Scalar solo Dev, `PingController`, RoutingTests.
