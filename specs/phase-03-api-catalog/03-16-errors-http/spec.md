# 03-16 — Errores HTTP

## Contexto
Manejo uniforme de excepciones sin try/catch ad-hoc. **Depende de**: `01-02`.

## Requisitos
1. Crear `ExceptionHandlingMiddleware` con mapeo: `ForbiddenAccessException`→403, no encontrado→404, concurrencia→409, validación→400/422, timeout→408, resto→500.
2. Cuerpo de error uniforme en PascalCase.
3. Eliminar try/catch manuales de controllers (p. ej. `ConcurrencyConflictException`).

## Diseño
- Middleware registrado al inicio del pipeline (tras página de excepciones de Dev); respeta el orden de `01-02`.

## Contratos
- Forma de error uniforme para todos los endpoints.

## Tests
- `IntegrationTest/Errors/ErrorHandlingTests.cs`.

## Criterios
- Sin try/catch ad-hoc; cuerpo uniforme.

## Límites
- Sin trazas internas en prod (cuerpo genérico).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
