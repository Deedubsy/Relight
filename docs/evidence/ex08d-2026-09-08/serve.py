"""Serve only the verified frozen playtest archive on loopback. Ctrl+C stops it."""
from pathlib import Path
import hashlib
import http.server
import json
import tempfile
import zipfile

HERE = Path(__file__).resolve().parent
manifest = json.loads((HERE / 'build-manifest.json').read_text())
archive = HERE / 'playtest-build.zip'
if hashlib.sha256(archive.read_bytes()).hexdigest() != manifest['archives'][archive.name]:
    raise SystemExit('Playtest archive differs from the recorded build; refusing to serve.')
with tempfile.TemporaryDirectory(prefix='relight-ex08d-') as temp:
    root = Path(temp).resolve()
    with zipfile.ZipFile(archive) as bundle:
        for name in bundle.namelist():
            target = (root / name).resolve()
            if not target.is_relative_to(root):
                raise SystemExit('Invalid archive path.')
            data = bundle.read(name)
            if hashlib.sha256(data).hexdigest() != manifest['served_files'].get(name):
                raise SystemExit('Build file hash mismatch: ' + name)
        bundle.extractall(root)
    def handler(*args, **kwargs):
        return http.server.SimpleHTTPRequestHandler(*args, directory=str(root), **kwargs)
    with http.server.ThreadingHTTPServer(('127.0.0.1', 5180), handler) as server:
        print(manifest['url'], flush=True)
        print('Frozen EX-08H-C build. Fresh, pre-warning and Turbine starts stay paused until the player starts. Ctrl+C stops the server.', flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass
