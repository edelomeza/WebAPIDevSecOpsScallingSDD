# plan.md — 09-01-ci

1. Mapeo

- Modificar: .github/workflows/ci-cd.yml.

2. Decisiones

- Orden: restore→build→unit→integration→security→database→contract→mutation(nightly)→performance(nightly opcional)→chaos(nightly).

3. Guardarraíles

- Tests con --no-build.
- Nightly mutation 180min y chaos.

4. Pruebas

- CI: pipeline verde.

5. Secuencia

1. Jobs base.
2. Nightly jobs.
3. Artifacts.
