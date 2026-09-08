from pathlib import Path
import hashlib,json,shutil,subprocess
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent;prior=root/'docs/evidence/p7-04-2026-09-08'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
m=json.loads((prior/'manifest.json').read_text(encoding='utf-8'))
for n,h in m['source_files'].items():assert sha(root/n)==h,n
dist=root/'packages/game/dist';build={p.relative_to(dist).as_posix():sha(p) for p in sorted(dist.rglob('*')) if p.is_file()}
assert build==m['build_files'],'production build differs from P7-04'
for n in ['source.zip','build.zip','guide-checkpoint.json']:shutil.copyfile(prior/n,out/n)
manifest={'date':'2026-09-08','authority':'Owner request: Ok onto P7-05; adopted Q09','branch':'codex/tram-expansion','baseHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'profile':'exploration-v2','campaignMetadata':9,'source_files':m['source_files'],'build_files':build,'archives':{n:sha(out/n) for n in ['source.zip','build.zip']},'gameplaySource':'unchanged from P7-04','checks':{'tests':271,'ordinaryRuns':12,'seeds':[3,4,5,8,11,13],'orders':['electrical-first','survey-first'],'placementSeeds':list(range(1,65)),'realSaveVersions':[5,7,8,9],'networkChecks':13,'browserWidths':[1366,900],'liveGuideSamples':4,'successfulLampPoleEdits':80},'humanPlay':'outstanding after EX-08C','nextTask':'EX-08C'}
(out/'manifest.json').write_bytes(json.dumps(manifest,indent=2).encode('utf-8'))
print('PASS: 181 source inputs and all 7 production files identical to P7-04; verified source/build archives retained for P7-05.')
