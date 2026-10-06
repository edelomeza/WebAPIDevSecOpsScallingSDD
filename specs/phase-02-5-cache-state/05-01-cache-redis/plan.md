# plan.md — 05-01-cache-redis

1. Mapeo

- Modificar: Program.cs (AddStackExchangeRedisCache tuned).
- Crear: WebAPIDevSecOps/Services/CacheService.cs.

2. Decisiones

- Claves blacklist:{jti}, attempts:{user}, lockout:{user}, cache:* con TTL 30–120s.
- Fallback IMemoryCache; multiplexer tuned.

3. Guardarraíles

- Nunca cachear password.
- /health debe reflejar caída de Redis; /health/ready 200.

4. Pruebas

- IntegrationTest/Cache/CacheFallbackTests.cs.
- SecurityTest/Cache/LockoutTests.cs.

5. Secuencia

1. Config multiplexer.
2. Cache-aside.
3. Fallback.
