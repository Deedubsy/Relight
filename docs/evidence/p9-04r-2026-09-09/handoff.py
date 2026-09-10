from pathlib import Path
import json
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
s=json.loads((out/'population/summary.json').read_text(encoding='utf-8'))
assert s['complete'] and s['passed']==10000 and s['failed']==0 and (root/'docs/P9_04R_REPAIR_REPORT.md').exists()
for name in ['CLAUDE.md','README.md','docs/PROGRAMME_STATE.md','docs/PROGRESS.md','docs/PHASES.md','docs/EXPLORATION_DEFENCE_PLAN.md','docs/STANDARDS.md','docs/RELIGHT-design.md']:
 p=root/name;text=p.read_text(encoding='utf-8')
 text=text.replace('P9-04R is next: repair the failures measured by P9-04 before P9-05.','P9-05 is next: city engineering review and human preparation.')
 text=text.replace('P9-04R is the next implementation task, repairing the P9-04 population failures before P9-05.','P9-04R is complete with all 10,000 seeds passing; P9-05 is next for city engineering review and human preparation.')
 text=text.replace('**Next implementation: P9-04R — population failure repairs.**','**Next implementation: P9-05 — city engineering review and human preparation.**')
 text=text.replace('P9-04R repairs those findings before P9-05 engineering/human preparation.','[P9-04R](P9_04R_REPAIR_REPORT.md) repairs all 258 findings and passes the separate 10,000-seed comparison; P9-05 engineering/human preparation is next.')
 # CLAUDE/README use root-relative prose, so keep this replacement free of a relative report link.
 text=text.replace('P9-04R repairs those findings before P9-05.','P9-04R now repairs all 258 findings and passes the separate 10,000-seed comparison. P9-05 is next.')
 if name=='docs/PROGRAMME_STATE.md':
  text=text.replace('Current handoff: 2026-09-08.','Current handoff: 2026-09-09.')
  text=text.replace('P9-01/P9-02/P9-03 implementation and P9-04 population evidence/tooling','P9-01/P9-02/P9-03 implementation, P9-04 population evidence/tooling and P9-04R repairs')
  text=text.replace('Gameplay/config/build remain byte-identical to P9-03. P9-04R repairs generation/survey findings before P9-05; human gates remain open.',"At that checkpoint, gameplay/config/build were byte-identical to P9-03. P9-04R now resolves those generation/survey findings; the failed baseline remains intact and human gates remain open.")
  text=text.replace('## Verified work and remaining observations\n','## Verified work and remaining observations\n\n**P9-04R verified:** revision-3 composed surveys pass 10,000/10,000 seeds, repairing every one of the 258 P9-04 failures with no paired regression. 351 full + 24 focused tests, 12 paid freight/truck routes, 36 extraction probes, 78 campaign experiment checks and eight browser cases pass. Older layouts, hashes and historical logs survive resaves with honest replay provenance. [Report](P9_04R_REPAIR_REPORT.md). Final source/build archives are retained; the current comparison uses fingerprint `ba11e896`. P9-05 is next; human play and performance remain separate.\n')
 if name=='docs/PROGRESS.md':
  text=text.replace('**Next implementation (2026-09-08, D-EX-50):** P9-04R, addressing the measured population failures before P9-05.','**Next implementation (2026-09-09, D-EX-51):** P9-05, city engineering review and human preparation after the passing P9-04R repair comparison.')
  text=text.replace('| P9-04R | Repair demonstrated campaign generation and survey failures | agent | in_progress |','| P9-04R | Repair demonstrated campaign generation and survey failures | agent | done |')
  text=text.replace('| pending — repair and population comparison report | |','| docs/P9_04R_REPAIR_REPORT.md — 10000/10000 seeds; all 258 old failures repaired; 351 full + 24 focused tests, 12 paid routes, 36 extraction probes, 78 experiment checks, 8 browser cases; old evidence/layouts/logs preserved | 2026-09-09 |')
  text=text.replace('| P9-05 | City engineering review and human preparation | agent | blocked |','| P9-05 | City engineering review and human preparation | agent | todo |')
  text+='\n- 2026-09-09 — P9-04R done under D-EX-51: revision-3 composed surveys pass the separate complete 10,000-seed comparison; all 258 baseline failures repaired, no paired regression. 351 full + 24 focused tests, 12 ordinary freight/truck routes, 36 extraction probes, 78 refreshed campaign experiment checks and eight browser save/reload cases pass. Old saved layouts/hashes and historical logs remain; old population/evidence is preserved. P9-05 blocked → todo; human gates and RI-02B sequencing unchanged. No commit or push.\n'
 p.write_text(text,encoding='utf-8',newline='\n')
p=root/'docs/DECISIONS.md';text=p.read_text(encoding='utf-8');assert '### D-EX-51 ' not in text
text+="""

### D-EX-51 — P9-04R composed survey repair and retained comparison (2026-09-09)

**Authority:** owner (current user), “Ok continue with p9-04r”. Implement the measured repair follow-up defined by D-EX-50 and PROGRESS; preserve old evidence, saved layouts and logs.

**Implementation choices (agent):** fresh survey revision 3 accepts initial/later route and stop arrangements only after complete district source/workshop/cache/recruit/Turbine composition succeeds. Reject rubble throughout stop footprints; use actual district-owned facility tiles and search beyond a preferred pad before rejecting the district. Stable candidate ordering uses the original seed and unchanged base city. Candidate metadata/events/revision are discarded on placement failure; existing safety distances, rewards, costs and validator rules remain unchanged. Older saved surveys are not rerouted, and incomplete historical session logs survive resaving without a fresh-factory replay promise.

**Evidence:** the separate complete 1–10,000 comparison passes every seed: 258 failed → passed, 9742 passed → passed, no base-generator retry-index changes. The failed P9-04 baseline remains unchanged. 351 full + 24 focused tests, 12 paid freight/truck probes, 36 powered extraction probes, 78 campaign experiment checks and eight browser save/reload cases pass. [Report](P9_04R_REPAIR_REPORT.md) retains preliminary failures, source provenance and test scope.

**Handoff:** P9-05 is runnable for engineering review and human preparation. RI-02B remains after P9-05. Human ten-seed/shared play, P6-AMMO, reference performance and Q07 remain separate; no phase verdict, commit or push is inferred.
"""
p.write_text(text,encoding='utf-8',newline='\n');print('P9-04R marked done; P9-05 runnable; human gates remain open.')
