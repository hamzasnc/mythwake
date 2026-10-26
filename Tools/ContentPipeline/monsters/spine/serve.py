"""Static server for the preview folder that also accepts POST /save?name=<file>.png (data URL body)."""
import base64, os, re, sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

ROOT = os.path.abspath(sys.argv[1])
OUTDIR = os.path.join(ROOT, "frames")
os.makedirs(OUTDIR, exist_ok=True)


class H(SimpleHTTPRequestHandler):
    def __init__(self, *a, **k):
        super().__init__(*a, directory=ROOT, **k)

    def do_POST(self):
        q = parse_qs(urlparse(self.path).query)
        name = q.get("name", ["frame.png"])[0]
        if not re.fullmatch(r"[A-Za-z0-9_.-]+\.png", name):
            self.send_error(400)
            return
        body = self.rfile.read(int(self.headers["Content-Length"])).decode()
        data = base64.b64decode(body.split(",", 1)[1])
        with open(os.path.join(OUTDIR, name), "wb") as f:
            f.write(data)
        self.send_response(200)
        self.end_headers()
        self.wfile.write(b"ok")

    def log_message(self, *a):
        pass


ThreadingHTTPServer(("127.0.0.1", int(sys.argv[2])), H).serve_forever()
