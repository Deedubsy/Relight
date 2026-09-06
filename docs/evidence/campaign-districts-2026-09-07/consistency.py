from pathlib import Path
import re, subprocess
root=Path(__file__).resolve().parents[3]
def original(path):
    return subprocess.check_output(['git','show',f'HEAD:{path}'],cwd=root).decode('utf-8').replace('\r\n','\n')
files=['docs/PROGRAMME_STATE.md','docs/PROGRESS.md','docs/CAMPAIGN_DISTRICTS_REPORT.md','docs/DECISIONS.md','docs/RELIGHT-design.md']
links=0
for file in files:
    p=root/file
    for target in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8')):
        if '://' in target or target.startswith('#'):continue
        dest=target.split('#')[0]
        if not dest or '<' in dest or ' ' in dest:continue
        assert (p.parent/dest).exists(),(file,target)
        links+=1
old=original('docs/RELIGHT-design.md')
new=(root/'docs/RELIGHT-design.md').read_text(encoding='utf-8')
blocks=r'<!-- docsync:([^ ]+).*?<!-- /docsync:\1 -->'
old_blocks=re.findall(blocks,old,re.S)
for name in old_blocks:
    pattern=rf'<!-- docsync:{re.escape(name)} .*?<!-- /docsync:{re.escape(name)} -->'
    assert re.search(pattern,old,re.S).group()==re.search(pattern,new,re.S).group(),name
prior=original('docs/PROGRESS.md')
current=(root/'docs/PROGRESS.md').read_text(encoding='utf-8')
legacy=[line for line in prior.splitlines() if re.match(r'\| (?:RI-|T)[0-9]',line)]
assert all(line in current.splitlines() for line in legacy),'historical task row changed'
assert (root/'packages/sim/src/districts.ts').read_text(encoding='utf-8')==original('packages/sim/src/districts.ts')
changed=subprocess.check_output(['git','diff','--name-only'],cwd=root).decode().splitlines()
assert not any(p.startswith(('docs/experiments/','packages/game/public/snapshots/','packages/sim/fixtures/','docs/archive/')) for p in changed)
assert '| EX-06 | Multi-stop freight, specialised factories and useful workshop | agent | done |' in current
assert '| EX-07 | Optional discovery and distinct exploration encounter | agent | todo |' in current
print(f'PASS: {links} local references, {len(old_blocks)} unchanged legacy generated blocks, {len(legacy)} unchanged historical task rows, unchanged legacy districts module and historical artifacts; EX-06 done / EX-07 todo.')
