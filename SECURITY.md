# Security Policy

## Reporting a vulnerability

Please report security issues **privately**, not through public issues.

- Open a [GitHub private security advisory](https://github.com/ahmet191/WerkPilot/security/advisories/new), or
- email the maintainer at **ahmetcemaleddin@gmail.com** with details and, if
  possible, reproduction steps.

Please allow reasonable time for a fix before any public disclosure.

## Scope notes

- **Git history may contain credentials from earlier development.** CI secret
  scanning (gitleaks) runs against the **committed files at HEAD** — a clean
  export with no `.git` — so it gates secrets in the current code deterministically,
  independent of checkout depth. History is not automatically scrubbed. Before the
  repository is made public (or now, if it already is), **rotate any real
  credential that was ever committed** — database passwords, connection strings,
  API tokens — and purge them from history with `git-filter-repo` if the exposure
  matters. The current code carries only empty placeholders and local-dev defaults.

- **Secrets** belong in environment variables or .NET user-secrets, never in
  committed files. `appsettings.json` holds only empty placeholders.

## Automated checks

Every push runs, in addition to build + tests:

- **Trivy** — dependency (CVE) and secret scanning, gating on fixable HIGH/CRITICAL.
- **CodeQL** — static analysis (SAST) for C# and TypeScript.
- **gitleaks** — working-tree secret scan.
- **Dependabot** — weekly dependency and GitHub Actions updates.
