# EX-08B evidence — shared playtest preparation

[Preparation report](../../EX08B_PREPARATION_REPORT.md), [current guide](../../EX08B_SESSION_GUIDE.md) and [blank record](../../EX08B_SESSION_RECORD.md) supply scope and handoff. New human session: **NOT RUN**.

[build-manifest.json](build-manifest.json) fingerprints [source.zip](source.zip), [playtest-build.zip](playtest-build.zip) and all 171 source/nine served entries. [serve.py](serve.py) verifies and serves the archive on loopback port 5177. [prepare.ts](prepare.ts) records fresh and normal-command pre-warning starts in the campaign subdirectory; [settings.json](settings.json) declares exact state and assistance. [freeze.py](freeze.py) refuses an existing capture.

Fresh evidence: [51-test log](focused.log), [start preparation](prepare.log), [final browser log](browser-final.log), [browser measurements](browser-result.json), [live warning](warning-final.log), [warning result](warning-result.json), [docsync](docsync.log), [freshness](freshness.log). Screenshots cover both starts at both viewports and the natural warning. Initial browser/warning scripts and logs remain separate failed coverage attempts; their real timing measurements remain reported, not discarded.

[check.py](check.py) checks archives/source, old evidence, new references and blank human/task status. [verification-manifest.json](verification-manifest.json) is captured once and [consistency.log](consistency.log) records the result. Prior P6-04 full-suite/build/lint/snapshot checks are retained on identical source. Browser tests are headless automated checks, not owner play. Compact fresh-start interaction stalls and reference-machine validation remain open; the ammo report remains unconfirmed pending retest.
