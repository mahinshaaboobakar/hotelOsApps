"""Start this stream's harness servers on ports nobody else holds, and prove each is ours.

A port is a shared resource. Two harnesses bound 8853 today and GG's browser was
answered by mine — a jobs page in a workforce capture, which cost them the time
it takes to ask why their own module had no data.

Three rules, from that:

  * probe UPWARD for a free port from a base of this stream's own, rather than
    incrementing from wherever we last were — incrementing is how two streams
    pick the same number.
  * verify by IDENTITY, never by status: a 200 cannot distinguish a server from
    *your* server, so each root is checked for a file only it holds.
  * never kill by port. The old servers are killed by pid, and only after their
    command line shows `--directory` inside this application's tree.
"""
import http.server, functools, socket, subprocess, sys, threading, urllib.request, json, os

BASE = 8901                            # this stream's base; GG's harnesses sit lower
ROOTS = {
    "ui": r"C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui",
    "mockups": r"C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/docs/mockups",
    "frames": r"C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/parta/frames",
}
# What only THIS root holds — the identity check, not a status code.
PROOF = {
    "ui": ("/preview/frame.html", "Jobs module realm"),
    "mockups": ("/01-the-jobs-screens.html", "Jobs · 01 · the screens"),
    "frames": ("/01-00.html", "HotelOS design tokens"),
}


def free(port):
    with socket.socket() as probe:
        try:
            probe.bind(("127.0.0.1", port))
            return True
        except OSError:
            return False


def serve(root, port):
    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=root)
    server = http.server.ThreadingHTTPServer(("127.0.0.1", port), handler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def proves(port, name):
    path, marker = PROOF[name]
    try:
        with urllib.request.urlopen(f"http://127.0.0.1:{port}{path}", timeout=4) as answer:
            return marker in answer.read(20000).decode("utf-8", "replace")
    except Exception:
        return False


ports, at = {}, BASE
for name, root in ROOTS.items():
    while not free(at):
        at += 1
    serve(root, at)
    if not proves(at, name):
        print(f"{name}: {at} answered, but not with this root's own file — refusing", file=sys.stderr)
        raise SystemExit(2)
    print(f"{name:8} {at}  verified by identity")
    ports[name] = at
    at += 1

open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "ports.json"), "w").write(json.dumps(ports))
print("serving; ctrl-c ends it")
threading.Event().wait()
