# 03-17 — Endpoint Catalog

## T1 — Catálogo consolidado vivo (opción B, ejecutado `phase03.11`)

- **Crear**: `docs/endpoints.md` (56 filas: 7 auth/misc + 1 ping raíz + 3 `probe` solo no-prod + 31 CRUD/search/autocomplete + 14 ventas/saga; rate-limit como `NOTE 04-04`; nota `ErrorResponse` post-`03-16`).
- **Corregir**: `spec.md` (tabla obsoleta → referencia canónica + 6 correcciones: `AdminOnly→AdminPolicy`, `TwoFactorSetupRequest` eliminado, `Usuario*→SegUsuario*`, rutas faltantes, rate `NOTE`, códigos `409/422/403`).
- **Crear**: `scripts/check_endpoints.ps1` (extrae `[HttpX]`+`[Route]` de `Controllers/V1/*.cs` + `MapGet probe` de `Program.cs`; falla si falta fila `| VERBO | ruta |` en `docs/endpoints.md`) + job `endpoints` en `ci-pr.yml`.
- **Actualizar**: `Memoria.md` (entrada 03-17).

## Verificar

- `powershell -File scripts/check_endpoints.ps1` → `OK exit 0` (56/56 rutas con fila).
- `powershell -File scripts/critic-guardrails.ps1` → `PASS exit 0`.
- Criterios del `spec.md`: 55 filas, script verde, nombres reales (sin `AdminOnly`/`TwoFactorSetupRequest`/`Usuario*`).

## Pendiente → firma

- Estado `🚧 Borrador con evidencia`; `✅ Aprobado` solo si el usuario indica revisor.
