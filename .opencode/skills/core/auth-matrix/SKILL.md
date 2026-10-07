---
name: 04-auth-matrix
description: Matriz auth/rate-limit por endpoint + checklist OWASP ASVS L2
---

## Propósito

Matriz auth/rate-limit por endpoint + checklist OWASP ASVS L2.

## Cuándo usarla

Al proteger endpoints y al auditar seguridad.

## Precondiciones

`04-01`, `04-02`, `04-03`, `04-04`, `04-05` definidos.

## Pasos

1. Marcar cada endpoint con `[Authorize]` o `[Authorize(Policy = "AdminPolicy")]` (requiere rol `Admin`; es la policy real — `AdminOnly` no existe en código).
2. Rate limiting diferido a `04-04`: registrar `NOTE (04-04)` por endpoint (policies objetivo: Login 5/5min, Login2faVerify 10/5min, Global 1000/min, Admin 200/min, ConcurrentWrites 10).
3. Verificar claims (`NameIdentifier`/`sub` para ownership dif. `NOTE 04-01`; `role` para `AdminPolicy`).
4. Matriz de tests probada: `Admin→2xx`, `User→403` con `AdminPolicy`, `User→200` en Bearer sin policy (desviación espejo `03-10/03-11`, no 403), anónimo→401 sin fugas.
5. Revisar checklist ASVS L2 (anti-enumeration, secrets, SQLi, XSS, CSRF/CORS, JWT en logs).

## Checklist

- Matriz endpoint↔auth↔rate-limit completa.
- OWASP ASVS L2 con estado de cada ítem.
- Cada endpoint tiene auth y rate-limit explícitos.

## Criterios de done

- Cada endpoint tiene auth y rate-limit explícitos.
- ASVS L2 revisado y con evidencia.

## Límites/trampas

- No dejar endpoints sin rate-limit.
- No loggear JWT.
- No marcar ASVS como OK sin evidencia.

## Referencias

`04-04`, `04-05`, `CHECKLIST_PR.md`.
