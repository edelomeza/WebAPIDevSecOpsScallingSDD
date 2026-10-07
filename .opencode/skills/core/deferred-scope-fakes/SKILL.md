---
name: deferred-scope-fakes
description: Alcance fake-first con NOTEs trazables para diferir bus, JWT y rate-limit sin bloquear slices
---

## Propósito

Avanzar slices (`03-06`…`03-12` probado) sin esperar fases pesadas (bus MassTransit, JWT HS256, rate-limit), dejando fakes reemplazables y pendientes trazables.

## Cuándo usarla

Al acotar un slice cuyo alcance completo depende de specs no ejecutadas (`06-01`, `04-01`, `04-04`, `03-09`…).

## Precondiciones

Acuerdo de alcance mínimo con el usuario + spec dueña del reemplazo identificada.

## Pasos

1. Diseñar interfaz reemplazable (`IPedidoEventPublisher`, `ISegUsuarioPasswordHasher`, `ITotpService`): el slice consume la interfaz; el fake vive en producción (`Fake*`, DI `AddScoped`) para que los tests de integración lo usen.
2. Marcar cada diferido con `NOTE (XX-YY)` donde `XX-YY` = spec dueña (`NOTE (06-01)`, `NOTE (04-02)`…). NUNCA `TODO`: S1135 lo convierte en error de build (`TreatWarningsAsErrors`).
3. Registrar en las 4 capas: `NOTE` en código + `Límites`/`Desviaciones` en `spec.md` + `Pendiente → XX-YY` en `task.md` + entrada en `Memoria.md`.
4. Tests que fijan el contrato del fake (publica 1 vez / no publica en fallo; semilla dummy observable vía `Recording*` doubles) para que la fase dueña los rompa a propósito al reemplazar.
5. Al llegar a la fase dueña: `grep NOTE (XX-YY)`, swap de implementación, re-correr Stryker + build restaurativo del slice tocado.

## Checklist

- Fake tras interfaz + `NOTE` con spec dueña; cero `TODO` en el diff.
- Las 4 capas de bitácora actualizadas; desviaciones aceptadas por el usuario.

## Criterios de done

Slice cierra en `🚧 Borrador con evidencia` en su alcance; nada queda pendiente sin dueño.

## Límites/trampas

- No fakear lógica de negocio del slice (solo infraestructura futura: bus, hash, JWT, rate-limit, TOTP real).
- Secretos/passwords jamás en llaves/logs/DTOs ni siquiera en fakes.
- Precedentes: `FakeSegUsuarioPasswordHasher`+`NOTE (04-02)`, token opaco+`NOTE (04-01)`, `FakeTotpService`+`NOTE (03-09)`, `FakePedidoEventPublisher`+`NOTE (06-01)`.

## Referencias

`03-05`, `03-06`, `03-07`, `03-12`, `Memoria.md`, `00-04-lessons-learned/spec.md`.
