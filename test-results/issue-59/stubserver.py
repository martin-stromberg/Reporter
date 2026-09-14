# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

"""Local stub servers for the manual UI verification of the feed search.

Port 8099: serves a HTML page advertising a feed via <link rel="alternate">
           plus the feed document itself (autodiscovery yields a Discovered
           result; fetching the feed URL directly yields an ExactUrl result).
Port 8100: serves an empty HTML page and 404s on every well-known feed path
           (autodiscovery yields no results -> direct-add fallback dialog).
"""

import http.server
import threading

HTML_WITH_LINK = b"""<html><head>
<link rel="alternate" type="application/rss+xml" title="Stub Feed" href="/feed.xml">
</head><body>stub site</body></html>"""

EMPTY_HTML = b"<html><head><title>empty</title></head><body>nothing here</body></html>"

RSS = b"""<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0"><channel>
<title>Stub News</title>
<link>http://localhost:8099/</link>
<description>Stub feed for manual UI verification</description>
<item><title>Erster Artikel</title><link>http://localhost:8099/a1</link></item>
</channel></rss>"""

FEED_PATHS = {"/feed", "/rss", "/rss.xml", "/atom.xml", "/feed.xml", "/index.xml"}


class SiteWithFeed(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path in FEED_PATHS:
            self.send_response(200)
            self.send_header("Content-Type", "application/rss+xml")
            self.end_headers()
            self.wfile.write(RSS)
        else:
            self.send_response(200)
            self.send_header("Content-Type", "text/html")
            self.end_headers()
            self.wfile.write(HTML_WITH_LINK)

    def log_message(self, fmt, *args):
        print("8099:", fmt % args, flush=True)


class EmptySite(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path in FEED_PATHS:
            self.send_response(404)
            self.end_headers()
        else:
            self.send_response(200)
            self.send_header("Content-Type", "text/html")
            self.end_headers()
            self.wfile.write(EMPTY_HTML)

    def log_message(self, fmt, *args):
        print("8100:", fmt % args, flush=True)


def serve(handler, port):
    http.server.HTTPServer(("127.0.0.1", port), handler).serve_forever()


threading.Thread(target=serve, args=(SiteWithFeed, 8099), daemon=True).start()
threading.Thread(target=serve, args=(EmptySite, 8100), daemon=True).start()
print("stub servers on 8099 (site+feed) and 8100 (empty)")
threading.Event().wait()
