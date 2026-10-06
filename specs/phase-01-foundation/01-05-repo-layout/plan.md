# plan.md — 01-05-repo-layout

1. Mapeo

- Crear: carpetas deploy/, scripts/, .semgrep/, fuzzing/.
- Modificar: .gitignore, .dockerignore.

2. Decisiones

- Estructura: API + 7 proyectos de test + ChaosTest + fuzzing + deploy + scripts + .github/ + .semgrep/.

3. Guardarraíles

- .gitignore defensivo: reports/, StrykerOutput/, *.user, secrets.
- .dockerignore: excluir bin/obj, pruebas, docs.

4. Pruebas

- Build: estructura compila.

5. Secuencia

1. Crear carpetas.
2. Ajustar gitignore/dockerignore.
