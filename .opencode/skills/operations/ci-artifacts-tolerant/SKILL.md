---
name: ci-artifacts-tolerant
description: Agregar artifacts de CI de forma robusta y tolerante
---

## Propósito

Agregar artifacts de forma robusta.

## Pasos

Majors en pareja; `needs`+`if: always()`+`continue-on-error`+`if-no-files-found: warn`; parsers defensivos.

## Checklist

Fixtures validan dockle/Trivy/ZAP/Semgrep; `python -c "import yaml..."`.

## Referencias

`ci-cd.yml`, `scripts/hardening_summary.py`.
