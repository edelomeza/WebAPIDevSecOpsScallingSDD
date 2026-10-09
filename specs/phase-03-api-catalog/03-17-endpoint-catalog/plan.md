# plan.md — 03-17-endpoint-catalog

## 1. Mapeo

- Crear: `docs/endpoints.md` (fuente única; el `spec.md` enlaza, no duplica).
- Crear: `scripts/check_endpoints.ps1` + job `endpoints` en `ci-pr.yml`.
- Tocar: `spec.md` (Contratos/Criterios/Aprobación), `task.md`, `Memoria.md`.

## 2. Decisiones

- Tabla método/ruta/auth/rate-limit(`NOTE 04-04`)/DTO/response/códigos, 55 filas medidas desde los 17 controllers + `Program.cs` (`probe`).
- `TwoFactorSetupRequest` no existe (`setup` sin DTO) → fila sin request.
- `PingResponse` anidada en el controller; `POST detalles` con ruta absoluta `~/...`; pago-por-pedido sin 404.
- Script en PowerShell (sin dependencias: el job CI ya usa `pwsh`); comparación por token `| VERBO | ruta |` en minúsculas.

## 3. Secuencia

1. Inventariar controllers (`[HttpX]`+`[Route]` por action) + `MapGet probe`.
2. Escribir `docs/endpoints.md`.
3. Conciliar `spec.md` (6 correcciones + criterios medibles).
4. Reescribir `task.md`/`plan.md` a formato estándar.
5. Crear script + job CI; correr ambos scripts en verde.
6. Entrada en `Memoria.md`; commit en `phase03.11`.
