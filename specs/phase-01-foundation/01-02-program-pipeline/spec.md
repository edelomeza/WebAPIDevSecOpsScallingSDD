# 01-02 — Pipeline de middleware y DI

## Contexto
Pipeline de middleware y DI correctos para la API. **Depende de**: `01-01`.

## Requisitos
1. Middleware importado en orden fijo y explícito.
2. DI registrada con los servicios fundacionales.
3. HSTS solo fuera de Development.
4. CORS bloqueado por defecto con orígenes configurables.
5. Límites defensivos desde `appsettings.json`.

## Diseño

| Orden | Middleware / Componente | Estado en Fase 01 | Meta / Destino Final |
|---|---|---|---|
| 1 | `UseForwardedHeaders` | ✅ Activado | IP real detrás de proxy |
| 2 | `UseHsts` | ✅ No-Dev | TLS en producción (fase 04 refinada) |
| 3 | `UseHttpsRedirection` | ✅ Activado | Enforzar TLS |
| 4 | `UseCors` | ✅ Activado | Orígenes desde `Cors:AllowedOrigins` |
| 5 | `MapOpenApi` (dev) | ✅ Activado | `/scalar` solo Dev (fase 03) |
| 6 | `UseAuthorization` | ✅ + `AddAuthorization` | Políticas por fase 04 |
| — | ResponseCompression / ResponseCaching | 🚧 Fase 05-cache | Cache-aside Redis |
| — | Serilog / CorrelationId / Audit | 🚧 Fase 08 | Correlación y audit chain |
| — | SecurityHeaders / CspNonce / RateLimiter | 🚧 Fase 04 | Headers y matriz auth |
| — | RequestTimeout | 🚧 Fase 04/08 | RequestTimeoutMiddleware |
| — | Health endpoints | 🚧 01-03 | `/health`, `/health/ready` |

## Contratos
- `Kestrel:Limits:MaxRequestBodySize` = 10485760 (10 MB), `KeepAliveTimeout` \"00:02:00\".
- `Cors:AllowedOrigins` array; default restringido.
- `Program` es `public partial` para `WebApplicationFactory<Program>`.

## Tests
- `UnitTest/DI/ServiceRegistrationTests.cs` (3): CORS options registradas, política sin `AllowAnyOrigin` por defecto, orígenes configurables.
- `IntegrationTest/Middleware/MiddlewareTests.cs` (3): `/ping` 200, HSTS ausente en Development, CORS bloquea orígenes desconocidos.
- `UnitTest/Common/BuildSmokeTests.cs` (2) se mantiene.

## Criterios
- `dotnet build -c Release` 0 errores 0 advertencias.
- `dotnet test IntegrationTest` 3/3 verdes.
- `dotnet test UnitTest` 5/5 verdes.

## Límites
El orden documentado en la especificación original queda como meta; slots diferidos se implementan en sus fases. No hay middleware custom de Seguridad/Observabilidad aún.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrada a plantilla `_template.md`; pipeline fundacional implementado y testeado.
