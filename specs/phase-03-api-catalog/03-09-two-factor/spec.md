# 03-09 — Two-factor

## Contexto
Alta y verificación TOTP sobre `SegUsuario`. **Depende de**: `03-05`, `04-01`.

## Requisitos
1. `POST /api/v1/two-factor/setup` (Bearer) devuelve `otpauth://` + secreto.
2. `POST /api/v1/two-factor/verify` (Bearer) habilita 2FA (`bln2FAHabilitado`).
3. Validadores `TwoFactorSetupRequestValidator`, `TwoFactorVerifyRequestValidator`.

## Diseño
- Secreto en `str2FASecreto`; setup no habilita hasta verify exitoso.

## Contratos
- `TwoFactorSetupRequest/Response`, `TwoFactorVerifyRequest/Response`; 200/401.

## Tests
- `UnitTest/TwoFactor/`, `IntegrationTest/TwoFactor/`, `SecurityTest/TwoFactor/`.

## Criterios
- Setup devuelve otpauth://; verify habilita 2FA.

## Límites
- Login 2FA en `03-07`.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
