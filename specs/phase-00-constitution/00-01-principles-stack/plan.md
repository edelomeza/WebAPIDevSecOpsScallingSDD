# plan.md — 00-01-principles-stack

1. Mapeo de Componentes e Impacto

- Modificar: AGENTS.md (citar principios).
- Crear: specs/phase-00-constitution/00-01-principles-stack/spec.md con tabla de stack.

2. Decisiones de Arquitectura y Contratos

- Stack por capa: .NET 10, EF Core, Redis, MassTransit/SQS, JWT, Argon2id, xUnit/Stryker/NBomber/Pact/Testcontainers, ZAP/Semgrep/dockle/SonarCloud.
- Regla de excepción: documentar riesgo y plan de remoción.

3. Guardarraíles de Seguridad (DevSecOps)

- Prohibido introducir librerías sin evaluar licencia/mantenimiento/seguridad.
- No hardcodear secretos; appsettings + env vars.

4. Estrategia de Validación y Pruebas

- Revisión humana: spec aprobada por al menos 1 revisor.
- Consistencia: todas las specs futuras citan 00-01.

5. Secuencia

1. Redactar spec.md.
2. Referenciar en AGENTS.md y 00-02.
3. Validar que cada tecnología del stack tiene spec que la cubre.
