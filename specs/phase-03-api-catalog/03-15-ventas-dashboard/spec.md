# 03-15 — Ventas dashboard

## Contexto
Dashboard agregado del estado de la saga de ventas. **Depende de**: `03-12`, `03-13`, `03-14`, `06-04`, `08-01`, `04-04`.

## Requisitos
1. `GET /api/v1/ventas/dashboard` (`AdminOnly+AdminPolicy`) devuelve `DashboardDto`.
2. Opcional `DashboardFilterDto` + validador; métricas de saga y profundidad de cola.

## Diseño
- Agregación de solo lectura sobre pedidos/pagos/facturas + métricas `08-01`.

## Contratos
- `DashboardDto`; códigos 200/401/403.

## Tests
- `IntegrationTest/Saga/VentasDashboardTests.cs`.

## Criterios
- AdminOnly+AdminPolicy.

## Límites
- Sin escritura; métricas detalladas en fase 08.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
