# 03-15 — Ventas dashboard

## Contexto
Dashboard agregado del estado de la saga de ventas. **Depende de**: `03-12`, `03-13`, `03-14`, `06-04`, `08-01`, `04-04`.

## Requisitos
1. `GET /api/v1/ventas/dashboard` (`AdminOnly+AdminPolicy`) devuelve `DashboardDto`.
2. Opcional `DashboardFilterDto` + validador; métricas de saga y profundidad de cola.

## Diseño
- Agregación de solo lectura sobre pedidos/pagos/facturas + métricas `08-01`.
- T1 ejecutado 2026-10-08: `DashboardDto {TotalPedidos/Pagos/Facturas, MontoTotalPedidos/Pagos/Facturas, PorEstadoSaga[{Estado, Total}] OrderBy(Estado), ProfundidadCola=0}`; filtros sueltos `desde/hasta/estadoSaga` (rango sobre fecha propia de cada entidad; estado solo filtra pedidos); cola fake `0` + `NOTE (06-01)`; caché `cache:dashboard:{desde}:{hasta}:{estado-sanitizado}` TTL 60s sin `VersionKey` (modelo GET-only `03-14`; sanitiza `password/secret/token/":"` en la llave).

## Contratos
- `DashboardDto`; códigos 200/400/401/403 (`400` por `DashboardFilterValidator`: rango invertido o `EstadoSaga>50`).

## Tests
- `UnitTest/VentasDashboard/VentasDashboardServiceTests.cs` (8) + `DashboardFilterValidatorTests.cs` (5).
- `IntegrationTest/Saga/VentasDashboardTests.cs` (3).
- `SecurityTest/Saga/VentasDashboardSecurityTests.cs` (1x401).

## Criterios
- AdminOnly+AdminPolicy.

## Límites
- Sin escritura; métricas detalladas en fase 08.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador con evidencia T1 (pendiente firma; bus/DLQ/compensación → fase 06, métricas OTel → `08-01`)
- **Revisores:** —
- **Fecha:** —
- **Detalle:** T1 ejecutado 2026-10-08: `VentasDashboardController`, `DashboardService` (`IVentasDashboardService.GetAsync`), `DashboardDtos`, `DashboardFilterValidator`, DI en `Program.cs`, `stryker-0315.json`; build 0/0, Unit 244/244, Security 60/60, Integration 85/85, Database 2/2; Stryker 100.00% (47 killed, 1 compile-error `Count→Sum` no computable, 28 ignored `Boolean`).
