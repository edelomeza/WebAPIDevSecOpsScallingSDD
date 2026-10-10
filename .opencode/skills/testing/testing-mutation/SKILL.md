---
name: testing-mutation
description: Mutation testing con Stryker.NET y umbrales documentados
---

## Propósito

Stryker.NET por slice con gate ≥80% y patrones de kill probados en `03-01`…`03-16`.

## Pasos

1. Crear `stryker-XXXX.json` en raíz (no en `MutationTest/`): `project` = csproj API, `test-projects` = `["UnitTest/UnitTest.csproj"]`, `mutate` = globs relativos al proyecto mutado (p. ej. `["**/VentaService.cs"]`), `ignore-mutations: ["Boolean"]` (`ConfigureAwait(false→true)` es equivalente), thresholds `break 80, low 80, high 90`, `reporters ClearText+Json`, `configuration Release`.
2. Correr detached (foreground excede 20 min interactivos): `Start-Process dotnet "stryker -f stryker-XXXX.json" -WindowStyle Hidden` con log, sondear `mutation score` (nightly: timeout 180 min). v5 eliminó `--config` (usar `-f|--config-file`; `--config` da `Unrecognized option`). Stryker corre los 5 proyectos de test por defecto (478 tests, 9 Docker-fallos sin daemon) → filtrar a Unit en local: `test-case-filter: "FullyQualifiedName~UnitTest."` (5min vs 75min, probado `04-02`).
3. Matar supervivientes con el catálogo: pág.2 vacía (`page=2,pageSize=2` distingue `Skip *→/`); borde exacto `50` con 12 items (distingue `>50` vs `>=50`); asserts de mensaje de excepción (`$"..."→$""`); asserts de llave de caché exacta (`cache:{ent}:...`); asserts de valores-versión (`version+1→-1`); test caché-stale (borra de DB, 2ª lectura sirve caché); orden con 2 detalles (`OrderBy→Desc`); cross-contexto InMemory (2º contexto no ve cambios sin `SaveChanges`); `ThrowingContext : AppDbContext` con flag `ThrowOnSave` (mata statement+string de ambos `catch` `DbUpdateConcurrencyException`/`DbUpdateException`, probado `03-13`: 86.36%→95.45%); fila de borde en CADA tabla filtrada por fecha (mutantes `>=/<=` por tabla son independientes; probado `03-15`: sembrar pago+factura en el pivote, no solo pedido); llave sanitizada exacta (`"_"→""` y `IsNullOrWhiteSpace→!=` solo mueren con `Assert.Equal` de la llave final, p. ej. `cache:dashboard:null:null:_-_-_-x`; probado `03-15`: 72%→100%); `Verify` fail-closed con `catch when` ante hashes corruptos + mutante `"m="→""` que sobrevive porque `AsSpan(2)` compensa la longitud del prefijo (matarlo exige hashes crafteados con prefijo contrabandeado, probado `04-02`).
4. Servicio GET-only sin `VersionKey` ni `InvalidateAsync` = menos superficie mutante (probado `03-14`: 100.00% al primer run, 11 killed + 6 ignored).
5. Tras cada run: `dotnet build` restaurativo antes de cualquier test `--no-build` (Stryker deja binarios mutantes en `bin/` → `TypeLoadException` fantasma).
6. Documentar iteraciones en `Memoria.md` (score inicial → final + lista de mutantes).

## Checklist

- Config por slice en raíz; `mutate` relativo; `Boolean` ignorado.
- `Logical (||→&&)` NO lo filtra `Boolean`: matarlo con tests.
- Score ≥80%; resto clasificado (ver Límites).

## Criterios de done

Gate ≥80% con margen; supervivientes restantes solo de clases aceptadas.

## Límites/trampas

- Aceptados fuera del scope UnitTest (documentar, no perseguir): mutantes CompileError (`Count→Sum`, `-` sobre `string+int` no compilan); `IsRelational()` y Tx/commit/dispose (solo MsSql relacional); `RemoveAsync` sobre `Guid` fresco (equivalente); `Safe Mode` puede ocultar métodos sin test.
- Precedentes: `03-04` 97.67%→100%, `03-10` 83.93%, `03-11` 86.05%→90.70%, `03-12` 76.00%→85.71%→87.76%, `03-13` 86.36%→95.45% (`ThrowingContext`; resto 2 `RemoveAsync` en id fresco equivalentes), `03-14` 100.00% primer run (GET-only, 11 killed + 6 ignored por `Boolean`+block-removal), `03-15` 72%→100.00% (borde en 3 tablas + llaves exactas; 47 killed + 1 CompileError `Count→Sum` + 28 ignored), `03-09` 68.42%→79.57%→94.62%→`93.41%` final tras refactor post-critic (`break 80`, 4 runs; equivalentes `new TwoFactorResult{Enabled=0}`, `RemoveAsync lockout:` inaccesible eliminado, `catch FormatException` eliminado — solo `ArgumentException`), `03-16` **100%** 1 run (`stryker-0316.json`), `04-01` slice→nightly (tope local 25min, umbral ≥83.93%), `04-02` **90.85%** (`stryker-0402.json`, 142 mutantes, 13 equivalentes: guardas misma-excepción, bloques fail-closed, guarda `<=0`, estado inalcanzable), `04-03` **100%** (`stryker-0403.json`, 18 mutantes, `ignore Boolean`, `break 80`) + `dotnet build` restaurativo + re-test filtro verde.

## Referencias

`stryker-0301.json`, `stryker-0309.json`, `stryker-0310.json`, `stryker-0311.json`, `stryker-0312.json`, `stryker-0313.json`, `stryker-0314.json`, `stryker-0315.json`, `stryker-0316.json`, `stryker-0402.json`, `stryker-0403.json`, `Memoria.md`.
