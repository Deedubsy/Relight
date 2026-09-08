# P6-01 evidence — 2026-09-07

See [the implementation report](../../P6_01_RELIABILITY_REPORT.md) for scope, fixtures, failures and limits.

Final validation: `tests-final.log` (243/243), `typecheck-final.log`, `lint.log`, `snapshot.log`, `docsync.log`, `freshness.log` and `consistency.log`. Earlier focused/build logs retain their original results. `verify.sh` uses the established WSL Ubuntu-24.04 / Node 22.18.0 runtime and refuses to overwrite an existing named log.

`check.py --capture` captures `build-manifest.json`, `source.zip` and `build.zip` once. Subsequent `check.py` runs verify current inputs, archive contents, historical evidence and the documented next task. The parent commit is `17939145b5d40afe4dcfc1937a3ec6d34b763df6`; the manifest identifies the uncommitted tested source/build precisely.

This directory is engineering evidence, not a new human playtest or Phase 6 exit verdict. The prior EX/P5 archives and review probes remain historical; their scripts are not rewritten to assert the new behaviour.
