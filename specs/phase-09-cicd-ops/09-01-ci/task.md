# 09-01 — CI

## T1 — Pipeline CI
- **Modificar**: `.github/workflows/ci-cd.yml`.
- **Verificar**: orden restore→build→unit→integration→security→database→contract→mutation(nightly)→performance(nightly)→chaos(nightly); `--no-build`; nightly mutation 180min y chaos.
