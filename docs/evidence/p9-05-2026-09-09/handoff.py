from pathlib import Path
names=['CLAUDE.md','README.md','docs/PROGRAMME_STATE.md','docs/PROGRESS.md','docs/PHASES.md','docs/STANDARDS.md','docs/EXPLORATION_DEFENCE_PLAN.md']
for n in names:
 p=Path(n);s=p.read_text(encoding='utf-8')
 s=s.replace('P9-04R is complete with all 10,000 seeds passing; P9-05 is next for city engineering review and human preparation.','P9-05 engineering and human preparation are complete on frozen port 5181; RI-02B-UI-01 is next. The repaired 10,000-seed population remains fully passing.')
 s=s.replace('P9-05 is next.','P9-05 engineering/preparation is complete; RI-02B-UI-01 is next and P9-H remains available.')
 s=s.replace('P9-05 engineering/human preparation is next.','[P9-05](P9_05_READINESS_REPORT.md) engineering/human preparation is complete on frozen port 5181; RI-02B-UI-01 is next.')
 s=s.replace('Current task pointer: P9-05 is next: city engineering review and human preparation.','Current task pointer: RI-02B-UI-01 is next: shared visual tokens and shell/input foundations. P9-05 engineering/preparation is complete on frozen port 5181; P9-H remains available.')
 s=s.replace('**Next implementation: P9-05 — city engineering review and human preparation.**','**Next implementation: RI-02B-UI-01 — shared visual tokens and shell/input foundations.** [P9-05 engineering/preparation](docs/P9_05_READINESS_REPORT.md) is complete; [post-P9 guide](docs/P9_SESSION_GUIDE.md) and [fresh city](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/fresh-3.json) are ready. Human verdicts remain open.')
 if n=='docs/PROGRESS.md':
  s=s.replace('**Next implementation (2026-09-09, D-EX-51):** P9-05, city engineering review and human preparation after the passing P9-04R repair comparison. RI-02B follows Phase 9 engineering/preparation (P9-05); UI-01 waits on P9-05.','**Next implementation (2026-09-09, D-EX-52):** RI-02B-UI-01, shared visual tokens and shell/input foundations. P9-05 engineering/preparation is complete on frozen port 5181; P9-H is available using the separate ten-seed guide and blank record.')
  s=s.replace('| P9-05 | City engineering review and human preparation | agent | in_progress |','| P9-05 | City engineering review and human preparation | agent | done |').replace('pending — P9-05 report','docs/P9_05_READINESS_REPORT.md; docs/evidence/p9-05-2026-09-09/verification.json')
  s=s.replace('| P9-H | Human ten-seed city review and Phase 9 verdict | human | blocked |','| P9-H | Human ten-seed city review and Phase 9 verdict | human | todo |').replace('pending — post-P9 human record','docs/P9_SESSION_GUIDE.md; docs/P9_SESSION_RECORD.md — NOT RUN')
  s=s.replace('| RI-02B-UI-01 | Shared visual tokens and shell/input foundations | agent | blocked |','| RI-02B-UI-01 | Shared visual tokens and shell/input foundations | agent | todo |')
  s+='\n- 2026-09-09 — P9-05 done under D-EX-52: 205 source inputs/six rebuilt production files match P9-04R; 351 full + 29 focused tests, types/build/lint/legacy snapshot, 20 current city and two older-save browser cases, eight prepared start/save cases, both-width truck/direct-ammo and Turbine controls, natural warning and 80 live edits pass. New frozen port 5181 provides ten fresh seeds and separate shared starts/guide/blank record; EX-08D and prior evidence preserved. P9-H blocked → todo and RI-02B-UI-01 blocked → todo. Human verdicts, ammo diagnosis and performance remain open; no commit or push.\n'
 if n=='docs/PROGRAMME_STATE.md':
  pos=s.index('**The current post-P8 playtest')
  s=s[:pos]+'''**The current post-P9 playtest is frozen on port 5181:** [fresh seed 3](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/fresh-3.json), [ten-seed guide](P9_SESSION_GUIDE.md), [blank human record](P9_SESSION_RECORD.md), [engineering report](P9_05_READINESS_REPORT.md). Ten fresh cities plus ordinary-command assisted construction/network/Turbine starts load paused. Restart with `python docs/evidence/p9-05-2026-09-09/serve.py`. All 205 source inputs/six production files match final P9-04R. New 351 full + 29 focused tests, typechecks/build/lint/legacy snapshot, 20 city + two historical-save browser cases, eight shared start/save cases, both-width truck/direct-ammo and Turbine controls, natural warning and 80 live edits pass. Frame samples reach 116.8 ms; performance and human verdicts remain open. RI-02B-UI-01 is next under D-UI-02; P9-H is available.

'''+s[pos:]
  s=s.replace('**The current post-P8 playtest','**The preserved post-P8 playtest')
  s=s.replace('The mutable development preview, now including P9-03, is on port 5178','The mutable development preview, now including P9-04R, is on port 5178')
  s=s.replace('P9-05 is next; human play and performance remain separate.','P9-05 is now complete; human play and performance remain separate.')
  s=s.replace('use a new post-P8 record.','use the separate post-P9 record for the new checkpoint.')
 p.write_text(s,encoding='utf-8',newline='\n')
p=Path('docs/DECISIONS.md');s=p.read_text(encoding='utf-8');assert '### D-EX-52 ' not in s
s+='''
### D-EX-52 — P9-05 engineering readiness and separate human checkpoint (2026-09-09)

**Authority:** owner (current user), “Ok onto P9-05”. Complete the approved city engineering review and refreshed human preparation. No new gameplay rule, human result, commit or push is implied.

**Measured implementation (agent):** final P9-04R's 205 source inputs and six production files match the freshly rebuilt handoff. New 351 full + 29 focused tests, typechecks/build/lint/legacy snapshot, 20 current city and two historical-save browser cases, eight prepared start/save cases, both-width truck/direct-ammo and paid Turbine controls, natural warning and 80 live edits pass. The exact-source 10,000/10,000 population and 78 campaign checks are retained and integrity checked, not rerun or relabelled. Existing session-log provenance exception in those experiments remains explicit. [Readiness report](P9_05_READINESS_REPORT.md) records boundaries and fresh evidence.

**Preparation choices (agent):** separate frozen port 5181, ten declared fresh seeds 3/4/5/8/11/13/80/88/102/842, ordinary-command assisted construction/network/Turbine starts, a comparable ten-seed protocol and blank human record. No stock/HP/clock/position injection; supplied prefixes/designs disclose assistance. Preserve EX-08D/5180, older checkpoints and prior owner Phase 5 success. Current sample frame intervals reach 116.8 ms; no performance fix or ammo diagnosis is claimed.

**Handoff:** P9-05 is done; P9-H is available and RI-02B-UI-01 is the next implementation under D-UI-02. Human city fairness/recognition/route choices and P6/P7/P8/EX-08/T18 verdicts remain separate and open; P6-AMMO awaits actual retest, reference performance remains unresolved, and wider Q07 progression/endgame is unchanged.
''';p.write_text(s,encoding='utf-8',newline='\n')
