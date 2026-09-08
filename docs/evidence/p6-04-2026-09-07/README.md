# P6-04 review evidence — 2026-09-07

[Readiness report](../../P6_04_READINESS_REPORT.md) records the assessment and owner-authorised ammo retest deferral. This directory contains fresh final-code [tests](tests.log), [typecheck/build](typecheck.log), [lint](lint.log), [legacy snapshot](snapshot.log), [docsync](docsync.log) and [freshness](freshness.log). [verify.sh](verify.sh) refuses to overwrite a named log.

[check.py](check.py) verifies 171 unchanged source/test/build inputs, six byte-identical production files, the source/build archive entries, nine retained workload results, sixteen retained ammo cases, historical hashes, local references and task order. [review-manifest.json](review-manifest.json) captures the review once; [consistency.log](consistency.log) records the result. Run `python check.py --capture` only for the initial capture, then `python check.py` for verification.

The independent implementation archives remain the [P6-03 source](../p6-03-2026-09-07/source.zip) and [P6-03 build](../p6-03-2026-09-07/build.zip), whose hashes and entries are checked. No duplicate shared source/build/start freeze is made: EX-08B owns that preparation. Earlier workload/browser/performance data are retained evidence, not newly executed experiments. The ammo report remains unresolved, no new human verdict is recorded, and performance/reference-machine findings remain open.
