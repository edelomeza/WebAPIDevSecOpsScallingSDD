# 04-02 — Protección de credenciales

## Contexto
Hashing, anti-enumeración y lockout de credenciales. **Depende de**: `01-02`, `05-01`. Delta de endurecimiento sobre `03-05/03-06` (dueños del fake SHA256 y del lockout-120s en caché): los reescribe sin recrear slices.

## Requisitos
1. `Argon2IdSegUsuarioPasswordHasher` Argon2id (64MB/3 iter) con fallback BCrypt solo migración; `NeedsRehash()` como capacidad (escritura diferida, ver Límites).
2. Fake hash anti-enumeración en login fallido (401 idéntico + timing acotado).
3. Lockout tras 5 intentos (15 min) persistente en DB, fuera de la caché efímera.
4. Password nunca en caché ni logs.

## Diseño
- Hasher: `Konscious.Security.Cryptography.Argon2 1.3.1` + `BCrypt.Net-Next 4.0.3` (solo `Verify $2a$/$2b$`); formato PHC `$argon2id$v=19$m,t,p$salt$hash`; `DegreeOfParallelism=Min(4, ProcessorCount)` como constante interna (no umbral); `PasswordHasherOptions` (`PasswordHasher:` sin secretos); DI factory en `Program.cs`; `Fake` intacto con `NeedsRehash=>false`.
- Lockout: tabla `SegBloqueo` (`strNombre` único, `intIntentosFallidos`, `dteBloqueoHasta`, `RowVersion`, migración `SegBloqueo0402`); `ILoginLockoutStore`/`EfLoginLockoutStore` con `TimeProvider` (`System` en prod, fake en tests); 1 reintento ante `DbUpdateConcurrencyException` (fail-closed); rearme limpio tras expiración; `CacheService` 30–120s intacto. Solo `LoginService`; `Login2Fa/TwoFactor`/blacklist siguen en caché (deuda).
- Policies de rate limit asociadas (solo documentadas, implementa `04-04`): Global 1000/min, Login 5/5min, Login2faVerify 10/5min, Admin 200/min, ConcurrentWrites 10.

## Contratos
- Interno: `ISegUsuarioPasswordHasher.{Hash,Verify,NeedsRehash}`; `ILoginLockoutStore.{IsLocked,RecordFailure,ResetAsync}`; `SegBloqueo` + `PasswordHasher:` (sin secretos). Sin endpoints nuevos.

## Tests
- `UnitTest/Login/PasswordHasherTests.cs` (10), `UnitTest/Login/LoginServiceTests.cs` (14, `TimeProvider` fake), `UnitTest/Login/LoginLockoutStoreTests.cs` (6, `FlakyDbContext`), `SecurityTest/Login/AntiEnumerationTests.cs` (3), `SecurityTest/Cache/NoLeakTests.cs` (4 casos).

## Criterios
- `dotnet build -c Release --no-restore` → 0 errores; `UnitTest` 316/316; `SecurityTest` 74/74; `IntegrationTest` 92/99 + 7 Docker-only excluidos (sin daemon local, igual que `04-01`); `ContractTest` 4/4 (09-Oct-2026).
- Stryker local `stryker-0402.json` (UnitTest, `test-case-filter`, sin Docker): **90.85%** (142 mutantes; 13 equivalentes documentados: guardas con misma excepción aguas abajo, bloques fail-closed convergentes, guarda `<=0` convergente, estado `(0, no-null)` inalcanzable).
- Login malo no revela usuario (mismo `401`+body, delta timing <10s); 5×401 → 6º `423`; expiración real 15 min (+14:59 bloquea, +15:01 libera).
- `critic-guardrails` PASS; `check_endpoints` OK (56 rutas); `Select-String` de secretos en servicios → 0 hits.

## Límites
- Sin rehash-escritura: `NeedsRehash` existe pero `LoginService` no persiste (lee `AsNoTracking`, `RowVersion`); followup con diseño de concurrencia.
- Sin Argon2id débil; sin secretos en repo. `strPWD nvarchar(200)` basta (PHC 85–120 chars con salt 16B); salt >32B o metadata extensa ⇒ migración a 256.
- Temp-2FA 5min y blacklist `RefreshTokenService` siguen en TTL 120s (deuda explícita). Datos seed/dev con hash placeholder o fake requieren reset de password (verify fail-closed ante formato desconocido).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador con evidencia (ver Criterios)
- **Revisores:** —
- **Fecha:** 09-Oct-2026
- **Detalle:** implementado según plan; pendiente 1 revisor + `@security-reviewer` pre-push.
