from pathlib import Path
import json
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent
read=lambda p:p.read_text(encoding='utf-8-sig')
assert '# pass 376' in read(out/'test.log') and '# fail 0' in read(out/'test.log')
assert '# pass 17' in read(out/'focused-final.log')
for name,count in [('matrix',8),('preferences',3),('integration',4),('accessibility',4),('inspection-browser',6),('regression-browser',2),('building',4),('projects',2)]:
 b=json.loads(read(out/(name+'-result.json')));assert len(b['rows'])==count and not b['errors'],name
for n in ['CLAUDE.md','README.md','docs/PROGRAMME_STATE.md','docs/PHASES.md','docs/EXPLORATION_DEFENCE_PLAN.md','docs/STANDARDS.md']:
 p=root/n;s=read(p).replace('RI-02B-UI-06 is next','RI-02B-UI-07 is next')
 if n=='CLAUDE.md':
  a=s.index('Current task pointer:');z=s.index('\n\n',a);s=s[:a]+'Current task pointer: RI-02B-UI-07 is next: actual visual review, usability feedback and targeted refinement. UI-01–06 implementation and integration are complete; evidence is in docs/evidence/ui-redesign/README.md. P9-05 remains frozen on port 5181 and P9-H remains available. PROGRESS owns execution order.'+s[z:]
 if n=='README.md':s=s.replace('**Next implementation: RI-02B-UI-06 — scaling, accessibility, settings and full integration.**','**Next implementation: RI-02B-UI-07 — actual visual review, usability feedback and targeted refinement.**').replace('UI-01–05 shell, HUD, building, inspection and Projects/alerts','UI-01–06 interface and integration').replace('settings/scaling and integration remain in UI-06.','independent scaling, motion preferences, rebinding and integration are complete. UI-07 reviews the delivered experience.')
 if n=='docs/PROGRAMME_STATE.md':
  a=s.index('Current handoff:');z=s.index('\n\nPrior city/construction handoff:',a)
  s=s[:a]+'Current handoff: 2026-09-09. **RI-02B-UI-06 is complete:** independent 100/125/150% interface scaling, system-aware reduced motion, live binding editor/reset, persisted quickbar/hints and recovery from corrupt or unavailable browser storage. Input capture and drawer/menu/modal layering are verified. [Evidence](evidence/ui-redesign/README.md); [mutable UI preview](http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json). 376 full tests, 17 focused tests, types/build/lint/docs and the eight-case PC scaling matrix pass. Current campaign and legacy save/replay, migrated older campaign loading, construction/library/truck/freight and prior UI controls are checked. UI-07 is next for actual visual review and targeted refinement. Simulation, save schema, city generation and campaign fingerprint are unchanged. Human verdicts and existing performance findings remain open.'+s[z:]
  s=s.replace('and UI-01–05 implementation are local changes','and UI-01–06 implementation are local changes').replace('now including UI-05','now including UI-06')
 p.write_text(s,encoding='utf-8',newline='\n')
p=root/'docs/PROGRESS.md';s=read(p);a=s.index('**Next implementation (');z=s.index('\n',a)
s=s[:a]+'**Next implementation (2026-09-09, D-UI-08):** RI-02B-UI-07, actual visual review, usability feedback and targeted refinement. UI-01–06 implementation and integration are complete; evidence is in docs/evidence/ui-redesign/README.md. P9-05 remains frozen on port 5181; P9-H and all existing human gates remain available.'+s[z:]
lines=[]
for line in s.splitlines():
 if line.startswith('| RI-02B-UI-06 |'):line=line.replace('| in_progress |','| done |').replace('pending — docs/evidence/ui-redesign/README.md','376 full + 17 focused; types/build/lint/docs; eight scale cases, preferences, paid construction, legacy/save/replay and prior UI browser flows — docs/evidence/ui-redesign/README.md')
 if line.startswith('| RI-02B-UI-07 |'):line=line.replace('| blocked |','| todo |')
 lines.append(line)
s='\n'.join(lines)+'\n\n- 2026-09-09 — RI-02B-UI-06 done under D-UI-08: independent scaling, motion, live rebinding, browser preference recovery and full integration. 376 full + 17 focused tests, types/build/lint/docs and eight-case browser matrix pass; paid construction, library/truck/freight, prior UI flows and save/profile checks verified. UI-07 blocked → todo. Older migrated campaign replay remains unavailable under the existing guard; human gates and performance findings remain open. No commit or push.\n';p.write_text(s,encoding='utf-8',newline='\n')
p=root/'docs/DECISIONS.md';s=read(p);assert '### D-UI-08 ' not in s
s+='''
### D-UI-08 — UI-06 scaling, preferences and integration (2026-09-09)

**Authority:** owner (current user), “Continue”, following the UI-01–05 sequence. Implements the existing UI-06 specification; developer browser QA is not human acceptance.

**Implementation choices (agent):** 100/125/150% UI scale changes shared text/control geometry independently of camera zoom. Layout uses logical viewport size to reflow compact and short screens; all ten quickbar slots retain their order. Settings offers system motion by default and explicit reduced/full overrides. The versioned browser preference record extends the existing quickbar format with scale, motion, validated binding overrides and opening-hint dismissal. Corrupt fields recover independently; denied storage keeps session settings and explains retry. Saves and simulation state contain none of these settings.

Bindings are live in handlers, held movement, buttons and Help. Same-context collisions are rejected; Ctrl/Cmd and blueprint contexts retain their established distinction. Escape, Tab navigation and numbered slot keys stay fixed; Build edits slot contents. Applying preferences releases held world input. UI target handlers receive key events before bubbling is blocked from the world. Drawer, More menu and Pause layers preserve accessible controls and focus. Existing profile-separated save/load/export and unsaved-change confirmation are retained.

**Evidence:** docs/evidence/ui-redesign/README.md UI-06; 376 full and 17 focused tests, types/build/lint/docs, eight PC scale cases, current campaign/legacy exact replay, older campaign migration, library/order/truck/freight and prior UI flows. The old P7 save loads through existing upgrades and truthfully loses complete replay eligibility; no migration or replay policy changed. Simulation, harness, city generation, costs and save schema are unchanged. UI-07 is next. Human gates and existing performance findings remain open; no commit or push.
''';p.write_text(s,encoding='utf-8',newline='\n')
print('Updated task handoff and D-UI-08; UI-07 next.')
