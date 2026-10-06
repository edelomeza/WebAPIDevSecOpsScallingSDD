# plan.md — 03-15-ventas-dashboard

1. Mapeo

- Crear: VentasDashboardController, DashboardService, DashboardDto.

2. Guardarraíles

- AdminOnly+AdminPolicy.
- No exponer datos sensibles de pedidos.

3. Pruebas

- IntegrationTest/Saga/VentasDashboardTests.cs.

4. Secuencia

1. Service.
2. Controller.
3. Tests.
