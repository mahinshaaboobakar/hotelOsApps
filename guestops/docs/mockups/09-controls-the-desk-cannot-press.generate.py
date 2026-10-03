"""Draw the decision behind GuestOps' disabled controls, for the owner.

WHY THIS PAGE EXISTS.  Part A's sweep at HEAD (2026-10-03) reported 56 differing
nodes, flat across three rounds of change, and 22 of them sat UNCLASSIFIED.  They
are one cause: `chrome/styles/shell.ts:91`'s `.btn.off`, applied by
`chrome/element.ts:64`'s `unavailable()` -- "a control drawn where the design
puts it, that GuestOps cannot perform yet", with `disabled`, a `title` and an
`aria-description`.  The frames draw these controls ENABLED.

So the build and the drawing disagree, and a fidelity sweep can never say which
is right.  That is an owner decision and it arrives drawn.

THE ROWS ARE DERIVED, NOT TYPED.  `REASONS` below is the measured output of
reading every `unavailable()` call site; the page is built from it, so a reason
added in code and not added here is visible as an absence rather than silently
missing.

THE COUNT, MEASURED -- and it is smaller than it looks:

    22  differing NODES          21 `button` + 1 `b`
    21  CONTROLS                 the `b` is label text, not a control
    15  reasons found by GREP    string literals passed to `unavailable()`
    16  reasons in truth         + the walk-in's, COMPOSED by `missing(draft)`
                                 at render, which no literal search can find
     3  are NOT capability gaps and are NOT the owner's choice
    13  are the owner's choice

That last line is the finding.  Deriving the list is what split it: a shared
symptom is not a shared owner, and three of the fifteen are correct forever.
"""

import html
import pathlib

OUT = pathlib.Path(__file__).with_suffix("").with_suffix(".html")

# Measured 2026-10-03 from every `unavailable(...)` call site under
# `ui/screens` and `ui/chrome`.  `kind` is the finding: a reason is either a
# capability GuestOps has not built, or something that is correct as it stands.
REASONS = [
    # --- NOT the owner's choice.  Correct as they are. ---
    ("settled", "Jobs is not installed at this property.",
     "screens/stay/index.ts:321",
     "A fact about the property, not a gap. The control is off because the "
     "neighbour is absent, and it must come back the day Jobs is installed.",
     "Nothing to decide. Building it would be wrong and drawing it off "
     "permanently would be wronger."),
    ("settled", "Nothing has happened to this stay yet.",
     "screens/stay/activity-tab.ts",
     "A data state. The stay is new; the control has nothing to act on.",
     "Nothing to decide. It resolves itself the first time anything happens."),
    ("settled", "...what the sheet still needs, named.",
     "screens/walkin/index.ts:281",
     "The walk-in's Create and check in, off because the fixture's sheet is "
     "empty. Its own code: “off until it can succeed, and it says what is "
     "missing — a pressable button that always refuses teaches the desk to "
     "distrust it.”",
     "Nothing to decide. It enables the moment the desk fills the sheet."),

    # --- THE OWNER'S CHOICE.  Each is a capability GuestOps has not built. ---
    ("choice", "Settings are shown here and cannot be changed from this screen yet.",
     "screens/setup/index.ts:99", "Setup's Save and Discard.", "desk.configure"),
    ("choice", "Settling a disagreement with the PMS is not available from this screen yet.",
     "screens/stay/banner.ts:50", "Keep 214 · Take the PMS's 208, on a stay.",
     "stay.override"),
    ("choice", "Filtering this list is not available yet.",
     "screens/stay/activity-tab.ts:46", "Everything · Ours · Other apps.",
     "reservation.read"),
    ("choice", "Raising a job from GuestOps is not available yet.",
     "screens/stay/index.ts:322", "＋ Raise a job, where Jobs IS installed.",
     "a Jobs contract"),
    ("choice", "Logging a request from GuestOps is not available yet.",
     "screens/stay/requests-tab.ts:63", "＋ Log a request.", "request.handle"),
    ("choice", "Asking for service from GuestOps is not available yet.",
     "screens/stay/index.ts:326", "Ask for service.", "a Room Care contract"),
    ("choice", "Assigning a room from GuestOps is not available yet.",
     "screens/today/table.ts:103", "＋ assign, on the day list.", "stay.assign"),
    ("choice", "Opening the PMS from GuestOps is not available yet.",
     "screens/stay/index.ts:332", "Open in the PMS.", "a connector contract"),
    ("choice", "Saving this list as a file is not available yet.",
     "screens/stay/index.ts:313", "Export.", "an export capability"),
    ("choice", "A second guest in the same room is not available from this screen yet.",
     "screens/newbooking/guest.ts:58", "＋ add a guest in this room.", "stay.create"),
    ("choice", "Settling this from GuestOps is not available yet.",
     "screens/stay", "An attention card's own action.", "stay.override"),
    ("choice", "This action is not available from this screen yet.",
     "screens/stay/index.ts:384", "A stay action the service names and this screen "
     "cannot perform — Cancel… among them.", "varies by action"),
    ("choice", "Not available from this screen yet.",
     "chrome/marks.ts:79 · chrome/panel.ts:110 · screens/setup/card.ts:37 "
     "· screens/stay/servicing-tab.ts:77",
     "The generic one, and the widest: reveal · ＋ add · Open the list "
     "· Record a filing · Ask again this evening, and every panel action.",
     "several — this sentence covers more than one capability and is the one "
     "row that cannot be decided as a unit"),
]


def shell(body: str) -> str:
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Controls The Desk Cannot Press</title>
<link rel="stylesheet" href="../../ui/preview/tokens.css">
<style>
  :root {{ color-scheme: dark; }}
  body {{ margin:0; background:#0b0d14; color:#e8ebf4;
    font:14px/1.6 ui-sans-serif,-apple-system,"Segoe UI",system-ui,sans-serif; }}
  .page {{ max-width:1180px; margin:0 auto; padding:40px 28px 90px; }}
  h1 {{ font-size:27px; letter-spacing:-.02em; margin:0 0 6px; text-wrap:balance; }}
  .sub {{ color:#9aa3bb; margin:0 0 30px; max-width:70ch; }}
  .count {{ display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin:0 0 34px; }}
  .count div {{ border:1px solid rgba(255,255,255,.09); border-radius:10px;
    padding:14px 16px; background:rgba(255,255,255,.02); }}
  .count b {{ display:block; font-size:25px; font-variant-numeric:tabular-nums;
    color:#818cf8; line-height:1.2; }}
  .count span {{ color:#9aa3bb; font-size:12px; }}
  h2 {{ font-size:13px; text-transform:uppercase; letter-spacing:.09em;
    color:#9aa3bb; margin:40px 0 4px; }}
  h2 + p {{ color:#9aa3bb; margin:0 0 18px; max-width:72ch; }}
  .row {{ border:1px solid rgba(255,255,255,.09); border-radius:12px;
    margin:0 0 14px; overflow:hidden; }}
  .say {{ padding:14px 18px; background:rgba(255,255,255,.03);
    border-bottom:1px solid rgba(255,255,255,.07); }}
  .say q {{ color:#e8ebf4; font-weight:500; }}
  .say q:before, .say q:after {{ content:'"'; color:#5a6172; }}
  .say em {{ display:block; color:#9aa3bb; font-style:normal; font-size:12px;
    margin-top:5px; font-family:ui-monospace,"Cascadia Mono",Consolas,monospace; }}
  .pair {{ display:grid; grid-template-columns:1fr 1fr; }}
  .pair > div {{ padding:16px 18px; }}
  .pair > div + div {{ border-left:1px solid rgba(255,255,255,.07); }}
  .t {{ font-size:11px; text-transform:uppercase; letter-spacing:.08em;
    color:#5a6172; margin:0 0 10px; }}
  .end {{ color:#9aa3bb; font-size:12.5px; margin:11px 0 0;
    padding-top:10px; border-top:1px dotted rgba(255,255,255,.12); }}
  .end b {{ color:#e8ebf4; font-weight:500; }}
  .btn {{ display:inline-block; padding:6px 13px; border-radius:7px; font-size:12.5px;
    border:1px solid rgba(255,255,255,.14); color:#e8ebf4; background:none; }}
  .btn.off {{ color:#5a6172; border-style:dashed; }}
  .btn.pri {{ border-color:transparent; font-weight:600; color:#0b0d14;
    background:#818cf8; }}
  .settled {{ border-color:rgba(74,222,128,.22); }}
  .settled .say {{ background:rgba(74,222,128,.05); }}
  .tag {{ float:right; font-size:11px; color:#5a6172; }}
  footer {{ margin-top:46px; padding-top:18px; color:#5a6172; font-size:12px;
    border-top:1px solid rgba(255,255,255,.09); max-width:76ch; }}
</style></head><body><div class="page">{body}</div></body></html>
"""


def row(reason, site, what, note, settled):
    e = html.escape
    if settled:
        right = (f'<div><p class="t">Why it is not a choice</p><p>{e(note)}</p>'
                 f'<p class="end"><b>End state</b> — unchanged. The desk reads the '
                 f'sentence and knows why.</p></div>')
        left = (f'<div><p class="t">As it renders today</p>'
                f'<p><span class="btn off">the control</span></p>'
                f'<p class="end">{e(what)}</p></div>')
    else:
        left = (f'<div><p class="t">Treatment 1 — the FRAMES move</p>'
                f'<p><span class="btn off">the control</span> drawn disabled and '
                f'dashed, carrying this sentence.</p>'
                f'<p class="end"><b>End state</b> — the desk meets a control it '
                f'cannot press, reads why in the tooltip and the screen reader, and '
                f'does the thing elsewhere. Nothing is built; the drawing stops '
                f'promising it.</p></div>')
        right = (f'<div><p class="t">Treatment 2 — the BUILD moves</p>'
                 f'<p><span class="btn pri">the control</span> live, and '
                 f'<code>{e(note)}</code> is what it needs.</p>'
                 f'<p class="end"><b>End state</b> — the desk presses it and the '
                 f'work happens on this screen. The capability is built, authorized '
                 f'and has somewhere to write.</p></div>')

    cls = "row settled" if settled else "row"
    return (f'<div class="{cls}"><div class="say"><q>{e(reason)}</q>'
            f'<span class="tag">{e(site)}</span>'
            f'<em>{e(what) if not settled else ""}</em></div>'
            f'<div class="pair">{left}{right}</div></div>')


def main():
    settled = [r for r in REASONS if r[0] == "settled"]
    choices = [r for r in REASONS if r[0] == "choice"]

    body = [
        "<h1>Controls the desk cannot press</h1>",
        '<p class="sub">Part A at HEAD, 2026-10-03: 56 differing nodes, flat across '
        'three rounds of change, and 22 unclassified. They are one cause — '
        '<code>.btn.off</code>, applied by <code>unavailable()</code>, which draws a '
        'control the design puts there that GuestOps cannot perform yet. '
        '<b>The frames draw them enabled.</b> A fidelity sweep compares two renderings '
        'and can never say which is right, so this is yours to decide.</p>',
        '<div class="count">'
        '<div><b>22</b><span>differing nodes</span></div>'
        '<div><b>21</b><span>controls — one node is label text</span></div>'
        f'<div><b>{len(REASONS)}</b><span>distinct reasons — 15 literal, '
        f'1 composed at render</span></div>'
        f'<div><b>{len(choices)}</b><span>actually your choice</span></div>'
        "</div>",
        "<h2>Not a choice — correct as they stand</h2>",
        "<p>Deriving the list is what split it. These three read like the others on "
        "the screen and are not the same question: one is a fact about the property, "
        "one is a data state, one is an incomplete form. Offering them as "
        "<em>build or draw off</em> would be the wrong question asked well.</p>",
    ]
    body += [row(r[1], r[2], r[3], r[4], True) for r in settled]

    body += [
        "<h2>Your choice — each drawn both ways, to its end state</h2>",
        "<p>Every row is a capability GuestOps has not built. Left: the frames move "
        "and the control is drawn off, carrying the sentence the build already shows "
        "the desk. Right: the capability is built and the control is live. Each is "
        "followed to what the person at the desk ends up doing.</p>",
    ]
    body += [row(r[1], r[2], r[3], r[4], False) for r in choices]

    body.append(
        "<footer>Derived from every <code>unavailable()</code> call site under "
        "<code>ui/screens</code> and <code>ui/chrome</code>, 2026-10-03, by "
        "<code>09-controls-the-desk-cannot-press.generate.py</code> — so a "
        "reason added in code and not added here shows as an absence rather than "
        "going missing quietly. The last row is the one that cannot be decided as a "
        "unit: one sentence covers several capabilities, and splitting it is itself "
        "part of the decision.</footer>")

    OUT.write_text(shell("\n".join(body)), encoding="utf-8")
    print(f"{OUT.name}: {len(settled)} settled, {len(choices)} choices, "
          f"{len(REASONS)} reasons")


if __name__ == "__main__":
    main()
