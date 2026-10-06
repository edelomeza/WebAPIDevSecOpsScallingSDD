# plan.md — 06-02-saga-flow

1. Mapeo

- Crear: Services/VentasPedidoService.cs, Services/CompensationService.cs.

2. Guardarraíles

- Estados correctos; compensación en fallo.
- Dual-write legacy correlacionado por LegacyVentaId.

3. Pruebas

- IntegrationTest/Saga/.

4. Secuencia

1. Saga estados.
2. Compensation.
3. Dual-write.
4. Tests.
