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

1. Marcar cada endpoint con `[Authorize]`/`[Authorize(Policy="AdminOnly")]`.
2. Asignar policy de rate limiting (`LoginPolicy`, `AdminPolicy`, `ConcurrentWritesPolicy`).
3. Verificar claims requeridos (`role`, `sub`).
4. Revisar checklist ASVS L2 (anti-enumeration, secrets, SQLi, XSS, CSRF/CORS, JWT en logs).
5. Marcar cada ítem ASVS L2 como OK/Parcial/Fail con evidencia (test/comando/doc).

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
