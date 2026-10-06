# plan.md — 07-01-static-analysis

1. Mapeo

- Modificar: Directory.Build.props, .editorconfig, .semgrep/semgrep.yaml, CI.

2. Guardarraíles

- Semgrep --error.
- SonarAnalyzer CA3000+ severity error.
- Complejidad ≤10 (S1541), ≤15 (S3776).

3. Pruebas

- CI: job Semgrep y SonarCloud.

4. Secuencia

1. Semgrep rules.
2. Editorconfig.
3. Directory.Build.props.
4. CI jobs.
