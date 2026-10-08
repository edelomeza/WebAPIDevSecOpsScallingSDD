# 03-13 — Ventas pago

## Contexto
Endpoint de consulta/procesamiento de pago de la saga. **Depende de**: `03-12`, `06-01`, `06-02`, `06-03`, `04-04`.

## Requisitos
1. `POST /api/v1/ventas/pago` (`AdminOnly+AdminPolicy`) crea `VenPedidoPago` con `strIdTransaccion` único.
2. `GET /api/v1/ventas/pago/{id}` (`AdminOnly+AdminPolicy`) devuelve `PagoResponseDto`.
3. `GET /api/v1/ventas/pago/pedido/{pedidoId}` (`AdminOnly+AdminPolicy`) lista los pagos del pedido.

## Diseño
- Alcance mínimo acotado (sin bus real, sin consumer): `VentasPagoService` (`Create/GetById/GetByPedidoId`, `strEstado` temporal `"Procesado"` + `NOTE 06-04`, `dteFechaPago=UtcNow` servidor, `strIdTransaccion`/`strMetodoPago` normalizados `Trim` + vacío→`null`, `AdminPolicy` existente + `NOTE 04-04`, `try/catch` manual hasta `03-16`).
- `VenPedidoPago` ya existía (fase 02); `strIdTransaccion` único filtrado `IS NOT NULL` (múltiples `NULL` permitidos, decisión `02-03`).
- Duplicado `strIdTransaccion` no-nulo → pre-chequeo + `DbUpdateException` → `ConcurrencyConflictException` → `409` (InMemory no impone el índice único; el pre-chequeo lo cubre en tests).
- Caché `cache:pago:{id}` + `cache:pago:pedido:{version}:{pedidoId}` + `pago:version` TTL 60s (interpolación `$"..."`, sin `+`).

## Contratos
- `PagoCreateDto {idVenPedido required Guid, decMonto required >0, strMetodoPago?(Max50), strIdTransaccion?(Max100)}` → `PagoResponseDto` (201 `CreatedAtAction`); códigos 201/400/401/403/404/409/422.
- `GET {id:int}` → `200 PagoResponseDto`; `404` si no existe.
- `GET pedido/{pedidoId:guid}` → `200 PagoResponseDto[]` (vacío si sin pagos); `403` con rol `User`.
- Caché `cache:pago:{id}` + `pago:version` TTL 60s.
- Fila añadida en `03-17` (nombre real `PagoResponseDto`).

## Tests
- `IntegrationTest/Saga/VentasPagoTests.cs` (7: flujo 201/200 + por-pedido + 409 duplicado + 422 FK + 400 + 404 + vacío + 403 `User`), `UnitTest/VentasPago/` (12: nulos, FK con mensaje + sin insert, duplicado→409 con mensaje, `NULL` duplicable, blanco→`null`, create+caché/TTL/llaves, versión 2 tras 2 creates, stale-caché, por-pedido ordenado+caché, vacío, validators), `SecurityTest/Saga/VentasPagoSecurityTests.cs` (3×401).
- Stryker (`stryker-0313.json`, `ignore-mutations Boolean`): gate ≥80%.

## Criterios
- Endpoints AdminOnly+AdminPolicy.
- `POST` crea pago con `strEstado="Procesado"`, `strIdTransaccion` único (duplicado → 409).
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` 100% verdes.
- Stryker en `VentasPagoService` ≥80%; `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).

## Límites
- Cobro real, eventos `PagoProcesadoEvent`/`PagoRechazadoEvent`, `PagoConsumer` cableado + compensación en fase 06 (`NOTE`s 06-01/06-02/06-03/06-04).
- `UseRateLimiter` (`04-04`), middleware `03-16`.
- `UpdateDto/DeleteDto` diferidos (sin rutas en catálogo).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario (1 Revisor)
- **Fecha:** 08-Oct-2026
- **Detalle:** T1 mínimo ejecutado: `VentasPagoController` (`POST/GET {id:int}/GET pedido/{pedidoId:guid} api/v1/ventas/pago`, `AdminPolicy`), `VentasPagoService` + `PagoDtos` + `PagoValidators`, DI en `Program.cs`, `stryker-0313.json`; build 0/0, Unit 226/226, Security 58/58, Integration 79/79, Database 2/2, Stryker 95.45% (`stryker-0313.json`).
- **Desviaciones registradas:** sin `PagoConsumer`/eventos/publisher (todo diferido a fase 06 por decisión del usuario; `task.md` pedía `PagoConsumer`); estado `"Procesado"` temporal (`NOTE 06-04`); sin rate-limit (`NOTE 04-04`); `403` verificado con rol `User`; nombre real `PagoResponseDto` (spec decía `VenPedidoPagoResponseDto`); `GET pedido/{pedidoId}` añadido (task pedía GET por pedidoId, spec solo GET por id).
