# 03-14 — Ventas Factura (ejecutado T1 mínimo 2026-10-08)

## T1 — Endpoint factura saga, solo lectura (alcance mínimo sin POST/folio/consumer)
- **Creados**: `Dtos/FacturaDtos.cs` (`VenPedidoFacturaResponseDto`, nombre real según catálogo `03-17`), `Services/VentasFacturaService.cs` (`IVentasFacturaService.GetByIdAsync`, caché `cache:factura:{id}` TTL 60s, `NOTE`s 06-02/06-04/04-04), `Controllers/V1/VentasFacturaController.cs` (`GET {id:int} api/v1/ventas/factura`, `[Authorize(Policy="AdminPolicy")]`, 200/404), DI en `Program.cs`, `stryker-0314.json`. `VenPedidoFactura` ya existía (fase 02).
- **Diferido a fase 06** (decisión del usuario en plan): `POST` + folio `F-{año}-{seq}` desde Redis + `FacturaConsumer` + eventos `FacturaGenerado/RechazadaEvent` (motivo técnico: `CacheService` sin `Increment` atómico, TTL máx 120s).
- **Verificado**: GET por id (200/404/403 `User`/401); build 0/0, Unit 231/231, Security 59/59, Integration 82/82, Database 2/2; Stryker 100.00% (11 killed, 6 ignored por config; gate ≥80%).
- **Guardarraíles**: solo lectura sin `try/catch` de dominio; llaves interpoladas `$"..."`; sin `password/secret/token` en llaves; inserción en Integration vía `AppDbContext` scoped + limpieza cliente/producto vía API.
- **Pendiente → fase 06/04**: folio secuencial Redis, consumer + compensación (`06-02`), schemas (`06-03`), estados (`06-04`), rate-limit (`04-04`), middleware errores (`03-16`).
- **Cierre**: conciliado con archivos reales; fila `03-17` ya coincidente (sin cambios); `Memoria.md` actualizado.
