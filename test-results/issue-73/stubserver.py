# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

# Manual repro stub for issue-73 E2E debugging (not part of the shipped suite).
import http.server
import urllib.parse

PORT = 8877
BASE = f"http://127.0.0.1:{PORT}"

SITE_HTML = f"""<html><head><title>Stub Site</title>
<link rel="alternate" type="application/rss+xml" href="/feeds/site-feed.xml" title="Stub Site Feed">
</head><body>stub site</body></html>"""

RSS = """<?xml version="1.0"?>
<rss version="2.0"><channel><title>{t}</title><link>{b}</link>
<description>stub</description>
<item><title>i</title><link>{b}/a</link></item></channel></rss>"""

DIRECTORY_HIT = """[{"url":"%s/feeds/search-hit.xml","title":"Stub Search Hit",
"description":"x","site_name":"s","site_url":"%s","score":0.9,"bozo":0}]""" % (BASE, BASE)


class H(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        path = urllib.parse.urlparse(self.path).path
        q = urllib.parse.parse_qs(urllib.parse.urlparse(self.path).query)
        if path == "/directory":
            body = DIRECTORY_HIT if q.get("url", [""])[0].rstrip("/") == BASE else "[]"
            self._send(200, "application/json", body)
        elif path == "/site":
            self._send(200, "text/html", SITE_HTML)
        elif path == "/empty":
            self._send(200, "text/html", "<html><head></head><body>empty</body></html>")
        elif path.startswith("/feeds/") and path.endswith(".xml"):
            name = path[len("/feeds/"):-len(".xml")]
            self._send(200, "application/rss+xml", RSS.format(t=name + ".xml", b=BASE + path))
        else:
            self._send(404, "text/plain", "nope")

    def _send(self, code, ct, body):
        self.send_response(code)
        self.send_header("Content-Type", ct)
        self.end_headers()
        self.wfile.write(body.encode())

    def log_message(self, fmt, *args):
        print(fmt % args, flush=True)


http.server.HTTPServer(("127.0.0.1", PORT), H).serve_forever()
