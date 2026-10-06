# plan.md — 06-01-transport-events

1. Mapeo

- Modificar: Program.cs (MassTransit transport).
- Crear: Consumers/*, Events/*.

2. Guardarraíles

- InMemory solo Dev/test.
- SQS en prod con DLQ, maxReceiveCount=3.
- Consumers idempotentes.

3. Pruebas

- IntegrationTest/Events/.
- ChaosTest/Experiments/ (SQS caída → DLQ).

4. Secuencia

1. Consumers.
2. Transporte.
3. Tests.
