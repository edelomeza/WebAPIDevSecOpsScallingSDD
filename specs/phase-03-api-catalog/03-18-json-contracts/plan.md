# plan.md — 03-18-json-contracts

## 1. Mapeo

- Crear: `ContractTest/Fixtures/*.json` (fuente única; `IntegrationTest/Common/` no existe y no se crea).
- Crear: `FixtureCaptureTests.cs`, `JsonContractTests.cs`, `FixturePaths.cs`, `Common/ContractAuthHandler.cs`, `xunit.runner.json`; cablear `ContractTest.csproj` (Mvc.Testing + TestHost + ProjectReference API + `NoWarn CA1515`).
- Tocar: `spec.md`, `task.md`, skill `testing-pact`, `Memoria.md`.

## 2. Decisiones

- Fixtures + test ahora; PactNet y provider verification en fase 10 (sin paquete `Pact*` hoy).
- Captura real (wire vía `WebApplicationFactory` `Staging`, InMemory, sin Docker) en vez de JSON a mano; escritura gateada por `CONTRACT_CAPTURE=1`.
- Convención de nombres medida, no "PascalCase estricto": prefijos legacy + `id`/sufijo + resto PascalCase (`IsConventional`).
- Refresh vía `IRefreshTokenService.CreateAsync` scoped (el login no devuelve refresh).
- `Login2FaVerifyResponse` diferido (TOTP real, sin fake determinista).

## 3. Guardarraíles

- xUnit1030: sin `ConfigureAwait(false)` en cuerpos `[Fact]` (solo helpers); CA1515: `NoWarn` en csproj (precedente `UnitTest`); CA2000: pragma en factory (precedente `IntegrationTest`).
- Analizadores que mordieron aquí: CA1515, xUnit1030 (ver `testing/analyzer-quickref` si se amplía).

## 4. Pruebas

- `ContractTest` 4/4: captura (status del flujo) + existencia + convención + sin secretos.

## 5. Secuencia

1. Cablear csproj + auth handler + paths.
2. Capture test → correr con `CONTRACT_CAPTURE=1` → 14 JSON.
3. Test de convención (iterar hasta codificar la convención real).
4. Conciliar spec/task + skill + Memoria; critic verde.
