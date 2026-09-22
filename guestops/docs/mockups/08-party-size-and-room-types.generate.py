"""Generate guestops/docs/mockups/08-party-size-and-room-types.html.

One owner decision, drawn three ways: what New booking does when the party is
larger than a room type sleeps. Built from the gold page's stylesheet exactly as
06 and 07 are, so the drawings and the module cannot disagree about a colour.

Three of the four room types, their counts and their occupancy are the review
data's own (`ui/book/recorded/availability.ts`); Superior Double is the approved
05 flow's fourth row, which the fixture does not carry. Nothing here is a number
invented for a drawing, and the two sources are named because they are two.
"""
import re

ROOT = r"C:\Users\Mahin Aboobakker\PycharmProjects\HotelOsApps\guestops\docs\mockups"
gold = open(ROOT + r"\01-guestops-gold.html", encoding="utf-8").read()
style = gold[gold.index("<style>") + len("<style>"):gold.index("</style>")]
style = re.sub(r"^\s*(--color-[a-z-]+|--radius-panel|--font-sans):[^;]*;", "", style, flags=re.M)
style = re.sub(r"/\* The three soft tones[^*]*\*/", "", style)
style = re.sub(r"/\* The fourteen published tokens[\s\S]*?\*/", "", style, count=1)

# name, sleeps-included, sleeps-most, total, sold, free
TYPES = [
    ("Deluxe King", 2, 3, 24, 19, 4),
    ("Deluxe Twin", 2, 2, 18, 18, 0),
    ("Executive Suite", 3, 5, 6, 2, 0),
    ("Superior Double", 2, 4, 12, 7, 5),
]

PARTY = 3  # two adults and a child — the count the first step already collects


def sleeps(included, most):
    return f"sleeps {included}, up to {most}" if most > included else f"sleeps {included}"


def row(name, included, most, total, sold, free, mode):
    """One room type's line, drawn as the module draws it."""
    fits = most >= PARTY
    cells = (f'<div>{total}</div><div>{sold}</div><div>0</div><div>0</div>'
             f'<div><b>{free}</b></div>')

    if mode == "hide" and not fits:
        return ""

    tone, note, control = "", "", ""

    if free == 0:
        control = '<span class="hint">none free</span>'
    elif fits:
        control = '<button class="btn sm">Choose</button>'
    elif mode == "mark":
        tone = " dim"
        note = (f'<span class="mark missing">sleeps {most} · '
                f'party of {PARTY}</span>')
        control = '<span class="hint">too small</span>'
    else:
        control = '<button class="btn sm">Choose</button>'

    return (f'<div class="tr list{tone}"><div class="nm"><b>{name}</b>'
            f'<span class="sleeps">{sleeps(included, most)}</span>{note}</div>'
            f'{cells}<div>{control}</div></div>')


def table(mode):
    head = ('<div class="tr list hd"><div>Room type</div><div>Total rooms</div>'
            '<div>Sold</div><div>Out of order</div><div>Held back</div>'
            '<div>Free</div><div></div></div>')
    lines = "".join(row(*t, mode) for t in TYPES)
    return f'<div class="card"><div class="cb"><div class="tbl">{head}{lines}</div></div></div>'


CONFIRM = '''<div class="card"><div class="ch">Create this booking?</div><div class="cb">
  <div class="fr"><div class="k">Room type</div><div class="v">Deluxe Twin · one room</div></div>
  <div class="fr"><div class="k">Guests</div><div class="v">2 adults, 1 child</div></div>
  <div class="note warn">This room type sleeps 2 and you have entered 3 guests.
  It can still be booked.</div>
</div></div>'''

OPTIONS = [
    ("A — hide what cannot sleep them",
     table("hide"),
     "Deluxe Twin is gone from the list. The desk cannot book a room too small "
     "for the party, and cannot see that the property has 18 of them either. "
     "<b>A row that is absent looks the same as a row that does not exist</b>, "
     "so a receptionist who wants to split a family across two twins finds "
     "nothing to split."),
    ("B — show them, marked",
     table("mark"),
     "Every type stays visible with what it sleeps. The one that is too small "
     "says so and offers no Choose. The desk sees the whole property and is "
     "stopped only where the room genuinely cannot take the party."),
    ("C — show them, warn at the end",
     table("plain") + CONFIRM,
     "Nothing is marked in the list; the warning appears on the confirm screen "
     "and the booking can still be made. The desk keeps every option, and a "
     "mistake is caught after the guest's details have been typed rather than "
     "before."),
]

cards = "\n".join(
    f"""<section class="item">
  <h2><span class="id">{chr(65 + n)}</span>{title}</h2>
  <div class="drawn">{drawn}</div>
  <p class="cost">{cost}</p>
</section>""" for n, (title, drawn, cost) in enumerate(OPTIONS))

page = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>Party Size And Room Types</title>
<!-- Published tokens at the shell's values; the frame classes are the gold page's
     stylesheet with its own copy of the published palette removed (page 64 §8). -->
<link rel="stylesheet" href="../../ui/preview/tokens.css">
<style>{style}
  body{{background:var(--color-surface);color:var(--color-ink);padding:34px 22px 80px}}
  .page{{max-width:1000px;margin:0 auto;display:flex;flex-direction:column;gap:32px}}
  .lede h1{{font-size:22px;font-weight:600;margin:0 0 8px}}
  .lede p, .cost{{max-width:78ch;color:var(--color-ink-muted);line-height:1.6;margin:0}}
  .item h2{{font-size:16px;font-weight:600;margin:0 0 10px;display:flex;align-items:center;gap:10px}}
  .id{{border:1px solid var(--color-line-strong);border-radius:6px;font-size:12px;
      padding:1px 8px;color:var(--color-ink-muted)}}
  .drawn{{margin:0 0 10px}}
  .tbl .tr.list{{grid-template-columns:2.4fr .8fr .6fr .8fr .8fr .6fr 1fr}}
  .tr.list.dim{{opacity:.55}}
  .sleeps{{color:var(--color-ink-faint);font-size:12px;margin-left:8px}}
  .note.warn{{border-left:2px solid var(--color-warn);padding-left:10px;margin-top:8px}}
  .ask{{color:var(--color-ink);font-size:14px}}
</style></head>
<body><div class="page">
<div class="lede">
  <h1>A party of three, and a room that sleeps two</h1>
  <p>New booking's first step asks how many guests. The room types now say how
  many each sleeps. <b>Nothing yet joins the two</b>, and what should happen is
  a decision about the desk rather than about the data: the party here is two
  adults and a child, and Deluxe Twin sleeps two.</p>
  <p>The numbers are real rather than drawn: three of these types, their counts
  and what they sleep are the review data GuestOps renders today, and Superior
  Double is the fourth row of the approved New booking flow. Deluxe King sleeps
  two and takes a third on an extra bed; Executive Suite would take the party
  and has none free.</p>
</div>
{cards}
<section class="item">
  <h2><span class="id">?</span>What I would choose, and why it is still yours</h2>
  <p class="cost"><b>B</b>, and A is the one I would argue against — which is why
  it is drawn first rather than described. Hiding a row removes the difference
  between <i>this property has none</i> and <i>this property has eighteen and I
  am not showing you</i>, and a desk splitting a family across two twin rooms
  needs to see them. C keeps every option too, but it spends the guest's details
  before saying anything, and the warning arrives where a person is committing
  rather than choosing.</p>
  <p class="ask">B also does not prevent the booking anywhere — it marks the row
  and withholds <i>Choose</i>. <b>If the desk must be able to book a too-small
  room anyway</b> — a family taking one room by choice — then B needs C's
  confirm warning behind it, and that is the fourth answer: say so and I will
  draw it.</p>
</section>
</div></body></html>
"""

open(ROOT + r"\08-party-size-and-room-types.html", "w", encoding="utf-8", newline="\n").write(page)
print("written", len(page))
