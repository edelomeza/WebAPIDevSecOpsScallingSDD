# plan.md — 03-09-two-factor

1. Mapeo

- Crear: TwoFactorController, TwoFactorService, DTOs.

2. Guardarraíles

- Secreto TOTP encriptado.
- Máx 3 intentos por minuto.

3. Pruebas

- UnitTest/TwoFactor/.
- IntegrationTest/TwoFactor/.

4. Secuencia

1. Setup.
2. Verify.
3. Tests.
