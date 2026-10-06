# 04-02 — Protección de credenciales

## Contexto
Hashing, anti-enumeración y lockout de credenciales. **Depende de**: `01-02`, `05-01`.

## Requisitos
1. `PasswordHasherService` Argon2id (64MB/3 iter) con fallback BCrypt solo migración; rehash transparente.
2. Fake hash anti-enumeración en login fallido.
3. Lockout tras 5 intentos (15 min).
4. Password nunca en caché ni logs.

## Diseño
- Policies de rate limit asociadas: Global 1000/min, Login 5/5min, Login2faVerify 10/5min, Admin 200/min, ConcurrentWrites 10.

## Contratos
- N/A (servicio interno + flags de lockout).

## Tests
- `UnitTest/Login/PasswordHasherTests.cs`, `SecurityTest/Login/LockoutTests.cs`.

## Criterios
- Login malo no revela usuario; password nunca en cache/logs.

## Límites
- Sin Argon2id débil; sin secretos en repo.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
