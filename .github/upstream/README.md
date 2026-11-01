# Parked upstream automation

These files come from the Nexus Mods App repository. They are kept here, unchanged, so GitHub does **not** run them in Metal Mod Manager's repository. GitHub only runs workflows in `.github/workflows` and only reads `.github/dependabot.yml` and `.github/ISSUE_TEMPLATE`.

They were parked because they:
- need Nexus Mods secrets (code signing, NuGet, Discord webhooks, a Nexus API key)
- call reusable workflows in Nexus Mods' `NexusMods.App.Meta` repository, pinned to `@main`
- request reviews from Nexus Mods teams, or link to Nexus Mods documentation
- open dependency-update PRs, including for MnemonicDB, which this fork keeps frozen

Move a file back, and adapt it, when Metal Mod Manager needs it. The fork's own CI is `.github/workflows/ci.yml`.
