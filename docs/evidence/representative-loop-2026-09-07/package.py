"""Freeze the tested production assets and exact build inputs without committing the checkout."""
from pathlib import Path
import hashlib
import json
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
def digest(data):
    return hashlib.sha256(data).hexdigest()

inputs = subprocess.check_output(['rg', '--files', 'packages'], cwd=ROOT, text=True).splitlines()
inputs += [p.name for pattern in ['package*.json', 'tsconfig*.json', 'eslint.config.*'] for p in ROOT.glob(pattern)]
inputs = sorted(set(p.replace('\\', '/') for p in inputs))
source = {name: (ROOT / name).read_bytes() for name in inputs}
assets = {p.relative_to(ROOT / 'packages/game/dist').as_posix(): p.read_bytes()
          for p in (ROOT / 'packages/game/dist').rglob('*') if p.is_file()}
assets['start.json'] = (OUT / 'start.json').read_bytes()
assets['settings.json'] = (OUT / 'settings.json').read_bytes()
for filename, contents in [('source.zip', source), ('playtest-build.zip', assets)]:
    with zipfile.ZipFile(OUT / filename, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(contents.items()):
            archive.writestr(name, data)
settings = json.loads((OUT / 'settings.json').read_text())
manifest = {key: settings[key] for key in ['source_commit', 'config_hash', 'config_ref', 'generated_at']}
manifest.update({
    'status': 'frozen uncommitted working-tree build; source_commit identifies the parent checkpoint only',
    'runtime': 'WSL Ubuntu-24.04, Node 22.18.0; npm lockfile in source.zip',
    'source_files': {name: digest(data) for name, data in source.items()},
    'served_files': {name: digest(data) for name, data in assets.items()},
    'archives': {name: digest((OUT / name).read_bytes()) for name in ['source.zip', 'playtest-build.zip']},
    'url': 'http://127.0.0.1:5175/?rules=exploration-v2&seed=3&view=world&state=/start.json',
})
(OUT / 'build-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'source_files': len(source), 'served_files': len(assets), 'archives': manifest['archives']}))
