# plan.md — 10-testing-strategy

1. Mapeo

- Crear: docs/testing-strategy.md.

2. Decisiones

- Matriz suite↔responsabilidad.
- Cobertura objetivo, flakiness rules, xunit.runner.json, --blame-crash.

3. Guardarraíles

- Cada tipo de test tiene suite y umbral.

4. Pruebas

- CI: todas las suites con --no-build.

5. Secuencia

1. Definir matriz.
2. Umbrales por suite.
3. Flakiness rules.
