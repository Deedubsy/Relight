from pathlib import Path
import json
root=Path(__file__).resolve().parents[4];out=Path(__file__).resolve().parent
read=lambda p:p.read_text(encoding='utf-8-sig')
assert '# pass 376' in read(out/'test.log') and '# fail 0' in read(out/'test.log')
assert '# pass 17' in read(out/'focused.log')
for name,count in [('matrix',8),('warnings',20),('inspection-browser',3),('regression-browser',2),('building',2),('projects',1)]:
 b=json.loads(read(out/(name+'-result.json')));assert len(b['rows'])==count and not b['errors'],name
next='RI-02B-UI-01–07 engineering is complete; current-build human feedback is next'
for n in ['CLAUDE.md','README.md','docs/PROGRAMME_STATE.md','docs/PHASES.md','docs/EXPLORATION_DEFENCE_PLAN.md','docs/STANDARDS.md']:
 p=root/n;s=read(p).replace('RI-02B-UI-07 is next',next)
 if n=='CLAUDE.md':
  a=s.index('Current task pointer:');z=s.index('\n\n',a);s=s[:a]+'Current task pointer: UI-01–07 engineering is complete. UI-07 is frozen on port 5182; the current-build fresh-player guide and blank observations are in docs/evidence/ui-redesign/README.md. Human feedback and existing formal human gates remain pending. EX-08 still depends on EX-08H; do not start broader progression without its prerequisites. PROGRESS owns execution order.'+s[z:]
 if n=='README.md':
  s=s.replace('**Next implementation: RI-02B-UI-07 — actual visual review, usability feedback and targeted refinement.**','**UI-01–07 engineering is complete. Next: a brief fresh-player review and the existing human playtest gates.**').replace('UI-01–06 interface and integration','UI-01–07 interface, integration and visual review').replace('http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json','http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json').replace('UI-07 reviews the delivered experience.','UI-07 corrected observed overlap and character-identification issues; fresh-player feedback remains pending.')
 if n=='docs/PROGRAMME_STATE.md':
  a=s.index('Current handoff:');z=s.index('\n\nPrior city/construction handoff:',a)
  s=s[:a]+'Current handoff: 2026-09-09. **RI-02B-UI-07 is complete:** actual opening/build/machine/project/warning review and one focused refinement pass. The engineer has a brief opening label; core HP sits above the building; the startup notice no longer obscures a fresh opening. Drawer/camera bounds include ultrawide margins; compact high-scale HUD gives urgent warnings priority. [Evidence and current-build follow-up guide](evidence/ui-redesign/README.md); [frozen UI-07 opening](http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json). 376 full tests, 17 focused tests, types/build/lint/docs, paid UI walkthroughs, eight scaling cases and 20 urgent-state cases pass. Five game files changed against UI-06; simulation, save schema, city generation and campaign fingerprint are unchanged. UI engineering is complete; fresh-player feedback remains pending and EX-08 still waits on EX-08H. No human verdict or existing performance finding is closed.'+s[z:]
  s=s.replace('and UI-01–06 implementation are local changes','and UI-01–07 implementation are local changes').replace('now including UI-06','now including UI-07')
 p.write_text(s,encoding='utf-8',newline='\n')
p=root/'docs/PROGRESS.md';s=read(p);a=s.index('**Next implementation (');z=s.index('\n',a)
s=s[:a]+'**Current handoff (2026-09-09, D-UI-09):** UI-01–07 engineering is complete. UI-07 is frozen on port 5182; its fresh-player guide and blank observation area are in docs/evidence/ui-redesign/README.md. Human feedback and P9-H/P6-H/P7-H/P8-H/EX-08H/T18 remain pending. EX-08 still waits for EX-08H; broader progression retains its dependencies.'+s[z:]
lines=[]
for line in s.splitlines():
 if line.startswith('| RI-02B-UI-07 |'):line=line.replace('| in_progress |','| done |').replace('pending — docs/evidence/ui-redesign/README.md','376 full + 17 focused; paid browser walkthroughs, eight scales and 20 urgent cases; visual review and frozen port 5182 follow-up — docs/evidence/ui-redesign/README.md');line=line[:-1]+'2026-09-09 |'
 lines.append(line)
s='\n'.join(lines)+'\n\n- 2026-09-09 — RI-02B-UI-07 done under D-UI-09: actual visual review and one focused refinement pass correct engineer-label, compact danger and ultrawide drawer overlaps. 376 full + 17 focused tests, types/build/lint/docs, paid browser walkthroughs, eight scale cases and 20 urgent-state cases pass. Port 5182 freezes the resulting build and ordinary P9-05 starts. Fresh-player feedback remains pending in the shared evidence note; EX-08 still waits on EX-08H. Formal human gates/performance findings unchanged; no commit or push.\n';p.write_text(s,encoding='utf-8',newline='\n')
p=root/'docs/DECISIONS.md';s=read(p);assert '### D-UI-09 ' not in s
s+='''
### D-UI-09 — UI-07 actual review and bounded refinement (2026-09-09)

**Authority:** owner (current user), “Continue”, after verified UI-06. Executes the existing UI-07 review scope, including one targeted refinement pass; this is not human usability approval.

**Observed and corrected:** the startup notice and core HP label obscured the fresh engineer; a brief “You” label now identifies the character until two tiles have been walked, the core label sits above its building and the fresh paused opening uses its existing HUD instead of a transient load notice. Ordinary loaded campaigns retain a concise load acknowledgement. At high scale on short screens, reduced secondary spacing leaves more city visible; while a drawer and urgent warning are open, the compact objective card yields to danger (Projects retains its full information). HP, equipment, stock, speed and warning controls remain visible. Ultrawide drawer/camera reservations now include its actual safe margin, preventing the warning strip from covering the drawer header. All changes are presentation-only in five game files.

**Evidence and limits:** docs/evidence/ui-redesign/README.md UI-07, exact UI-06 comparison and preserved P9-05 prepared starts. Opened baseline is 1280×720, seed 3, time 0:00, camera 0.65. Paid commands verify supplies/build/inspection/project delivery; explicit prepared urgent fixtures cover ten states at two sizes. 376 full + 17 focused tests, types/build/lint/docs, eight scales and save/replay/input checks pass. No new simulation batches or content. The immutable UI-07 build is served on port 5182; mutable 5178 and frozen 5177/5179/5180/5181 remain separate.

Fresh-player observation is recommended and pending; its blank current-build observation area stays in the shared evidence note. No intuition score or unaided-play success is inferred. UI-07 engineering is complete; EX-08 remains blocked on EX-08H and broader progression retains Q07 and its other prerequisites. Existing formal human gates and performance findings remain open. No commit or push.
''';p.write_text(s,encoding='utf-8',newline='\n')
print('UI-07 complete; frozen current-build follow-up and human feedback next; EX-08 remains blocked on EX-08H.')
