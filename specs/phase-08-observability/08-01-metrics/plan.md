# plan.md — 08-01-metrics

1. Mapeo

- Modificar: Program.cs (OpenTelemetry, Prometheus).
- Crear: Services/QualityMetricsService.cs.

2. Guardarraíles

- Resolver singleton eager de Meter.
- Nombres verificados con curl /metrics.
- No registrar Meter perezoso.

3. Pruebas

- IntegrationTest/Metrics/.
- UnitTest/Metrics/.

4. Secuencia

1. OTel setup.
2. Meter.
3. Prometheus endpoint.
4. Tests.
