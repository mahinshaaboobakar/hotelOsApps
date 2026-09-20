"""Generate guestops/docs/mockups/07-booking-heading-and-dates.html.

Two owner decisions, drawn: what a group booking's heading says, and how a date
range reads. Built from the gold page's stylesheet exactly as page 06 is, so the
drawings and the module cannot disagree about a colour.

Every string in the drawings is what the code produces — the spans came out of
`Intl.DateTimeFormat` at `en-IN` and `en-US`, not out of memory.
"""
import re

ROOT = r"C:\Users\Mahin Aboobakker\PycharmProjects\HotelOsApps\guestops\docs\mockups"
gold = open(ROOT + r"\01-guestops-gold.html", encoding="utf-8").read()
style = gold[gold.index("<style>") + len("<style>"):gold.index("</style>")]
style = re.sub(r"^\s*(--color-[a-z-]+|--radius-panel|--font-sans):[^;]*;", "", style, flags=re.M)
style = re.sub(r"/\* The three soft tones[^*]*\*/", "", style)
style = re.sub(r"/\* The fourteen published tokens[\s\S]*?\*/", "", style, count=1)


def head(title, sub, mark=None):
    """A screen's own heading block — the title and the line under it."""
    pms = f'<span class="mark pms">{mark}</span>' if mark else ""
    return f"""<div class="card"><div class="cb">
      <div class="ht" style="font-size:16px">{title}</div>
      <div class="hsub">{sub}{pms}</div>
    </div></div>"""


ITEMS = [
    {
        "id": "A",
        "title": "A group booking's heading — what should the line say?",
        "where": (
            "Frame 9, the booking that came from the PMS as a group: three rooms "
            "claimed, one sent. The line under the guest's name."
        ),
        "note": (
            "<b>The frame drew a line the platform cannot produce.</b> It read "
            "<i>Group 84119377 · from the PMS · booked 28 Aug</i>, and the service has "
            "only ever sent a count and the dates. Nobody noticed because the sample data "
            "behind the review screens had the drawn sentence written into it by hand. That "
            "sample now carries what the service really sends, so this is a decision rather "
            "than a defect: the "
            "heading can stay as the platform can fill it, or the three missing facts "
            "can be built."
        ),
        "options": [
            ("What the platform sends today",
             head("Rajesh Pillai", "One stay · 31 Aug → 02 Sept", "The PMS manages this booking"),
             "Nothing to build. The confirmation number is on the row below and on the list."),
            ("With the confirmation number",
             head("Rajesh Pillai", "84119377 · One stay · 31 Aug → 02 Sept", "The PMS manages this booking"),
             "Small work: the view already reads the number, it is not put on this line."),
            ("The line as it was drawn",
             head("Rajesh Pillai", "Group 84119377 · from the PMS · booked 28 Aug"),
             "More work, and it drops the dates: the booked date is not read here yet, and "
             "<i>from the PMS</i> needs the connected system's name, which is still an open "
             "question (the integration id is held; the name is not)."),
        ],
        "ask": "Which line — or a mix, and say which parts.",
    },
    {
        "id": "B",
        "title": "A stay's dates — two days, or the shorter range?",
        "where": (
            "Everywhere a stay's span is drawn: the lists, a booking's stays, and the "
            "cancellation dialog."
        ),
        "note": (
            "<b>The drawings used the short form and the build now uses the long one.</b> "
            "A range like <i>3 – 7 September</i> puts the month at the end because that is "
            "how English is written here; a property reading American English writes the same "
            "two days <i>Sep 3 – 7</i>. The service used to write the short form itself, in "
            "one language, so it moved to the screen as two plain days. <b>The short form is "
            "available correctly</b> — the browser knows each language's own way of joining a "
            "range — and the strings below are what it actually produces."
        ),
        "options": [
            ("Two days, as built today",
             head("Fatima Sheikh", "Two stays · 03 Sept → 07 Sept"),
             "Already built. Always right, and longer."),
            ("The short range, per language",
             head("Fatima Sheikh", "Two stays · 3–7 Sept"),
             "A small addition to the shared formatting, then every screen gets it. "
             "The same booking reads <i>Sep 3 – 7</i> for a property set to American "
             "English, and <i>31 Aug – 2 Sept</i> when it crosses a month."),
        ],
        "ask": "Short or long — the same choice applies to every screen.",
    },
]

cards = []
for item in ITEMS:
    options = "\n".join(
        f"""<div class="opt-col">
      <div class="opt-name">{name}</div>
      {drawn}
      <p class="cost">{cost}</p>
    </div>"""
        for name, drawn, cost in item["options"])

    cards.append(f"""<section class="item">
  <h2><span class="id">{item['id']}</span>{item['title']}</h2>
  <p class="says">{item['where']}</p>
  <p class="says">{item['note']}</p>
  <div class="opts">{options}</div>
  <p class="ask">{item['ask']}</p>
</section>""")

page = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>Booking Heading And Dates</title>
<!-- Published tokens at the shell's values; the frame classes are the gold page's
     stylesheet with its own copy of the published palette removed (page 64 §8). -->
<link rel="stylesheet" href="../../ui/preview/tokens.css">
<style>{style}
  body{{background:var(--color-surface);color:var(--color-ink);padding:34px 22px 80px}}
  .page{{max-width:1040px;margin:0 auto;display:flex;flex-direction:column;gap:34px}}
  .lede h1{{font-size:22px;font-weight:600;margin:0 0 8px}}
  .lede p, .says{{max-width:78ch;color:var(--color-ink-muted);line-height:1.6;margin:0 0 10px}}
  .item h2{{font-size:16px;font-weight:600;margin:0 0 4px;display:flex;align-items:center;gap:10px}}
  .id{{border:1px solid var(--color-line-strong);border-radius:6px;font-size:12px;
      padding:1px 8px;color:var(--color-ink-muted)}}
  .opts{{display:flex;gap:18px;flex-wrap:wrap;margin:14px 0 6px}}
  .opt-col{{flex:1 1 300px;min-width:290px;display:flex;flex-direction:column;gap:6px}}
  .opt-name{{font-size:13px;font-weight:600}}
  .cost{{color:var(--color-ink-faint);font-size:13px;line-height:1.55;margin:0}}
  .ask{{color:var(--color-ink);font-size:13px;margin:6px 0 0}}
</style></head>
<body><div class="page">
<div class="lede">
  <h1>Two lines to decide</h1>
  <p>Both came out of the same piece of work: the service used to write finished
  sentences, and the screens now build them from the values it sends, so that a
  property in another language reads its own. Two sentences need a decision
  rather than a translation. Each option below is drawn; the words in them are
  what the build produces, not a sketch.</p>
</div>
{chr(10).join(cards)}
</div></body></html>
"""

open(ROOT + r"\07-booking-heading-and-dates.html", "w", encoding="utf-8", newline="\n").write(page)
print("written", len(page))
