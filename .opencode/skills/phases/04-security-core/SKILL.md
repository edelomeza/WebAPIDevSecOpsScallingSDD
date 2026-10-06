---
name: 04-security-core
description: Hashing, JWT, rate limit, headers de seguridad, object-level auth
---

## Propósito

Hashing, JWT, rate limit, headers, object-level auth.

## Cuándo usarla

Fase 4.

## Pasos

PasswordHasherService Argon2id+BCrypt; JWT HS256≥32B; policies; headers+CSP nonce; ForbiddenAccessException→403.

## Checklist

`ValidAlgorithms`; lockout 5→15min; CORS única; assembly integrity.

## Criterios de done

alg=none rechazado; headers presentes; 403 en ownership.

## Límites/trampas

Key corta rompe startup; HSTS solo prod.

## Referencias

`04-01`…`04-03`.
