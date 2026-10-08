# 03-14 — Ventas factura

## Contexto
Endpoint de consulta de factura de la saga. **Depende de**: `03-13`, `06-01`, `06-02`, `06-03`, `05-01`, `04-04`.

## Requisitos
1. `GET /api/v1/ventas/factura/{id}` (`AdminOnly+AdminPolicy`) devuelve `VenPedidoFacturaResponseDto`.
2. `VenPedidoFactura` ya existía (fase 02) con `strFolioFactura` único.

## Diseño
- Alcance mínimo acotado (sin POST, sin folio secuencial, sin consumer): `VentasFacturaService` (`GetByIdAsync`, caché `cache:factura:{id}` TTL 60s, interpolación `$"..."`, `AsNoTracking`, `ConfigureAwait(false)`), `AdminPolicy` existente + `NOTE 04-04`, sin `try/catch` de dominio (solo lectura; `404` por nulo).
- `VenPedidoFactura` ya existía (fase 02); `strFolioFactura` único (múltiples facturas por pedido permitidas, decisión `02-03`).
- `NOTE`s `06-02` (consumer + eventos `FacturaGenerado/RechazadaEvent`), `06-04` (estados), `04-04` (rate-limit) en el servicio.

## Contratos
- `GET {id:int}` → `200 VenPedidoFacturaResponseDto`; `404` si no existe; `403` con rol `User`; `401` anónimo.
- Caché `cache:factura:{id}` TTL 60s.
- Fila `03-17` ya existente y coincidente (nombre real `VenPedidoFacturaResponseDto`).

## Tests
- `IntegrationTest/Saga/VentasFacturaTests.cs` (3: flujo 200 vía pedido API + inserción directa por `AppDbContext` scoped + 404 + 403 `User`), `UnitTest/VentasFactura/` (5: nulos, miss-sin-caché, persistencia+caché/llave/TTL, stale-caché, mapeo RFC/fecha), `SecurityTest/Saga/VentasFacturaSecurityTests.cs` (1×401).
- Stryker (`stryker-0314.json`, `ignore-mutations Boolean`): gate ≥80%.

## Criterios
- Endpoint AdminOnly+AdminPolicy.
- `GET` devuelve factura con `strFolioFactura`, `decTotal`, `strEstado`.
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security|Database>Test -c Release --no-build` 100% verdes.
- Stryker en `VentasFacturaService` ≥80%; `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).

## Límites
- `POST` + folio `F-{año}-{seq}` desde Redis, eventos `FacturaGeneradoEvent`/`FacturaRechazadaEvent`, `FacturaConsumer` cableado + compensación en fase 06 (`NOTE`s 06-01/06-02/06-03/06-04). Motivo técnico: `CacheService` solo expone `Get/Set/Remove` sin `Increment` atómico y TTL máx 120s (ver `05-01`).
- `UseRateLimiter` (`04-04`), middleware `03-16`.
- `UpdateDto/DeleteDto` diferidos (sin rutas en catálogo).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
- **Desviaciones registradas:** sin `POST`/`CreateDto`/validador/folio Redis/`FacturaConsumer` (todo diferido a fase 06 por decisión del usuario en plan; `task.md` original pedía `VentasFacturaService` + `FacturaConsumer` + folio consecutivo); solo lectura `GET {id:int}` como exige el catálogo `03-17`.
