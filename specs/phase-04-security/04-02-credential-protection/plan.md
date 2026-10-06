# plan.md — 04-02-credential-protection

1. Mapeo

- Crear: Services/PasswordHasherService.cs, Dto/PasswordHasherOptions.

2. Guardarraíles

- Argon2id + BCrypt fallback.
- Fake hash anti-enumeration.
- Lockout 5→15min.
- Policies documentadas.
- Rehash transparente.

3. Pruebas

- UnitTest/Login/PasswordHasherTests.cs.
- SecurityTest/Login/LockoutTests.cs.

4. Secuencia

1. Hasher.
2. Fake hash.
3. Lockout.
4. Tests.
