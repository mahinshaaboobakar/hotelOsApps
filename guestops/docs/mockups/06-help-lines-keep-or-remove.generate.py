"""Generate guestops/docs/mockups/06-help-lines-keep-or-remove.html."""
import re

ROOT = r"C:\Users\Mahin Aboobakker\PycharmProjects\HotelOsApps\guestops\docs\mockups"
gold = open(ROOT + r"\01-guestops-gold.html", encoding="utf-8").read()
style = gold[gold.index("<style>") + len("<style>"):gold.index("</style>")]
style = re.sub(r"^\s*(--color-[a-z-]+|--radius-panel|--font-sans):[^;]*;", "", style, flags=re.M)
style = re.sub(r"/\* The three soft tones[^*]*\*/", "", style)
style = re.sub(r"/\* The fourteen published tokens[\s\S]*?\*/", "", style, count=1)

LOCK = lambda t: f'<span class="lock">{t}</span>'

ITEMS = [
    ("Stay — tags beside the stay's values",
     "Small tags that say where a value came from or how long it lasts.",
     f"""<div class="card"><div class="cb">
       <div class="fr"><div class="k">Arrival</div><div class="v">31 Aug <b>14:10</b> {LOCK("OBSERVED")}</div></div>
       <div class="fr"><div class="k">Departure</div><div class="v">4 Sep <b>11:00</b> {LOCK("DERIVED FROM PROPERTY CLOCK")}</div></div>
       <div class="fr"><div class="k">Room type</div><div class="v">Deluxe King {LOCK("FROM THE PMS")}</div></div>
       <div class="fr"><div class="k">Contact</div><div class="v">+91 98470 •••• 12 {LOCK("MOBILE · PRIMARY")}</div></div>
       <div class="fr"><div class="k">Preferences</div><div class="v">High floor, away from the lift {LOCK("GUEST · CARRIES TO NEXT STAY")}</div></div>
       <div class="fr"><div class="k">Note</div><div class="v">Anniversary — cake sent 31 Aug {LOCK("THIS STAY ONLY")}</div></div>
     </div></div>""",
     "Each tag can be kept or removed on its own — say which."),
    ("Setup — guest reporting, the tags",
     "Tags beside three of the reporting settings.",
     f"""<div class="card"><div class="cb">
       <div class="fr"><div class="k">Applies to</div><div class="v">Guests from outside the home country {LOCK("OR EVERY GUEST")}</div></div>
       <div class="fr"><div class="k">Home country</div><div class="v"><b>India</b> {LOCK("DECIDES WHO IS “FROM OUTSIDE”")}</div></div>
       <div class="fr"><div class="k">How it is sent</div><div class="v"><span class="lock no">BY A PERSON, ON THE AUTHORITY'S PORTAL</span></div></div>
     </div></div>""",
     "Each tag on its own."),
    ("Setup — due to file, the line under the list",
     "A sentence under the list of guests whose report is due.",
     """<div class="card"><div class="cb">
       <div class="fr"><div class="k">Chen Wei</div><div class="v">arrived 29 Aug · <span class="pill bad">overdue</span></div></div>
       <div class="hint">Overdue is shown, never enforced. Chen Wei is checked in, served and will check out on time — the platform says what is owed and stops nothing.</div>
     </div></div>""",
     "Keep, remove, or keep only the first sentence."),
    ("Attention — why a possible duplicate is here",
     "The sentence under a stay that may be the same as one the PMS sent.",
     """<div class="card"><div class="ch">Same stay, or two?</div><div class="cb">
       <div class="note">Same room and overlapping dates is what raised this. The names only ordered the list — they can never join two stays. Until you decide, the PMS's version is held and applied to nothing.</div>
     </div></div>""",
     "Keep, remove, or keep only the first and last sentences."),
    ("Today and New booking — the line under the title",
     "The line under “New booking”, and after the business day on Today.",
     """<div class="card"><div class="cb">
       <div class="ht" style="font-size:16px">New booking</div>
       <div class="hsub">Standalone — this property is the book</div>
       <div class="hsub" style="margin-top:10px">Connected to the PMS — the PMS writes the lifecycle</div>
     </div></div>""",
     "A property shows one of the two. Keep, remove, or reword."),
]

cards = []
for title, where, drawn, ask in ITEMS:
    cards.append(f"""<section class="item">
  <h2>{title}</h2>
  <p class="says">{where}</p>
  <div class="drawn">{drawn}</div>
  <div class="choice"><span class="opt">Keep</span><span class="opt">Remove</span><span class="ask">{ask}</span></div>
</section>""")

page = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>Help Lines Keep Or Remove</title>
<!-- Published tokens at the shell's values; the frame classes are the gold page's
     stylesheet with its own copy of the published palette removed (page 64 §8). -->
<link rel="stylesheet" href="../../ui/preview/tokens.css">
<style>{style}
  body{{background:var(--color-surface);color:var(--color-ink);padding:34px 22px 80px}}
  .page{{max-width:900px;margin:0 auto;display:flex;flex-direction:column;gap:30px}}
  .lede h1{{font-size:22px;font-weight:600;margin:0 0 8px}}
  .lede p, .says{{max-width:75ch;color:var(--color-ink-muted);line-height:1.6;margin:0 0 10px}}
  .item h2{{font-size:16px;font-weight:600;margin:0 0 4px}}
  .drawn{{margin:8px 0 10px}}
  .choice{{display:flex;gap:10px;align-items:center;flex-wrap:wrap}}
  .opt{{border:1px solid var(--color-line-strong);border-radius:8px;padding:4px 14px;font-size:13px}}
  .ask{{color:var(--color-ink-faint);font-size:13px}}
</style></head>
<body><div class="page">
<div class="lede">
  <h1>Help lines — keep or remove?</h1>
  <p>These lines are on GuestOps screens today. Each could be help for the person at the desk, or a note that was meant for the developer and should not be on a screen. It was not clear which, so none of them has been changed. For each, say <b>keep</b> or <b>remove</b>.</p>
</div>
{chr(10).join(cards)}
</div></body></html>
"""
open(ROOT + r"\06-help-lines-keep-or-remove.html", "w", encoding="utf-8", newline="\n").write(page)
print("written", len(page))
