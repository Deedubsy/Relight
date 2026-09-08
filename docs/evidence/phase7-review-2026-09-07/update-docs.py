from pathlib import Path

root = Path(__file__).resolve().parents[3]
def edit(name, old, new):
    p = root / name
    s = p.read_text(encoding='utf-8-sig')
    assert old in s, (name, old)
    p.write_text(s.replace(old, new, 1), encoding='utf-8')

old = 'EX-08B shared preparation is complete; the paused port-5177 playtest and EX08B_SESSION_GUIDE.md are ready for EX-08H/P6-H observations.'
new = 'EX-08B remains the completed Phase 6 checkpoint. D-EX-34 moves shared playtesting after P7; the Phase 7 scope review is complete in docs/PHASE_7_SCOPE_REPORT.md, with catalogue choice Q09 next and implementation pending. EX-08C will refresh preparation after P7.'
for name in ['CLAUDE.md', 'README.md', 'docs/PROGRESS.md']:
    edit(name, old, new)
edit('README.md', '[Open the paused Phase 6 playtest]', '[Open the retained Phase 6 checkpoint]')
edit('docs/PROGRESS.md', 'EX-08B prepares the shared follow-up build after P6-04. Broader progression retains its EX-08/Q07 dependencies.', 'EX-08C will prepare the post-P7 shared build. P7 catalogue Q09 is next; broader progression retains EX-08/Q07 dependencies.')
edit('docs/DECISIONS.md', '\n## Supersession', '\n## Supersession') if False else None
p=root/'docs/DECISIONS.md';s=p.read_text(encoding='utf-8');pos=s.index('\n',s.index('| D-EX-33 |'))
s=s[:pos]+'\n\n| D-EX-34 | Move shared playtesting after Phase 7 and begin its discovery scope review. | decided | owner (current user) | 2026-09-07 | “OK lets do the playtest after P7. Lets move onto P7”. | Supersedes EX-08-before-P7 sequencing: P7-00 reconciles the catalogue, adopted P7 implementation follows, EX-08C refreshes preparation, then shared human observations. EX-08B stays completed historical preparation; no human task is passed or waived. Q09 records the proposed catalogue separately; Q07 remains open for full progression/endgame. No commit or push implied. |'+s[pos:]
pos=s.index('\n',s.index('| D-EX-Q08 |'))
s=s[:pos]+'\n| D-EX-Q09 | Revised Phase 7 facility/recruit catalogue and Phase 8 boundary. | recommended | Coding-agent recommendation, 2026-09-07, PHASE_7_SCOPE_REPORT.md: explicit local Electricians recruitment; Concrete crew with Mixer/Concrete/Barricade; restored riverside Turbine hall; optional Lamplighters/Arc lamp and Surveyors/district information; truthful discovery/recipe UI. Credit existing tram/radio/workshop/repair and direct turret feed. Defer Foundry/Arsenal/Refinery/Chemist/endgame to wider reconciliation; deliver Foreman/blueprint recruitment with its working Phase 8 tools. Owner has authorised review and sequencing, not yet chosen this revised catalogue. | P7-01–05 |'+s[pos:];p.write_text(s,encoding='utf-8')

p=root/'docs/PROGRESS.md';s=p.read_text(encoding='utf-8');pos=s.index('| EX-08H |')
rows='''| P7-00 | Review found-tech scope and reconcile the retained catalogue | agent | done | D-EX-34 | Existing capabilities, conflicting legacy unlocks, candidate catalogue, implementation acceptance and post-P7 human sequencing recorded; focused baseline and documentation checks reported | docs/PHASE_7_SCOPE_REPORT.md | 2026-09-07 |
| P7-D | Choose the revised discovery catalogue | human | todo | P7-00; D-EX-Q09 | Owner selects or revises the concrete catalogue and deferred boundaries in PHASE_7_SCOPE_REPORT.md; Q09 records the actual choice | docs/DECISIONS.md D-EX-Q09 — recommendation awaiting choice | |
| P7-01 | Persistent campaign discoveries and Electricians | agent | blocked | P7-D | PHASE_7_SCOPE_REPORT.md P7-01: accessible local recruitment, real unlocks, stable records, earned capability/old-save preservation and replay | pending — P7-01 report | |
| P7-02 | Concrete crew, Mixer and Barricades | agent | blocked | P7-01 | PHASE_7_SCOPE_REPORT.md P7-02: paid stone/concrete production and working defences, routing/inspection/repair/removal conservation | pending — P7-02 report | |
| P7-03 | Turbine restoration and optional lighting/survey discoveries | agent | blocked | P7-02 | PHASE_7_SCOPE_REPORT.md P7-03: useful connected generation, optional working rewards, fair placement, no false base registration | pending — P7-03 report | |
| P7-04 | Discovery and recipe information | agent | blocked | P7-03 | PHASE_7_SCOPE_REPORT.md P7-04: truthful revealed/recruited/operational state, recipe provenance and ordinary responsive controls | pending — P7-04 report | |
| P7-05 | Discovery integration and engineering review | agent | blocked | P7-04 | PHASE_7_SCOPE_REPORT.md P7-05: every adopted reward used through paid commands, alternate orders/seeds, conservation/save/replay, full checks and limits | pending — P7-05 report | |
| EX-08C | Prepare the post-P7 shared playtest | agent | blocked | P7-05 | Freeze resulting source/build/starts, refresh guide and blank record for P5/P6/P7 observations and ammo retest; preserve earlier EX-08A/B evidence | pending — EX-08C preparation report | |
| P7-H | Human discovery observations and Phase 7 verdict | human | blocked | EX-08C | Observe self-directed discovery, useful rewards, restoration/recipe comprehension and optional route choice; record actual owner verdict separately from automated results | pending — post-P7 shared observation record | |
'''
s=s[:pos]+rows+s[pos:]
s=s.replace('| EX-08H | Play the representative loop | human | todo | EX-08B |', '| EX-08H | Play the representative loop | human | blocked | EX-08C (D-EX-34) |')
s=s.replace('EX08B_SESSION_GUIDE.md reconciled protocol and EX08B_SESSION_RECORD.md observation copy, interventions and explicit gaps; human verdict', 'Post-P7 EX-08C protocol and new observation copy, interventions and explicit gaps; retain earlier confirmed play and separate human verdicts')
s=s.replace('| P6-H | Human Phase 6 observations and verdict | human | todo | P6-04; EX-08B |','| P6-H | Human Phase 6 observations and verdict | human | blocked | P6-04; EX-08C (D-EX-34) |')
s=s.replace('| T18 | Human Phase 5 construction exercise and exit | human | todo | EX-08B |','| T18 | Human Phase 5 construction exercise and exit | human | blocked | EX-08C (D-EX-34) |')
s=s.replace('owner gate recorded before content expansion', 'owner gate recorded; P7 precedes this assessment under D-EX-34, while dependent wider progression follows it')
s += '\n- 2026-09-07 — D-EX-34 moves shared play after P7. P7-00 done: source/catalogue/standards reconciliation and five proposed increments recorded in PHASE_7_SCOPE_REPORT.md; 19/19 focused baseline tests passed. P7-D is todo for catalogue Q09; dependent increments and EX-08C/P7-H inserted. EX-08H/P6-H/T18 change from todo to blocked on post-P7 preparation. EX-08B and its blank record remain historical; no human verdict or gameplay change claimed.\n'
p.write_text(s,encoding='utf-8')

edit('docs/PROGRAMME_STATE.md', '**EX-08B preparation is complete; EX-08H/P6-H shared human observation is next.**', '**Phase 7 scope review is complete; catalogue choice Q09 is next. Shared playtesting moves after P7 under D-EX-34.**')
edit('docs/PROGRAMME_STATE.md', 'The frozen shared test is now served separately', 'The retained Phase 6 checkpoint is served separately')
p=root/'docs/PROGRAMME_STATE.md';s=p.read_text(encoding='utf-8');a=s.index('1. **EX-08B is complete:**');b=s.index('\n## Final validation',a)
s=s[:a]+'''1. **P7-00 is complete:** [Phase 7 scope review](PHASE_7_SCOPE_REPORT.md) reconciles current source, legacy facilities/recruits and standards. Fresh opening/discovery/district baseline: 19/19 tests passed; no gameplay source changed.
2. **P7-D / Q09 is next:** choose or revise the proposed Electricians, concrete/Barricade chain, Turbine hall, optional lighting/surveying and discovery/recipe UI catalogue. Recommended deferred boundaries are explicit; the proposal is not an owner-approved game rule yet.
3. **P7-01–05** implement and verify the adopted scope, then **EX-08C** prepares a new shared build/guide/blank record. EX-08B stays the completed pre-P7 checkpoint, with its archives and port 5177 untouched.
4. **EX-08H / P6-H / P7-H / T18** follow EX-08C. Carry the ammo retest and confirmed earlier factory/transport observations forward. No human task is passed or waived by this sequencing change.

Q07 remains open for full project progression/endgame and dependent EX-09/10 work. P7-AMMO is not a new task: the existing P6-AMMO investigation resumes from owner retest or an affected save. Existing performance findings and D-SA-2/C3/D-B2-2 positions remain open. Phase 6 technical readiness is not full phase approval.
'''.replace('P7-AMMO is not a new task: the existing P6-AMMO investigation', 'The existing P6-AMMO investigation')+s[b:];p.write_text(s,encoding='utf-8')

for name in ['docs/PHASES.md','docs/EXPLORATION_DEFENCE_PLAN.md','docs/STANDARDS.md']:
    p=root/name;s=p.read_text(encoding='utf-8');i=s.index('\n\n')
    note='\n\n**Current sequencing (D-EX-34, 2026-09-07):** the owner moves shared playtesting after P7. [Phase 7 scope review](PHASE_7_SCOPE_REPORT.md) is complete; catalogue Q09 is recommended pending the owner’s choice. P7 implementation and EX-08C preparation precede EX-08H/P6-H/P7-H and remaining T18 observations. EX-08B is retained historical preparation, not the post-P7 build. Earlier sequencing statements below describe prior checkpoints; human criteria remain outstanding.'
    s=s[:i]+note+s[i:];p.write_text(s,encoding='utf-8')
edit('docs/PHASES.md', 'Complete the approved facility/survivor/discovery catalogue after the slice.', 'Complete the facility/survivor/discovery catalogue reconciled in [the P7 review](PHASE_7_SCOPE_REPORT.md), after Q09 selection. The proposed reduced catalogue and Phase 8 Foreman boundary are recommendations until then.')
edit('docs/RELIGHT-design.md', 'Existing facilities and survivor unlocks are a reusable catalogue subject to revised placement, costs and dependencies.', 'Existing facilities and survivor unlocks are a reusable catalogue subject to revised placement, costs and dependencies. D-EX-34 moves Phase 7 ahead of the shared playtest. [The P7 scope review](PHASE_7_SCOPE_REPORT.md) proposes a revised catalogue under open choice Q09; its entries and deferred Phase 8 boundary are not adopted gameplay rules until the owner chooses them.')
edit('docs/PHASE_7_SCOPE_REPORT.md', 'The baseline review runs existing campaign opening, discovery and district tests; results are recorded', 'The baseline review passed **19/19 existing campaign opening, discovery and district tests** through WSL Ubuntu-24.04 (the initial sandbox launch was denied; the approved retry passed); results are recorded')
