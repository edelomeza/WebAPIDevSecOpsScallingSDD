# 04-05 — OWASP ASVS L2

## Contexto
Cobertura de amenazas según OWASP ASVS nivel 2. **Depende de**: `04-01`, `04-02`, `04-03`, `04-04`.

## Requisitos
1. Checklist ASVS L2 con estado y evidencia por capítulo.

## Diseño
- Documento de trazabilidad; sin código.

## Contratos
| Capítulo ASVS | Estado | Evidencia |
|---|---|---|
| V1 Arquitectura y diseño | Cubierto | Constitución + specs 00-01/01-02 |
| V2 Autenticación | Cubierto | 04-01/04-02: JWT, 2FA, lockout, anti-enumeration |
| V3 Gestión de sesiones | Cubierto | refresh rotado, blacklist jti, 04-01 |
| V4 Control de acceso | Cubierto | AdminOnly+AdminPolicy, ownership 403, 04-03 |
| V5 Validación | Cubierto | FluentValidation en frontera, 03-16 |
| V6 Criptografía | Cubierto | Argon2id 64MB/3 iter, JWT HS256, 04-02 |
| V7 Gestión de errores y logs | Cubierto | ExceptionHandling uniforme, audit hash chain |
| V8 Protección de datos | Cubierto | password nunca en cache/logs, 05-01 |
| V9 Comunicaciones | Cubierto | HSTS 365d prod, TLS, 04-03 |
| V14 Configuración | Cubierto | headers de seguridad, CSP nonce, assembly integrity |

## Tests
- N/A (trazabilidad documental; evidencia en suites de fase 04).

## Criterios
- Cada ítem documentado.

## Límites
- Solo L2 (no L3).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
