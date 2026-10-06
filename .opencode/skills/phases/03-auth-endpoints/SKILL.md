---
name: 03-auth-endpoints
description: Login, 2FA, refresh, logout con anti-enumeration y lockout
---

## Propósito

Login, 2FA, refresh, logout.

## Cuándo usarla

Fase 6.

## Pasos

LoginService anti-enumeration+lockout+rehash; 2FA TOTP; tempToken 5min; refresh rotation; logout blacklist.

## Checklist

Rate limit Login/Login2faVerify; Redis lockout; JWT+refresh emitidos.

## Criterios de done

Login 200 con token; credenciales malas 401 sin revelar; 2FA verify OK.

## Límites/trampas

Timing attack; token reutilizado debe fallar.

## Referencias

`03-06`…`03-08`, `04-01`, `04-02`.
