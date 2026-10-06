# plan.md — 03-06-login (conciliado con lo ejecutado 06-Oct-2026)

1. Mapeo

- Crear: `Controllers/V1/LoginController.cs`, `Services/LoginService.cs`
  (`ILoginService` + `LoginResult` + `LoginStatus`), `Dtos/LoginDtos.cs`
  (`LoginRequest`/`LoginResponse`), `Validators/LoginRequestValidator.cs`,
  `stryker-0306.json`.
- Modificar: Program.cs (solo DI de `ILoginService` + validador; sin `LoginPolicy`:
  la ruta `POST api/v{version}/auth/login` es anónima).
- Crear tests: `UnitTest/Login/LoginServiceTests.cs` (11),
  `IntegrationTest/Login/LoginControllerTests.cs` (4),
  `SecurityTest/Login/LoginSecurityTests.cs` (2).

2. Guardarraíles

- Anti-enumeration: 401 genérico idéntico + verify fake contra hash dummy.
- Lockout 5 intentos en caché (`attempts:/lockout:`, TTL 120s = máximo de
  `CacheService`; 15 min reales en `04-02`).
- Password nunca en llaves de caché ni logs.
- Sin `UseRateLimiter` en este slice (429 en `04-04`); sin JWT real (token opaco,
  `04-01`); sin rehash (interfaz sin `NeedsRehash`, `04-02`).

3. Pruebas

- UnitTest/Login/LoginServiceTests.cs (11, Stryker 100%).
- IntegrationTest/Login/LoginControllerTests.cs (4: 200/401-idénticos/423/400).
- SecurityTest/Login/LoginSecurityTests.cs (2: 401 sin fugas, 400).

4. Secuencia (ejecutada)

1. DTOs + validador → 2. Servicio → 3. Controller + DI → 4. Unit →
   Integration → Security → 5. build Release → Stryker (80.65% → 100%) →
   6. `dotnet build` post-Stryker + suites completas + Memoria.md.
   (Rate limiting no se implementó: difiere a `04-04`.)
