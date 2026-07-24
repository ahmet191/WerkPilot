# Security Policy

## Reporting a vulnerability

Please report security issues **privately**, not through public issues.

- Open a [GitHub private security advisory](https://github.com/ahmet191/WerkPilot/security/advisories/new), or
- email the maintainer at **ahmetcemaleddin@gmail.com** with details and, if
  possible, reproduction steps.

Please allow reasonable time for a fix before any public disclosure.

## Scope notes

- **Credentials & Secrets.** The repository contains zero committed credentials in active files or Git history. Secrets belong in environment variables or .NET user-secrets, never in committed code. `appsettings.json` carries only empty placeholders and safe local development defaults.


## Automated checks

Every push runs, in addition to build + tests:

- **Trivy** — dependency (CVE) and secret scanning, gating on fixable HIGH/CRITICAL.
- **gitleaks** — secret scan of the committed files at HEAD.
- **Dependabot** — weekly dependency and GitHub Actions updates.

**CodeQL (SAST)** is not wired as a workflow: code scanning result upload requires
a public repository or GitHub Advanced Security. Once this repository is public,
enable CodeQL via **Settings → Code security → Code scanning → CodeQL → Default
setup** (one click, self-maintaining) rather than a custom workflow.
