# 06-02 — Flujo de saga

## Contexto
Saga coreográfica de ventas con compensación en 2 niveles. **Depende de**: `06-01`, `03-10`.

## Requisitos
1. Implementar el flujo coreográfico pedido → stock → pago → factura.
2. Compensar ante fallo de pago/factura (restaurar stock, anular pago).

## Diseño
- Sin orquestador central; cada consumer avanza o compensa (ver `06-04`).

## Contratos
- Eventos de `06-03`; estados en `VenPedido.strEstadoSaga` (valores en `06-04`).

## Tests
- `IntegrationTest/Saga/`.

## Criterios
- Estados correctos; compensación en fallo de pago/factura.

## Límites
- Casos borde de doble compensación en `06-04`.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
