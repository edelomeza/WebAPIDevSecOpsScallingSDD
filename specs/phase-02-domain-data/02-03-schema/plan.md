# plan.md — 02-03-schema

1. Mapeo

- Crear/Actualizar: specs/phase-02-domain-data/02-03-schema/spec.md.
- Referenciar en 02-01.

2. Decisiones

- 12 tablas con columnas, tipos, PKs, FKs, índices únicos, estados saga.

3. Guardarraíles

- No cambiar tipos sin migración.
- Estados saga enumerados y documentados.

4. Pruebas

- DatabaseTest/SchemaTests.cs.

5. Secuencia

1. Esquema por tabla.
2. Índices.
3. Estados saga.
