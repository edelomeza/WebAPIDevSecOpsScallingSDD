---
name: testing-mutation
description: Mutation testing con Stryker.NET y umbrales documentados
---

## Propósito

Stryker.NET por slice con gate ≥80% y patrones de kill probados en `03-01`…`03-12`.

## Pasos

1. Crear `stryker-XXXX.json` en raíz (no en `MutationTest/`): `project` = csproj API, `test-projects` = `["UnitTest/UnitTest.csproj"]`, `mutate` = globs relativos al proyecto mutado (p. ej. `["**/VentaService.cs"]`), `ignore-mutations: ["Boolean"]` (`ConfigureAwait(false→true)` es equivalente), thresholds `break 80, low 80, high 90`, `reporters ClearText+Json`, `configuration Release`.
2. Correr detached (foreground excede 20 min interactivos): `Start-Process dotnet "stryker --config-file stryker-XXXX.json" -WindowStyle Hidden` con log, sondear `mutation score` (nightly: timeout 180 min).
3. Matar supervivientes con el catálogo: pág.2 vacía (`page=2,pageSize=2` distingue `Skip *→/`); borde exacto `50` con 12 items (distingue `>50` vs `>=50`); asserts de mensaje de excepción (`$"..."→$""`); asserts de llave de caché exacta (`cache:{ent}:...`); asserts de valores-versión (`version+1→-1`); test caché-stale (borra de DB, 2ª lectura sirve caché); orden con 2 detalles (`OrderBy→Desc`); cross-contexto InMemory (2º contexto no ve cambios sin `SaveChanges`).
4. Tras cada run: `dotnet build` restaurativo antes de cualquier test `--no-build` (Stryker deja binarios mutantes en `bin/` → `TypeLoadException` fantasma).
5. Documentar iteraciones en `Memoria.md` (score inicial → final + lista de mutantes).

## Checklist

- Config por slice en raíz; `mutate` relativo; `Boolean` ignorado.
- `Logical (||→&&)` NO lo filtra `Boolean`: matarlo con tests.
- Score ≥80%; resto clasificado (ver Límites).

## Criterios de done

Gate ≥80% con margen; supervivientes restantes solo de clases aceptadas.

## Límites/trampas

- Aceptados fuera del scope UnitTest (documentar, no perseguir): mutantes CompileError (`Count→Sum`, `-` sobre `string+int` no compilan); `IsRelational()` y Tx/commit/dispose (solo MsSql relacional); `RemoveAsync` sobre `Guid` fresco (equivalente); `Safe Mode` puede ocultar métodos sin test.
- Precedentes: `03-04` 97.67%→100%, `03-10` 83.93%, `03-11` 86.05%→90.70%, `03-12` 76.00%→85.71%→87.76%.

## Referencias

`stryker-0301.json`, `stryker-0310.json`, `stryker-0311.json`, `stryker-0312.json`, `Memoria.md`.
