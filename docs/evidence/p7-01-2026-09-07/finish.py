from pathlib import Path
import hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
def read(p):return p.read_text(encoding='utf-8-sig')
def edit(n,a,b):
 p=root/n;s=read(p);assert a in s,(n,a);p.write_bytes(s.replace(a,b,1).encode())
assert '# pass 255' in read(out/'tests.log') and '# fail 0' in read(out/'tests.log')
assert 'built in' in read(out/'typecheck.log')
assert 'error' not in read(out/'lint.log').lower()
assert read(out/'browser.log').count('PASS ')==2
edit('docs/PROGRESS.md','| P7-01 | Persistent campaign discoveries and Electricians | agent | in_progress |','| P7-01 | Persistent campaign discoveries and Electricians | agent | done |')
edit('docs/PROGRESS.md','pending — P7-01 report | |','docs/P7_01_DISCOVERIES_REPORT.md | 2026-09-07 |')
edit('docs/PROGRESS.md','| P7-02 | Concrete crew, Mixer and Barricades | agent | blocked |','| P7-02 | Concrete crew, Mixer and Barricades | agent | todo |')
for n in ['CLAUDE.md','README.md','docs/PROGRESS.md']:
 edit(n,'with catalogue Q09 adopted under D-EX-35 and P7-01 underway.','with catalogue Q09 adopted under D-EX-35 and P7-01 complete (persistent recruits and local Electricians recruitment); P7-02 is next.')
edit('docs/PROGRAMME_STATE.md','P7-01 is underway under D-EX-35.','P7-01 is complete under D-EX-35; P7-02 is next.')
edit('docs/PROGRAMME_STATE.md','P7-01 is the current implementation task.','P7-01 is implemented and verified; P7-02 is the next runnable increment.')
edit('docs/PROGRAMME_STATE.md','## Final validation and retained findings','## Final validation and retained findings\n\nP7-01 freshly passed 255/255 tests, package typechecks/production build, lint, legacy snapshot and documentation checks. Browser button/E recruitment, paid equipment, replay and Ctrl+S/O passed at 1366/900 widths. [P7-01 report](P7_01_DISCOVERIES_REPORT.md) records stable recruits, old earned-unlock migration and 32-seed reachability. These checks do not supply human play or reference-machine performance acceptance.')
p=root/'docs/PROGRESS.md';s=read(p);s+='\n- 2026-09-07 — P7-01 done under D-EX-35: saved campaign recruits, local Electricians interaction, permanent paid electrical capability and version-6 compatibility. 255/255 tests, typecheck/build/lint, unchanged legacy snapshot, browser button/E/save/replay at two sizes and docs/integrity checks passed. P7-02 becomes todo; post-P7 human tasks remain deferred and no commit/push performed. See P7_01_DISCOVERIES_REPORT.md.\n';p.write_bytes(s.encode())
p=root/'docs/P7_01_DISCOVERIES_REPORT.md';s=read(p);s+='''
## Final results

- **255/255 tests passed**, including five P7 tests and the declared 32-seed path sweep: [full log](evidence/p7-01-2026-09-07/tests.log). Paid placement/replay also passed its targeted rerun. The final on-foot check uses the campaign `truckSeat` flag, not the legacy truck toggle.
- All-package [typechecks and production build](evidence/p7-01-2026-09-07/typecheck.log), [lint](evidence/p7-01-2026-09-07/lint.log), [legacy snapshot](evidence/p7-01-2026-09-07/snapshot.log), [docsync](evidence/p7-01-2026-09-07/docsync.log) and [freshness](evidence/p7-01-2026-09-07/freshness.log) passed. The existing Vite large-chunk advisory remains.
- [Browser results](evidence/p7-01-2026-09-07/browser.json): actual panel button at 1366×900 and keyboard E at 900×900, unchanged recruitment stock, paid Floodlight/Big pole construction, full replay and Ctrl+S/O save/reload; no page errors or horizontal overflow. The initial keyboard probe omitted the canvas page offset; correcting pointer coordinates passed without an interaction-code change. Both panel screenshots were inspected. Isolated headless Chrome 153, using command hooks for stock/walking/placement, is automated evidence rather than a human playtest.
- [Manifest](evidence/p7-01-2026-09-07/manifest.json) identifies current source/build and preserves source/build archives. Prior EX-08A/B, P6 and scope-review evidence remains unchanged. These small interaction checks do not close earlier light-map stalls or reference-machine validation.
''';p.write_bytes(s.encode())
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
prior=root/'docs/evidence/ex08b-2026-09-07';m=json.loads(read(prior/'verification-manifest.json'))
for n,h in m['protected_files'].items():assert sha(root/n)==h,n
for n,h in m['verification_files'].items():assert sha(prior/n)==h,n
for n in ['docs/EX08B_PREPARATION_REPORT.md','docs/EX08B_SESSION_GUIDE.md','docs/EX08B_SESSION_RECORD.md']:
 assert sha(root/n)==m['documents'][n],n
review=json.loads(read(root/'docs/evidence/phase7-review-2026-09-07/manifest.json'))
assert sha(root/'docs/PHASE_7_SCOPE_REPORT.md')==review['documents']['docs/PHASE_7_SCOPE_REPORT.md']
sources={n:sha(root/n) for n in review['source_files']}
for n in ['packages/sim/src/campaignRecruits.ts','packages/sim/test/campaignRecruits.test.ts']:sources[n]=sha(root/n)
build={p.relative_to(root/'packages/game/dist').as_posix():sha(p) for p in (root/'packages/game/dist').rglob('*') if p.is_file()}
for archive,files,prefix in [('source.zip',sources,root),('build.zip',build,root/'packages/game/dist')]:
 with zipfile.ZipFile(out/archive,'w',zipfile.ZIP_DEFLATED) as z:
  for n in files:z.write(prefix/n,n)
manifest={'parent_commit':'17939145b5d40afe4dcfc1937a3ec6d34b763df6','source_files':sources,'build_files':build,'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'checks':{'tests':255,'seedSweep':32,'browserWidths':[1366,900]},'human':'deferred until after P7'}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('PASS 255 tests; 173 source inputs and build archived; prior evidence/records preserved; P7-01 done and P7-02 next.')
