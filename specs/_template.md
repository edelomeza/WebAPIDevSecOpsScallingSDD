# Plantilla de spec — SDD

Toda spec del repositorio sigue esta estructura. Las 7 secciones son obligatorias para specs orientadas a código; ver reglas de opcionalidad al final.

```markdown
# NN-NN — Título

## Contexto
Qué problema resuelve y qué fase/specs la preceden. Incluir: **Depende de**: `NN-NN`.

## Requisitos
Qué debe hacer el sistema. Lista numerada, lenguaje imperativo.

## Diseño
Enfoque técnico: capas, patrones, archivos/rutas tocados, decisiones de arquitectura.

## Contratos
APIs, DTOs, eventos, claves, flags de configuración o esquemas de datos afectados. "N/A" si no aplica.

## Tests
Rutas y tipos de tests que cubren los requisitos (Unit/Integration/Security/Contract/Performance/Chaos).

## Criterios
Criterios de aceptación verificables con comando o test (≥/</mensajes/ambas ramas).

## Límites
Lo que explícitamente no cubre la spec, límites conocidos y trabajo diferido.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado | 🚧 Borrador | ❌ Rechazado
- **Revisores:** @handle (N Revisores)
- **Fecha:** DD-Mmm-AAAA
- **Detalle:** decisión registrada, excepciones documentadas.
```

## Reglas de opcionalidad

- Specs constitucionales/no orientadas a código (p. ej. principios, NFR, gobernanza): `Contratos` y `Tests` pueden ser "N/A", justificando en la propia sección.
- Todo cambio de principios o del stack exige actualizar versión y `Detalle` del bloque de aprobación.
- Los criterios deben ser medibles: incluir el comando o test que los verifica.
- Trazabilidad obligatoria al cerrar: spec ↔ código ↔ test.
