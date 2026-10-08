# plan.md — 03-13-ventas-pago (ejecutado T1 mínimo 2026-10-07)

1. Mapeo

- Creados: VentasPagoController, VentasPagoService (+IVentasPagoService), PagoDtos, PagoValidators. `VenPedidoPago` ya existía (fase 02). `PagoConsumer`/eventos/publisher diferidos a fase 06 por decisión del usuario.

2. Guardarraíles

- AdminOnly+AdminPolicy (`NOTE 04-04` rate-limit).
- Estado temporal `"Procesado"` (`NOTE 06-04`); sin bus/consumer (`NOTE`s 06-01/06-02/06-03); `try/catch` manual hasta `03-16`.
- Stryker ≥80% (`ignore-mutations Boolean`); `dotnet build` restaurativo tras cada run.

3. Pruebas

- `UnitTest/VentasPago/` (12) + `IntegrationTest/Saga/VentasPagoTests.cs` (7) + `SecurityTest/Saga/VentasPagoSecurityTests.cs` (3).
- Verificado: build 0/0; Unit 226/226; Security 58/58; Integration 79/79; Database 2/2; Stryker 95.45% (42 killed/2 survived/33 ignored; resto 2 `RemoveAsync` equivalente en id fresco, fuera del scope — mismo criterio que 03-12) + build restaurativo.

4. Secuencia

1. DTOs + validators.
2. Servicio + DI.
3. Controller.
4. Tests + Stryker + docs.
