# 06-04 — Máquina de estados de saga

## Contexto
Estados, transiciones y compensaciones de la saga (aquí se enumeran los valores de `strEstadoSaga`). **Depende de**: `06-02`.

## Requisitos
1. Documentar el diagrama completo sin estados huérfanos.
2. Cada transición indica consumer responsable y condición.
3. Compensación en 2 niveles; DLQ con `maxReceiveCount` 3.

## Diseño
- Creado → (StockValidator) → StockValidado → (Pago consumer) → PagoProcesado → (Factura consumer) → Facturado.
- StockValidado → sin stock → (StockValidator) → StockRechazado → compensación.
- Pago consumer falla → PagoRechazado → restaurar stock → cancelación.
- Factura consumer falla → FacturaRechazada → anular pago + restaurar stock.
- Consumers: StockValidator, Pago, Factura, Compensation.
- `docs/saga-state-machine.md` con el diagrama.

## Contratos
- Valores de `VenPedido.strEstadoSaga` y `strEstado` de pago/factura.

## Tests
- `IntegrationTest/Saga/` verde.

## Criterios
- Diagrama completo.

## Límites
- Sin transiciones sin consumer; sin estados sin evidencia.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
