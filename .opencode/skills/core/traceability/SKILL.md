---
name: traceability
description: Enlazar spec↔código↔tests↔criterios en cada sub-spec y PR
---

## Propósito

Enlazar spec↔código↔tests↔criterios.

## Cuándo usarla

En cada sub-spec y PR.

## Precondiciones

Spec existente.

## Pasos

1. Citar spec en commits.
2. Test names referencian requisito.
3. Criterios listados en PR con conteos reales (no aspiracionales).
4. Al cerrar: `spec/plan/task.md` conciliados con archivos reales + `Desviaciones registradas` en `spec.md` + entrada en `Memoria.md`.
5. Estado: `🚧 Borrador con evidencia` al terminar código; `✅ Aprobado` SOLO si el usuario indicó revisor (+ PR verde). Sin indicación, queda en Borrador aunque todo esté verde (`03-11`, `03-12`).
6. Bitácora de pendientes en 4 capas: `Memoria.md` (cronológica) + `spec.md` (`Límites`/`Detalle: pendiente`) + `task.md` (`Pendiente → XX-YY`) + `NOTE (XX-YY)` en código + tabla `01-04` para config.

## Checklist

Cada requisito tiene ≥1 test; cada test tiene spec; desviaciones y diferidos registrados con spec dueña.

## Checklist

Cada requisito tiene ≥1 test; cada test tiene spec.

## Criterios de done

`CHECKLIST_PR.md` completo.

## Límites/trampas

No tests "sueltos" sin spec.
