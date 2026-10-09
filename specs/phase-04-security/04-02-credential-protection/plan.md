# plan.md — 04-02-credential-protection (ejecutado 09-Oct-2026)

1. Mapeo

- Crear: `Services/PasswordHasherOptions.cs`, `Services/LoginLockoutStore.cs`, `Models/SegBloqueo.cs`, `Migrations/*_SegBloqueo0402.cs`, `UnitTest/Login/PasswordHasherTests.cs`, `SecurityTest/Login/AntiEnumerationTests.cs`, `SecurityTest/Cache/NoLeakTests.cs`.
- Modificar: `Services/SegUsuarioPasswordHasher.cs` (interfaz `NeedsRehash` + `Argon2IdSegUsuarioPasswordHasher`, S101), `Services/LoginService.cs` (lockout por store, ctor 4 args), `Context/AppDbContext.cs` (`SegBloqueos` + índice único), `Program.cs` (DI hasher factory + `TimeProvider` + store), `nuget.config` (`Konscious.*`, `BCrypt.Net*`), `appsettings.Example.json` (`PasswordHasher:`), `UnitTest/Login/LoginServiceTests.cs` + `Login2fa/*Tests.cs` (ctor 4 args).

2. Guardarraíles

- Argon2id piso + BCrypt solo `Verify`; Fake intacto (`NeedsRehash=>false`).
- Fake hash anti-enumeración + timing <10s.
- Lockout 5→15min persistente (opción b); `CacheService` intacto.
- Policies solo documentadas (implementa `04-04`); rehash-escritura diferida.
- Reintento único ante `DbUpdateConcurrencyException` (fail-closed).

3. Pruebas

- `UnitTest/Login/PasswordHasherTests.cs` (5), `UnitTest/Login/LoginServiceTests.cs` (11).
- `SecurityTest/Login/AntiEnumerationTests.cs` (3), `SecurityTest/Cache/NoLeakTests.cs` (4).
- `IntegrationTest --filter Login` (8), `ContractTest` (4).

4. Secuencia (ejecutada)

1. Hasher + packages + DI + unit tests. 2. Lockout persistente + migración + `LoginService` + tests. 3. Anti-enumeración + no-leak. 4. Docs + gates.
