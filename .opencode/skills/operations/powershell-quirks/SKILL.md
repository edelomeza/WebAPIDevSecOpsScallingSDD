---
name: powershell-quirks
description: Evitar falsos negativos en scripts PowerShell 5.1
---

## Propósito

Evitar falsos negativos en scripts.

## Pasos

`-UseBasicParsing`; `utf-8-sig`; validar CRLF en variables CI.
Para inspeccionar `mutation-report.json` (PowerShell enreda la navegación del JSON): script `.js` en temp + `node` (patrón probado `03-13/03-14`: contar por `status` y listar no-`Killed` con `mutatorName`+`location`).
Hardening `critic-guardrails.ps1` (bugs reales del merge a `main`, detalle en `operations/critic-guardrails`): helper `Invoke-Git` con `2>&1` + filtrar `ErrorRecord` y EAP `Continue` localizado (`git fetch` escribe `From https://...` a stderr y con EAP `Stop` eso es terminating aunque haya `2>$null` → PASS vacuo); ámbito = unión `git diff HEAD` + `git ls-files --others` (untracked sin diff + scan directo B4); mensaje `PASS (N files scanned)`; self-test scratch `FAIL 4/1 aviso exit 1 → PASS exit 0`; `# Todo` en prosa dispara el regex de TODO.

## Checklist

Status 0 no implica OK; verificar con grupo control.

## Referencias

`agent.md` Fase 6, `operations/critic-guardrails`.
