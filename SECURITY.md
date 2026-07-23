# Security Policy

## Reporting a vulnerability

Please report security issues **privately**, not through public issues.

- Open a [GitHub private security advisory](https://github.com/ahmet191/WerkPilot/security/advisories/new), or
- email the maintainer at **ahmetcemaleddin@gmail.com** with details and, if
  possible, reproduction steps.

Please allow reasonable time for a fix before any public disclosure.

## Scope notes

- **Known item — Telegram bot token in git history.** An early commit contains a
  low-value demo Telegram bot token. It is not present in the current code
  (`Telegram:BotToken` ships empty) and the application runs fully without it.
  CI secret scanning (gitleaks) runs against the **working tree**, so it gates
  new secrets without re-flagging this historical one. If the history matters for
  your use, purge it with `git-filter-repo` and rotate the token via BotFather.

- **Secrets** belong in environment variables or .NET user-secrets, never in
  committed files. `appsettings.json` holds only empty placeholders.

## Automated checks

Every push runs, in addition to build + tests:

- **Trivy** — dependency (CVE) and secret scanning, gating on fixable HIGH/CRITICAL.
- **CodeQL** — static analysis (SAST) for C# and TypeScript.
- **gitleaks** — working-tree secret scan.
- **Dependabot** — weekly dependency and GitHub Actions updates.
