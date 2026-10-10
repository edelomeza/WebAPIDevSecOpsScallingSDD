---
name: critic-guardrails
description: Gate pre-push diff-scoped con 4 bloqueantes + hardening PowerShell 5.1
---

## Propósito

Dueña de `scripts/critic-guardrails.ps1`: critica sin editar, diff-scoped vs `origin/main`, 4 bloqueantes + avisos. Full-repo grep descartado: la deuda pre-`03-16` legítima fallaría cada PR.

## Cuándo usarla

En cada PR antes del push; al endurecer el script; al declarar un waiver.

## Pasos

1. Ámbito = unión `git diff HEAD` + `git ls-files --others` (untracked no salen en `diff`; sin esto un untracked pasa si el diff no es vacío). Untracked sin diff requieren scan directo para B4.
2. B1 TODO: regex sobre diff (nunca `TODO`: S1135 lo convierte en error; usar `NOTE (XX-YY)`). Trampa: `# Todo` en prosa dispara el regex → reword a `# Ámbito`.
3. B2 secretos en DTOs/caché: `password/secret/token` salvo `blacklist:{jti}`. Waiver documentado `03-09`: el secreto de enrollment se devuelve UNA vez por requerimiento TOTP estándar (`TwoFactorDtos.cs:5`); jamás va a logs/caché/DB en claro (tests `DoesNotContain` + protegido en reposo vía DataProtection).
4. B3 auth 1-de-3 por controller nuevo: `AdminPolicy|[Authorize]|[AllowAnonymous]`.
5. B4 catch nuevo en Controllers: prohibido (canónico `03-16`: único try/catch en `ExceptionHandlingMiddleware`; ver `phases/03-errors-middleware`).
6. Hardening `Invoke-Git` (bugs cazados al resolver el merge a `main`, no teóricos): `git fetch` escribe `From https://...` a stderr y con `$ErrorActionPreference='Stop'` eso es terminating **aunque haya `2>$null`** → el `try/catch` lo tragaba y el PASS era vacuo. Fix: `2>&1` + filtrar `ErrorRecord`, EAP `Continue` localizado. Mensaje PASS dice cuántos ficheros: `PASS (N files scanned)`.
7. Self-test: scratch con TODO+sin-auth+catch+password → `FAIL 4/1 aviso, exit 1`; tras borrar → `PASS exit 0`. Re-verificado tras hardening: `PASS 9 ficheros exit 0` + `FAIL 2/1 aviso exit 1` con scratch.

## Checklist

- 4 bloqueantes diff-scoped verdes; avisos (401/S1541/Stryker) van a tests vía PR template, no bloquean aquí.
- `PASS (N files scanned)` visible; `exit 0/1` correcto (`shell: pwsh`, job `critic` paralelo sin `needs`, mismo SHA checkout).

## Criterios de done

`scripts/critic-guardrails.ps1` → `PASS exit 0` sobre el diff real del PR (evidencia fase 04: `PASS (11 files)` en `04-01`, `PASS` en `04-02/04-03/04-04/04-05` + `endpoints OK (56)` en cada una).

## Límites/trampas

- PS 5.1: `-UseBasicParsing`, EAP `Stop` + stderr de git = falso PASS (ver `operations/powershell-quirks`).
- No ampliar a full-repo sin reclasificar la deuda pre-`03-16`.

## Referencias

`scripts/critic-guardrails.ps1`, `.opencode/agents/security-reviewer.md`, `.opencode/agents/slice-scaffolder.md`, `CHECKLIST_PR.md` (8 checks), `.github/workflows/ci-pr.yml` (job `critic`), `Memoria.md` (Fase 1 híbridos + hardening).
