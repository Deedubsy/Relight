# Always dark stage 1 (L-01) — handoff (closed)

Closed 2026-09-20. Stage 1 was reviewed, fixed and merged to `main` at `25fc4890`, and pushed. `Unity/Docs/TASKS.md` owns what remains: the owner's checks (L-ACC) and stage 2 (L-02). The evidence, both reviews' findings and the leftovers not yet fixed are in `Unity/Docs/evidence/always-dark-stage-1/checks.md`.

Two things worth knowing before the next Play Mode work:

- Every Play Mode stop and every PlayMode test run rewrites the owner's auto-quit save under `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Relight\saves\exploration-v2\auto`. Stop that first (one save-root helper with a test override). A copy of the saves as they were on 2026-09-20 is at `...\Relight\saves-backup-2026-09-20`.
- The `screenshot` command and `capture_game_view` with the default `source=camera` leave out the UI. Use `capture_game_view --source screen` in Play Mode, with a `save_path` inside the project (it lands under `Assets/`, so move it out and delete the `.meta`).
