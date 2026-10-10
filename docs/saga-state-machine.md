# Saga state machine (06-01 mínimo, canónico en `06-04`)

Coreografía pedido → stock → pago → factura. Sin orquestador: cada consumer avanza
o compensa. Fuente de estados: `VenPedido.strEstadoSaga`.

```text
Creado --(StockValidator: hay stock, reserva)--> StockValidado --(pago POST + PagoConsumer)--> PagoProcesado --(FacturaConsumer)--> Facturado
Creado --(StockValidator: sin stock)--> StockRechazado --(Compensation)--> Cancelado
StockValidado --(cobro fuera de orden / inválido + PagoConsumer)--> PagoRechazado --(Compensation: restaura reserva + anula pagos)--> Cancelado
PagoProcesado --(factura imposible + FacturaConsumer)--> FacturaRechazada --(Compensation 2 niveles: anula pagos + restaura reserva)--> Cancelado
```

| Transición | Consumer responsable | Condición |
|---|---|---|
| `Creado → StockValidado` | `StockValidatorConsumer` (`PedidoCreadoEvent`) | stock suficiente; reserva existencias |
| `Creado → StockRechazado` | `StockValidatorConsumer` (`PedidoCreadoEvent`) | sin stock o sin detalles |
| `StockValidado → PagoProcesado` | `PagoConsumer` (`PagoProcesadoEvent` del POST) | pedido en `StockValidado` |
| `* → PagoRechazado` | `PagoConsumer` | cobro sobre estado que no lo admite |
| `PagoProcesado → Facturado` | `FacturaConsumer` (`PagoProcesadoEvent`) | pedido en `PagoProcesado`; folio determinista `F-{año}-{pedidoId:N}` |
| `PagoProcesado → FacturaRechazada` | `FacturaConsumer` | estado que no admite factura (`StockValidado` temprano = reintento, no rechazo) |
| `*Rechazado → Cancelado` | `CompensationConsumer` | anula pagos (`Anulado`); restaura solo si hubo reserva |

Reglas:

- Idempotencia por `VenEventoProcesado` UNIQUE (`strNombreEvento`, `idPedido`); la marca
  viaja en el mismo `SaveChanges` que la mutación; los caminos de reintento no marcan.
- Cobro fuera de orden (pago antes de validar stock) se rechaza: `PagoRechazadoEvent`.
- `FacturaConsumer` ante `StockValidado` temprano lanza para reintento del bus (retry
  exponencial 5× 500ms→10s en InMemory); nunca traga veneno sin DLQ.
- Transporte `Transport=InMemory` local/test; `SQS` prod con colas `saga-*.fifo` y
  redrive DLQ (`Sqs:MaxReceiveCount`, aprovisionado en fase 09). Credenciales solo env/IAM.
- Eventos sin secretos ni PII (contratos `06-03` v1, `SchemaVersion = 1`).
- Legado: el seed usa `Registrado` (`02-04`/`TestDataFactory`); el canónico de 06-04
  decide su mapeo (los tests vivos usan los 8 estados de arriba).
