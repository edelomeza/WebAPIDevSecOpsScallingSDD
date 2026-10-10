# 04-02 — Credential Protection (tareas, delta sobre 03-05/03-06 ✅)

> Base ya hecha en `03-05/03-06`: `ISegUsuarioPasswordHasher` + `FakeSegUsuarioPasswordHasher` (SHA256+sal fija) + `LoginService` con `_dummyHash`/401 genérico. Ejecutado 09-Oct-2026 (ver **Ejecutado** en cada T). Skills: `phases/04-security-core`, `core/deferred-scope-fakes`, `operations/critic-guardrails`, `operations/drift-guards`.

## T1 — Hasher Argon2id + `NeedsRehash` sin escritura (Req 1)
- **Modificar**: `Services/SegUsuarioPasswordHasher.cs` — interfaz `+= bool NeedsRehash(string)`; `Fake` añade `=>false` (intacto, lo usan ~30 tests); nuevo `Argon2IdSegUsuarioPasswordHasher` (S101 exige `Id`): `Konscious.Security.Cryptography.Argon2 1.3.1` Argon2id 64MB/3iter + `DegreeOfParallelism` como constante interna `Min(4, ProcessorCount)` (no umbral de spec); formato PHC `$argon2id$v=19$m,t,p$salt$hash`; `BCrypt.Net-Next 4.0.3` solo `Verify $2a$/$2b$`; sin rehash-escritura (`LoginService` lee `AsNoTracking` + `RowVersion`, ver Límites).
- **Crear**: `Services/PasswordHasherOptions.cs` (`MemoryKBytes=65536`, `Iterations=3`, bind `PasswordHasher:` en `appsettings.Example.json` sin secretos) + `UnitTest/Login/PasswordHasherTests.cs` (10: OK/KO, BCrypt legacy, fresco/desconocido, nulos, Fake-nulos, coste-cero, opciones custom, `p=` fijado, batería 14 malformados).
- **Modificar**: `Program.cs` — DI factory con `PasswordHasher:` + `nuget.config` patterns `Konscious.*`/`BCrypt.Net*`.
- **No tocar**: `SegUsuarioService.cs`, `Dtos/`, `docs/endpoints.md` (canónico, no duplicar).
- **Ejecutado 09-Oct-2026**: `UnitTest --filter PasswordHasher` 10/10.
- **Verificar**:
  - `dotnet build -c Release --no-restore` → 0 errores.
  - `dotnet test UnitTest -c Release --no-build --filter PasswordHasher` → 5/5.
  - `powershell -File scripts/critic-guardrails.ps1` → `PASS` exit 0.

## T2 — Anti-enumeración con timing real (Req 2)
- **Modificar**: nada funcional; conservar `_dummyHash` + `Verify` en rama `user is null` + `401` idéntico.
- **Crear**: `SecurityTest/Login/AntiEnumerationTests.cs` (3: dos desconocidos → mismo `401`+mismo body con delta timing <10s; body sin `password/strPWD/argon2`; 5×401 + 6º→423 en desconocido).
- **No tocar**: lockout (T3), JWT/temp opaco (`NOTE 04-01`).
- **Ejecutado 09-Oct-2026**: `SecurityTest --filter AntiEnumeration` 3/3.
- **Verificar**: `dotnet test SecurityTest -c Release --no-build --filter AntiEnumeration` → 3/3.

## T3 — Lockout 5 intentos → 15 min persistente (Req 3, opción b)
- **Decisión**: opción (b) — `SegBloqueo` en DB (`strNombre` único, `intIntentosFallidos`, `dteBloqueoHasta`, `RowVersion`); `CacheService` 30–120s intacto (sin tocar `05-01`). Cubre usuarios inexistentes (la fila vive por nombre, no por `SegUsuario`).
- **Crear**: `Models/SegBloqueo.cs` + `DbSet` + índice único + migración `SegBloqueo0402` + `Services/LoginLockoutStore.cs` (`ILoginLockoutStore`/`EfLoginLockoutStore` con `TimeProvider`; 1 reintento ante `DbUpdateConcurrencyException`, fail-closed; rearme tras expiración sin castigo acumulado).
- **Modificar**: `Services/LoginService.cs` — lockout por store (1–5→401, 6º→423, `ResetAsync` en éxito); `Program.cs` (`TimeProvider.System` + store scoped). `Login2Fa/TwoFactor/Refresh-blacklist` quedan en caché como deuda explícita.
- **Modificar tests**: `UnitTest/Login/LoginServiceTests.cs` (14: expiración +14:59/+15:01 y borde exacto, rearme sin recastigo, re-bloqueo tras 5 más; `SegBloqueo` en vez de llaves `lockout:`; sin `lockout:/attempts:` en caché) + ctor de 4 args en `Login2fa/*Tests.cs`.
- **Ejecutado 09-Oct-2026**: `UnitTest --filter Login` 52/52; `IntegrationTest --filter Login` 8/8.
- **Verificar**:
  - `dotnet test UnitTest -c Release --no-build --filter Login` → verde.
  - `dotnet test IntegrationTest -c Release --no-build --filter Login` → verde.
  - Nota perf: Argon2id colapsa 2 vCPU (`testing-nbomber`); `PERF_*` relajado solo en test, nunca en prod.

## T4 — Password nunca en caché ni logs (Req 4)
- **Modificar**: nada funcional; `CacheService.ForbiddenPatterns={password,secret,token}` vigente; hasher sin logging de plano/salt; `strPWD` fuera de DTOs lectura/autocomplete (ya cumplido en `03-05`).
- **Crear**: `SecurityTest/Cache/NoLeakTests.cs` (`Theory` 3 patrones vía `ICacheService` de DI → `InvalidOperationException` + `Fact` higiene del hash: sin plaintext, salt aleatoria).
- **Ejecutado 09-Oct-2026**: `SecurityTest --filter NoLeak` 4/4; `Select-String 'strPWD|strPasswordPlano|str2FASecreto'` en `CacheService/SegUsuarioPasswordHasher/LoginLockoutStore` → 0 hits.
- **Verificar**:
  - `dotnet test SecurityTest -c Release --no-build` → 74/74.
  - `powershell -File scripts/critic-guardrails.ps1` → `PASS` exit 0.
  - `powershell -File scripts/check_endpoints.ps1` → `OK` exit 0 (56 rutas, sin rutas nuevas).
  - `@security-reviewer` pre-push verde (`edit: deny`); Stryker local `stryker-0402.json` (UnitTest + `test-case-filter`, sin Docker) → **90.85%** (142 mutantes, 13 equivalentes documentados) + `dotnet build` restaurativo posterior.
- **Pendiente →** rehash-escritura con `RowVersion` (followup), temp-2FA 5min + blacklist `RefreshTokenService:49` (TTL), `04-04` (policies + `429`; `spec.md Diseño` solo las documenta), `05-01` (Redis real).
