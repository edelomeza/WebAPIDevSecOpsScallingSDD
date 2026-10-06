# plan.md — 04-03-http-hardening

1. Mapeo

- Modificar: Middleware/SecurityHeadersMiddleware.cs, Middleware/CspNonceMiddleware.cs.

2. Guardarraíles

- Headers presentes; CSP nonce; HSTS prod; CORS single origin; assembly integrity; object-level auth → 403.

3. Pruebas

- SecurityTest/Headers/HeaderTests.cs.

4. Secuencia

1. Headers.
2. CSP nonce.
3. Assembly integrity.
4. Tests.
