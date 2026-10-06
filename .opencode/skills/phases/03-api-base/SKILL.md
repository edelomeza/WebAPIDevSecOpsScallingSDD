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
2. Asegurar `PropertyNamingPolicy=null` (PascalCase).
3. Crear `ExceptionHandlingMiddleware` con mapeo 403/404/409/400/408/500.
4. Publicar catálogo consolidado de endpoints con método/ruta/auth/rate-limit.
5. Proveer fixtures JSON request/response por endpoint.

## Checklist

Rutas `api/v{version}/[controller]`; un solo middleware de errores; catálogo y contratos JSON completos.

## Criterios de done

Todas las rutas v1 resuelven; JSON PascalCase; errores uniformes.

## Límites/trampas

No mezclar PascalCase y camelCase; no try/catch ad-hoc.

## Referencias

`03-00`, `03-16`, `03-17`, `03-18`, `Program.cs`.
