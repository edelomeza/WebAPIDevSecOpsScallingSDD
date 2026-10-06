# plan.md — 08-02-quality-audit

1. Mapeo

- Crear: Middleware/AuditHashChain.cs, scripts/collect-quality-metrics.sh.

2. Guardarraíles

- Audit hash chain SHA-256 tamper-evident.
- Scripts atómicos (mktemp+mv), LC_NUMERIC=C.
- 4 gauges en /metrics.

3. Pruebas

- UnitTest/Audit/.
- CI: script produce quality-metrics.env.

4. Secuencia

1. Hash chain middleware.
2. Script de métricas.
3. Tests.
