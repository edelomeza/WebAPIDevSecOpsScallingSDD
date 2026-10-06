# plan.md — 09-05-chaos-experiments

1. Mapeo

- Crear: ChaosTest/Experiments/redis-kill.json, sql-kill.json, redis-latency.json.

2. Guardarraíles

- JSON schema válido.
- Verify retry 5×5s.
- Exit codes 0/1/2.
- Thresholds relajados.

3. Pruebas

- Chaos nightly: todos los experimentos pasan.

4. Secuencia

1. JSONs.
2. Runner.
3. Verify retry.
