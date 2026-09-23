"""Generate guestops/docs/mockups/08-party-size-and-room-types.html.

One owner decision, drawn three ways: what New booking does when the party is
larger than a room type sleeps. Built from the gold page's stylesheet exactly as
06 and 07 are, so the drawings and the module cannot disagree about a colour.

Three of the four room types and their occupancy are the review data's own
(`ui/book/recorded/availability.ts`); Superior Double is the approved 05 flow's
fourth row, which the fixture does not carry.

**Deluxe Twin's stock is CHANGED from the fixture, deliberately.** It is sold
out there (18 of 18), and a sold-out row draws "none free" in every treatment —
so the first edition of this page sent the owner two byte-identical tables for
B and C and asked them to compare. The too-small type must have rooms free or
there is nothing for the treatments to differ about. `assert_distinct` below
checks that, because an eye cannot.
"""
import re
from hashlib import sha256

ROOT = r"C:\Users\Mahin Aboobakker\PycharmProjects\HotelOsApps\guestops\docs\mockups"
gold = open(ROOT + r"\01-guestops-gold.html", encoding="utf-8").read()
style = gold[gold.index("<style>") + len("<style>"):gold.index("</style>")]
style = re.sub(r"^\s*(--color-[a-z-]+|--radius-panel|--font-sans):[^;]*;", "", style, flags=re.M)
style = re.sub(r"/\* The three soft tones[^*]*\*/", "", style)
style = re.sub(r"/\* The fourteen published tokens[\s\S]*?\*/", "", style, count=1)

# name, sleeps-included, sleeps-most, total, sold, free
TYPES = [
    ("Deluxe King", 2, 3, 24, 19, 4),
    ("Deluxe Twin", 2, 2, 18, 12, 6),
    ("Executive Suite", 3, 5, 6, 2, 0),
    ("Superior Double", 2, 4, 12, 7, 5),
]

PARTY = 3  # two adults and a child — the count the first step already collects


def sleeps(included, most):
    return f"sleeps {included}, up to {most}" if most > included else f"sleeps {included}"


def row(name, included, most, total, sold, free, mode):
    """One room type's line, drawn as the module draws it.

    The mark follows whether the party FITS; the control follows the stock. The
    first edition tested the stock first, so a sold-out too-small row could
    never be marked — B's whole distinguishing feature was unreachable rather
    than merely unseen.
    """
    fits = most >= PARTY
    marks = not fits and mode in ("mark", "both")
    cells = (f'<div>{total}</div><div>{sold}</div><div>0</div><div>0</div>'
             f'<div><b>{free}</b></div>')

    if mode == "hide" and not fits:
        return ""

    note = (f'<span class="mark missing">sleeps {most} · party of {PARTY}</span>'
            if marks else "")

    if free == 0:
        control = '<span class="hint">none free</span>'
    elif mode == "mark" and not fits:
        # B alone withholds it: marked AND unchoosable.
        control = '<span class="hint">too small</span>'
    else:
        control = '<button class="btn sm">Choose</button>'

    return (f'<div class="tr list{" dim" if marks else ""}"><div class="nm"><b>{name}</b>'
            f'<span class="sleeps">{sleeps(included, most)}</span>{note}</div>'
            f'{cells}<div>{control}</div></div>')


def table(mode):
    head = ('<div class="tr list hd"><div>Room type</div><div>Total rooms</div>'
            '<div>Sold</div><div>Out of order</div><div>Held back</div>'
            '<div>Free</div><div></div></div>')
    lines = "".join(row(*t, mode) for t in TYPES)
    return f'<div class="card"><div class="cb"><div class="tbl">{head}{lines}</div></div></div>'


def assert_distinct(tables):
    """Four treatments must be four pictures — the check the first edition lacked.

    B and C went to the owner byte-identical (SHA-256 e138677e...), because the
    only type they treat differently was sold out and drew "none free" in both.
    The owner compared two copies of one image and asked, correctly, where the
    warning was. A hash is a one-line check and would have caught it with
    nobody's eye involved.
    """
    digests = {name: sha256(html.encode("utf-8")).hexdigest()[:16] for name, html in tables}

    for name, digest in digests.items():
        print(f"  {name} table {digest}")

    duplicates = [n for n, d in digests.items()
                  if list(digests.values()).count(d) > 1]
    if duplicates:
        raise SystemExit(
            f"REFUSED: {', '.join(duplicates)} draw the same table — a reader "
            "comparing them is comparing nothing")


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
    ("D — marked in the list AND warned at confirm",
     table("both") + CONFIRM,
     "Both halves: the row says it is too small, <b>and it can still be "
     "chosen</b>, and the confirm screen says so again before anything is "
     "written. <b>The trade is the one this is really choosing: D makes the "
     "mistake visible twice and prevents it neither time.</b> That is the right "
     "answer if a too-small room is a legitimate booking — a family taking one "
     "room by choice — and the wrong one if the desk should be stopped, because "
     "two warnings that stop nothing are two warnings a busy desk learns to "
     "click past. B is the only treatment here that prevents; C and D both "
     "inform, at different moments."),
]

assert_distinct([
    ("A", table("hide")),
    ("B", table("mark")),
    ("C", table("plain")),
    ("D", table("both")),
])

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
  <h2><span class="id">?</span>What changed since the first edition</h2>
  <p class="cost"><b>You said B and C looked the same, and they were</b> — the
  same picture twice. Deluxe Twin was sold out in the data this page drew, so
  the one type the treatments handle differently showed <i>none free</i> in
  every one of them, and B's mark could not appear at all. It now has six rooms
  free, so the difference is visible rather than argued.</p>
  <p class="cost"><b>D is drawn because you asked for both halves</b>, and it is
  the answer if a too-small room is a booking a desk should be able to make. The
  cost is stated beside it rather than buried: it warns twice and stops nothing.
  <b>Of the four, only B prevents</b>; A prevents by removing the row, which
  also removes the eighteen twins from the desk's sight.</p>
  <p class="ask">Say A, B, C or D — or B with D's confirm warning, which is the
  shape if a too-small booking should be possible but never accidental.</p>
</section>
</div></body></html>
"""

open(ROOT + r"\08-party-size-and-room-types.html", "w", encoding="utf-8", newline="\n").write(page)
print("written", len(page))
