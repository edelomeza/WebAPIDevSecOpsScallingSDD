# 04-03 — Hardening HTTP

## Contexto
Cabeceras de seguridad y autorización a nivel de objeto. **Depende de**: `01-02`.

## Requisitos
1. Headers: nosniff, `X-Frame-Options: DENY`, `Referrer-Policy`, `X-XSS-Protection: 0`, HSTS 365d en prod, CSP con nonce.
2. CORS single origin + preflight.
3. Object-level auth: `ForbiddenAccessException` → 403.
4. Assembly integrity (ver `01-03`).

## Diseño
- Middleware de headers tras el pipeline base; respeta orden de `01-02`.

## Contratos
- N/A (headers y excepciones).

## Tests
- `SecurityTest/Headers/HeaderTests.cs`.

## Criterios
- Headers presentes; 403 en ownership ajeno.

## Límites
- HSTS solo no-Dev.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
