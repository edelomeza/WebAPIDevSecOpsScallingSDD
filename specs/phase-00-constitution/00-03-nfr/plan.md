# plan.md — 00-03-nfr

1. Mapeo

- Crear/Actualizar: specs/phase-00-constitution/00-03-nfr/spec.md.
- Modificar: AGENTS.md (sección NFR).

2. Decisiones

- Tabla NFR con umbrales medidos y herramienta de medición.

3. Guardarraíles

- Cada NFR debe tener comando de verificación (NBomber, chaos, tests).
- No declarar umbrales sin medir primero.

4. Pruebas

- NBomber: escenarios Login/Producto/Venta/Mixto.
- Chaos: redis-kill, sql-kill.
- Stryker: threshold ≥80% (break 60).

5. Secuencia

1. Listar NFR con herramienta.
2. Linkear a verificadores.
