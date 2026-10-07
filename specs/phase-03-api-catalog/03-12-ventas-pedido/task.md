# 03-12 — Ventas Pedido (ejecutado T1 mínimo 2026-10-07)

## T1 — Endpoint pedido saga (ejecutado 2026-10-07, alcance mínimo sin bus real)
- **Creados**: `Controllers/V1/VentasPedidoController.cs` (`api/v{version}/ventas/pedido`, `POST` 201 `CreatedAtAction` + `GET {id:guid}`, `[Authorize(Policy="AdminPolicy")]`, `422 {error}` FKs + `409` concurrencia), `Services/VentasPedidoService.cs` (`Create/GetById`, total servidor, SIN descuento de stock, `strEstadoSaga="Creado"` + `NOTE (06-04)`, Tx solo `IsRelational`, caché `cache:pedido:*` 60s), `Dtos/PedidoDtos.cs` (Create con `required` + Response con `RowVersion`), `Validators/PedidoValidators.cs` (`>0`, detalles `NotEmpty`), `Events/PedidoCreadoEvent.cs` (POCO + `NOTE 06-03`), `Services/PedidoEventPublisher.cs` (`IPedidoEventPublisher` + `FakePedidoEventPublisher` + `NOTE 06-01`), `Services/StockValidatorConsumer.cs` (stub solo-lectura `HasStockAsync` + `NOTE 06-02`), DI en `Program.cs`, `stryker-0312.json`.
- **Verificado**: POST crea pedido + publica 1 evento + stock intacto; `User` → 403; build 0/0, Unit 212/212, Security 55/55, Integration 72/72; Stryker 87.76% (gate ≥80%; resto solo-relacional/equivalente).
- **Guardarraíles**: `Guid` cliente → único `SaveChanges`; `ConfigureAwait(false)`; llaves interpoladas `$"..."`; sin `password/secret/token` en llaves.
- **Pendiente → fase 06/04**: bus MassTransit/SQS (`06-01`), consumers + compensación (`06-02`), schemas (`06-03`), estados (`06-04`), rate-limit (`04-04`), middleware errores (`03-16`).
- **Cierre**: conciliado con archivos reales; `Memoria.md` actualizado.
