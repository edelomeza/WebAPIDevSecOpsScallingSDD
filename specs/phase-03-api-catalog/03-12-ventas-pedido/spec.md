# 03-12 — Ventas pedido

## Contexto
Endpoint de creación de pedido de la saga de ventas. **Depende de**: `03-00`, `03-16`, `04-04`, `06-01`, `06-02`, `06-04`, `03-01`, `03-03`.

## Requisitos
1. `POST /api/v1/ventas/pedido` (`AdminOnly+AdminPolicy`) crea `VenPedido` y publica `PedidoCreadoEvent`.
2. DTOs `PedidoCreateDto/PedidoDetalleCreateDto/PedidoResponseDto/PedidoDetalleResponseDto` + validadores.

## Diseño
- `VenPedido` con estados (valores en fase 06); evento inicial de la saga coreográfica.
- Alcance mínimo acotado (sin bus real): `VentasPedidoService` (`Create/GetById`, total servidor `decPrecio×cantidad`, SIN descuento de stock — lo valida `StockValidatorConsumer` en `06-02`), `IPedidoEventPublisher` + `FakePedidoEventPublisher` (`NOTE 06-01`), `PedidoCreadoEvent` POCO (`NOTE 06-03`), estado temporal `"Creado"` (`NOTE 06-04`), `AdminPolicy` existente (`NOTE 04-04`), `try/catch` manual hasta `03-16`.
- `Guid` cliente: pedido + detalles en un único `SaveChanges` (Tx explícita solo `IsRelational`).

## Contratos
- `PedidoCreateDto` → `PedidoResponseDto` (201); códigos 201/400/401/403/404/422.
- `GET /api/v1/ventas/pedido/{id}` → `200 PedidoResponseDto`; `404` si no existe.
- Caché `cache:pedido:{id}` + `pedido:version` TTL 60s.
- Datos asumidos (`02-04`): estado `saga`; `VenPedido` id=11111111-… estado `Registrado`; `CliCliente` id=1; `ProProducto` id=1.

## Tests
- `IntegrationTest/Saga/VentasPedidoTests.cs` (5: flujo 201/200 + stock intacto + 422 FKs + 400 + 404 + 403 `User`), `UnitTest/VentasPedido/` (13: servicio + consumer + validators + llaves/TTL/caché-stale/orden-2-detalles), `SecurityTest/Saga/` (2×401).
- Stryker (`stryker-0312.json`, `ignore-mutations Boolean`): gate ≥80%.

## Criterios
- Endpoints AdminOnly+AdminPolicy.
- `POST` crea pedido con `strEstadoSaga="Creado"`, publica 1 evento y no descuenta stock.
- `dotnet build -c Release --no-restore` → 0/0; `dotnet test <Unit|Integration|Security>Test -c Release --no-build` 100% verdes.
- Stryker en `VentasPedidoService` ≥80%; `dotnet build` tras Stryker antes de `--no-build` (`AGENTS.md` §4).

## Límites
- Pago/factura/dashboard en `03-13`…`03-15`; máquina de estados en fase 06.
- Bus real MassTransit/SQS + DLQ (`06-01`), consumers cableados + compensación (`06-02`), schemas definitivos (`06-03`), `UseRateLimiter` (`04-04`), middleware `03-16`.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador con evidencia T1 mínimo (pendiente firma)
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 07-Oct-2026
- **Detalle:** T1 mínimo ejecutado: `VentasPedidoController` (`POST/GET api/v1/ventas/pedido`, `AdminPolicy`), `VentasPedidoService` + `PedidoDtos` + `PedidoValidators` + `PedidoCreadoEvent` + `FakePedidoEventPublisher` + `StockValidatorConsumer` (stub solo-lectura); build 0/0, Unit 212/212, Security 55/55, Integration 72/72, Stryker 87.76% (`stryker-0312.json`).
- **Desviaciones registradas:** sin bus real (fake + `NOTE 06-01`); estado `"Creado"` temporal (`NOTE 06-04`); sin rate-limit (`NOTE 04-04`); `403` verificado con rol `User` (Bearer sin policy adicional, espejo 03-10/03-11); resto Stryker (2 compile-error `Count`, 1 `IsRelational`, 3 NoCoverage relacional, 2 `RemoveAsync` equivalente) solo cubrible en MsSql o equivalente — gate ≥80% cumplido con margen.
- **Desviaciones registradas:** sin bus real (fake + `NOTE 06-01`); estado `"Creado"` temporal (`NOTE 06-04`); sin rate-limit (`NOTE 04-04`); `403` verificado con rol `User` (Bearer sin policy adicional, espejo 03-10/03-11).
