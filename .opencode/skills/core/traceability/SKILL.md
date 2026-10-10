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
7. Fase 1 (gates directos de esta skill): Fase A `slice-scaffolder` (test-aserta-diferido) + `security-reviewer` (`edit: deny`, diff-scoped vs `origin/main`, 4 bloqueantes + avisos) + `operations/critic-guardrails` (TODO / secretos salvo `blacklist:{jti}` / auth 1-de-3 / catch nuevo; hardening `Invoke-Git 2>&1` + `ls-files --others` + `PASS (N files)`); evidencia `03-16` exigible (`stryker-0316.json 100%`, sondas `probe`).
8. Firma masiva y cierre de addenda (probado cierre fase 03, 09-Oct-2026): timbrar en bloque los specs con evidencia mergeada (`✅ Aprobado (@usuario, fecha)`, firma sin cambios); addenda T2 ya ejecutados → `✅ Aprobado (fusionado y cerrado)` + línea T2 en el Detalle del principal (puntero al addendum + doc canónico) en vez de reescribir el principal; entrada resumen (`Fase 03 al 100%: 19/19 + addenda`).
9. Matrices vivas fase 04 (verifica `traceability-clerk`, no editar: reportar drift con `ruta:linea`): `04-04` — todo endpoint de `docs/endpoints.md` tiene fila en `docs/rate-limit-matrix.md` con auth + policy explícitas; endpoint nuevo sin fila es gap. `04-05` — todo ítem `Cubierto` en `docs/asvs-l2-checklist.md` tiene evidencia (test, job o spec mergeada); ítem que cite spec en Borrador es cobertura ficticia. `Contratos`/`spec.md` enlazan canónicos, nunca duplican tablas (ver `operations/drift-guards`).

## Checklist

Cada requisito tiene ≥1 test; cada test tiene spec; desviaciones y diferidos registrados con spec dueña; Fase A + critic verdes.

## Criterios de done

`CHECKLIST_PR.md` completo.

## Límites/trampas

No tests "sueltos" sin spec.
