---
name: 03-api-base
description: Versionado, errores uniformes, catálogo de endpoints y contratos JSON
---

## Propósito

Fijar versionado, errores uniformes, catálogo de endpoints y contratos JSON.

## Cuándo usarla

Al definir la capa API antes de implementar endpoints.

## Precondiciones

`01-02` (pipeline), `03-00`, `03-16`, `03-17`, `03-18` definidos.

## Pasos

1. Configurar `AddApiVersioning` con `DefaultApiVersion=1.0` y `UrlSegmentApiVersionReader`.
2. `PropertyNamingPolicy=null` (sin camelCase del serializador; la convención de NOMBRES es la legacy, no PascalCase puro — ver punto 5).
3. Crear `ExceptionHandlingMiddleware` con mapeo 403/404/409/400/408/500.
4. Catálogo en fuente única `docs/endpoints.md` (el `spec.md` solo enlaza, no duplica; ver `operations/drift-guards`): método/ruta/auth/rate-limit(`NOTE 04-04`)/DTOs/códigos.
5. Contratos JSON en `ContractTest/Fixtures/*.json` por captura real (ver `testing/testing-pact`): convención de nombres MEDIDA en `IsConventional` — prefijos legacy minúsculos (`str/int/dec/dte/bln` + mayúscula/dígito) + `id`/sufijo PascalCase + resto PascalCase (`RowVersion`). "PascalCase puro" es FALSO en este repo (lo tumbó el test de `03-18`: `id`, `strNombre`, `bln2FAHabilitado`, `idCliCliente`).
6. Canon `03-16` (detalle en `phases/03-errors-middleware`): `ErrorResponse {Error/Status/TraceId + Detail solo no-prod}`, `NotFound/ForbiddenAccessException`, middleware primero + fuera `UseExceptionHandler` no-Dev, purga try/catch en 8 controllers + `21 NotFound()→throw` + `EnsureOwner→403` (outlier `EmpEmpleado` FK `400→422`), sondas `probe/{timeout,error,forbidden}` gateadas `EnableProviderStates`+no-prod, `10` factories a `Staging`, Stryker `100%` (`stryker-0316.json`).

## Checklist

Rutas `api/v{version}/[controller]`; un solo middleware de errores; catálogo y contratos JSON completos.

## Criterios de done

Todas las rutas v1 resuelven; nombres según convención legacy medida; errores uniformes; `check_endpoints` + `ContractTest` verdes.

## Límites/trampas

No asumir PascalCase puro (ver punto 5); no mezclar camelCase del serializador; no try/catch ad-hoc; no duplicar el catálogo en el spec.

## Referencias

`03-00`, `03-16`, `03-17`, `03-18`, `Program.cs`, `phases/03-errors-middleware`, `operations/critic-guardrails`, `operations/drift-guards`, `testing/testing-pact`.
