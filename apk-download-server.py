from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

APK = Path(__file__).parent / "LiftDesk-Android-armv7.apk"

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path != "/LiftDesk-Android-armv7.apk":
            self.send_error(404)
            return
        size = APK.stat().st_size
        self.send_response(200)
        self.send_header("Content-Type", "application/vnd.android.package-archive")
        self.send_header("Content-Length", str(size))
        self.send_header("Content-Disposition", 'attachment; filename="LiftDesk-Android-armv7.apk"')
        self.end_headers()
        with APK.open("rb") as apk:
            while chunk := apk.read(1024 * 256):
                self.wfile.write(chunk)

    def log_message(self, fmt, *args):
        pass

ThreadingHTTPServer(("0.0.0.0", 8766), Handler).serve_forever()
