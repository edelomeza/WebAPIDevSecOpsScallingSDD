# 00-02 — Gobernanza SDD

## Contexto
Define cómo se escriben, mantienen y verifican las specs del proyecto. **Depende de**: `00-01`.

## Requisitos
1. Toda spec usa la plantilla de `specs/_template.md`.
2. Cada spec define Definition of Done verificable.
3. Trazabilidad spec↔código↔test en cada cierre.
4. Revisión: aprobación de al menos 1 revisor antes de considerar "Aprobado".
5. Cambios de principios o stack requieren actualizar el bloque de aprobación y registrar el porqué en `Memoria.md`.

## Diseño
- Plantilla fija: Contexto, Requisitos, Diseño, Contratos, Tests, Criterios, Límites, Aprobación y Control de Cambios.
- DoD por tipo de spec: constitución (plantilla + aprobación), feature (plantilla + tests verdes + trazabilidad), fix (mismo DoD + issue vinculado).
- Versionado implícito en el bloque de aprobación (fecha + detalle de cambio).

## Contratos
N/A — spec de gobernanza. Referencia normativa: `specs/_template.md`.

## Tests
N/A — la verificación es la aplicación de la plantilla a un piloto de 3 specs (00-01, 00-03, 01-01).

## Criterios
- Cada spec nueva sigue `specs/_template.md` (medible: comparación estructural).
- Cada criterio de aceptación cita comando o test concreto.
- No se modifica la constitución sin registro en `Memoria.md`.

## Límites
No impone herramienta de gestión de specs; la trazabilidad es documental (markdown + enlaces).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Plantilla y DoD formalizados; piloto de migración aplicado a 00-01, 00-03 y 01-01.
