"""Serve only the archived UI-07 build; keep mutable and earlier frozen previews separate."""
from pathlib import Path
from http.server import BaseHTTPRequestHandler,HTTPServer
from urllib.parse import urlsplit,unquote
import zipfile,mimetypes,hashlib,json
out=Path(__file__).resolve().parent
m=json.loads((out/'manifest.json').read_text())
assert hashlib.sha256((out/'build.zip').read_bytes()).hexdigest()==m['archives']['build.zip']
with zipfile.ZipFile(out/'build.zip') as z:files={n:z.read(n) for n in z.namelist()}
for n,data in files.items():assert hashlib.sha256(data).hexdigest()==m['build_files'][n]
class Handler(BaseHTTPRequestHandler):
 def do_GET(self):
  name=unquote(urlsplit(self.path).path).lstrip('/') or 'index.html'
  if name not in files:self.send_error(404);return
  data=files[name];self.send_response(200);self.send_header('Content-Type',mimetypes.guess_type(name)[0] or 'application/octet-stream');self.send_header('Content-Length',str(len(data)));self.send_header('Cache-Control','no-store');self.end_headers();self.wfile.write(data)
 def log_message(self,*args):pass
print('Frozen UI-07: http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json',flush=True)
HTTPServer(('127.0.0.1',5182),Handler).serve_forever()
