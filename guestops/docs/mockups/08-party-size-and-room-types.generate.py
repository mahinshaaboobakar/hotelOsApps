"""Generate guestops/docs/mockups/08-party-size-and-room-types.html.

One owner decision, drawn four ways and FOLLOWED TO THE END STATE: what New
booking does when the party is larger than a room type sleeps. Built from the
gold page's stylesheet exactly as 06 and 07 are, so the drawings and the module
cannot disagree about a colour.

# Two checks this generator runs on itself, and why each exists

`assert_distinct` refuses to write when two treatments draw the same table. The
first edition sent B and C byte-identical: Deluxe Twin was sold out, so the one
type the treatments handle differently drew *none free* in all of them — and
`row` tested the stock before the fit, so B's mark could not appear at all. The
owner compared two copies of one picture.

`assert_answerable` refuses to write when a card ASKS something and offers no
control that answers it. The second edition drew "Create this booking?" with no
button, so the owner asked how a room is booked after a warning and the frame
had no answer. That is the owner's standing rule of 2026-09-23, `5c817674`:
*"when a mock design must complete the flow, then only understand"* — depth, as
distinct from drawing every option, which is breadth.

# What the data is, and where each row comes from

Three of the four room types and their occupancy are the review data's own
(`ui/book/recorded/availability.ts`); Superior Double is the approved 05 flow's
fourth row, which the fixture does not carry. **Deluxe Twin's stock is changed
from the fixture deliberately** — it is sold out there, and a sold-out row draws
*none free* in every treatment, which is what made two of them one picture.
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
    """One room type's line. The mark follows the FIT; the control follows the stock."""
    fits = most >= PARTY
    marks = not fits and mode in ("mark", "both")
    cells = (f'<div>{total}</div><div>{sold}</div><div>0</div><div>0</div>'
             f'<div><b>{free}</b></div>')

    if mode == "hide" and not fits:
        return ""

    note = (f'<span class="mark missing">sleeps {most} \u00b7 party of {PARTY}</span>'
            if marks else "")

    if free == 0:
        control = '<span class="hint">none free</span>'
    elif mode == "mark" and not fits:
        control = '<span class="hint">too small</span>'   # B alone withholds it
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


def confirm(warned):
    """The confirm screen, with the controls that answer it."""
    warning = ('<div class="note warn">This room type sleeps 2 and you have '
               'entered 3 guests. It can still be booked.</div>' if warned else "")

    return f'''<div class="card"><div class="ch">Create this booking?</div><div class="cb">
      <div class="fr"><div class="k">Guest</div><div class="v">Fatima Sheikh · +91 98470 11234</div></div>
      <div class="fr"><div class="k">Room type</div><div class="v">Deluxe Twin · one room</div></div>
      <div class="fr"><div class="k">Dates</div><div class="v">03 Sept → 07 Sept 2026 · 4 nights</div></div>
      <div class="fr"><div class="k">Guests</div><div class="v">2 adults, 1 child</div></div>
      {warning}
      <div class="row"><button class="btn">Back</button>
      <button class="btn pri">Create booking</button></div>
    </div></div>'''


def created(recorded):
    """Where the desk ends up: the booking, open."""
    note = ('<div class="fr"><div class="k">Recorded</div><div class="v">'
            'Booked over the room type\'s capacity — 3 guests in a type that '
            'sleeps 2 <span class="mark missing">on the stay</span></div></div>'
            if recorded else "")

    return f'''<div class="card"><div class="ch">Fatima Sheikh · booking created here</div><div class="cb">
      <div class="fr"><div class="k">Stay</div><div class="v">Deluxe Twin · 03 Sept → 07 Sept 2026 · Booked</div></div>
      <div class="fr"><div class="k">Room</div><div class="v">assigned at check-in</div></div>
      {note}
      <div class="hint">It is in Bookings and on the arrivals list for 03 Sept.</div>
    </div></div>'''


def blocked():
    """B's end for a desk that wanted the twin: there isn't one."""
    return '''<div class="card"><div class="cb">
      <div class="note">Deluxe Twin cannot be chosen, so this booking cannot be
      made here at all. The desk takes a larger type, splits the party across two
      rooms — <b>which this flow has no step for</b> — or turns the guest away.</div>
    </div></div>'''


def assert_distinct(tables):
    """Four treatments must be four pictures.

    B and C went to the owner byte-identical. A hash apiece is a one-line check
    and would have caught it with nobody's eye involved.
    """
    digests = {name: sha256(html.encode("utf-8")).hexdigest()[:16] for name, html in tables}

    for name, digest in digests.items():
        print(f"  {name} flow {digest}")

    repeated = [n for n, d in digests.items() if list(digests.values()).count(d) > 1]
    if repeated:
        raise SystemExit(
            f"REFUSED: {', '.join(repeated)} draw the same thing — a reader "
            "comparing them is comparing nothing")


def assert_answerable(html):
    """A card that ASKS carries a control that answers it — the owner's rule.

    The second edition drew `Create this booking?` with no button of any kind,
    so the flow stopped at the question and the owner had to ask how a room gets
    booked after a warning. Cheaper than `assert_distinct` and it would have
    caught this before they did, twice.
    """
    # By CARD rather than by a body pattern: the first attempt matched
    # `<div class="cb">(.*?)</div></div>` and a non-greedy body stops at the
    # first nested pair, which is a field row — so it read every confirm card as
    # buttonless and refused three pages that were fine. A detector is a
    # hypothesis about the fault's shape, and that one was wrong in the safe
    # direction. Splitting on the card is what the rule is actually about.
    unanswerable = [
        re.search(r'<div class="ch">([^<]*)</div>', card).group(1)
        for card in html.split('<div class="card">')[1:]
        if re.search(r'<div class="ch">[^<]*\?</div>', card) and "<button" not in card]

    if unanswerable:
        raise SystemExit(
            "REFUSED: a card asks and offers nothing that answers it — "
            + "; ".join(unanswerable))


FLOWS = [
    ("A — hide what cannot sleep them",
     [table("hide"), confirm(False), created(False)],
     "Deluxe Twin is gone, so the flow that follows is the ordinary one: a "
     "bigger type, no warning, a booking. <b>The mistake is impossible and so "
     "is the deliberate case</b> — a receptionist who wanted two twins for a "
     "family finds neither the rooms nor the fact that eighteen exist. Nothing "
     "on the screen distinguishes <i>this property has none</i> from <i>this "
     "property is not showing you</i>."),

    ("B — show them, marked, and not choosable",
     [table("mark"), blocked()],
     "The row says why it cannot be taken, and there is no confirm screen in "
     "this flow because there is nothing to confirm. <b>This is the only "
     "treatment that prevents</b>, and the end state is the price: a desk that "
     "genuinely wants that room has nowhere to go from here."),

    ("C — show them, warn at the confirm",
     [table("plain"), confirm(True), created(False)],
     "The list says nothing; the warning arrives after the guest's details are "
     "typed, and <b>Create booking</b> still creates it. The booking that "
     "results is an ordinary one — <b>nothing on it says the warning was ever "
     "shown</b>."),

    ("D — marked in the list AND warned at the confirm",
     [table("both"), confirm(True), created(True)],
     "Both halves, and the end state is where D differs from C rather than in "
     "the mark: the stay carries that it was booked over the type's capacity. "
     "<b>D makes the mistake visible twice and prevents it neither time</b> — "
     "which is right if a too-small room is a booking a desk should be able to "
     "make, and wrong if the desk should be stopped, because two warnings that "
     "stop nothing are two a busy desk learns to click past."),
]

assert_distinct([(name, "".join(cards)) for name, cards, _ in FLOWS])

sections = []
for index, (title, cards, cost) in enumerate(FLOWS):
    steps = "".join(
        f'<div class="step"><div class="sn">{n + 1}</div>{card}</div>'
        for n, card in enumerate(cards))

    sections.append(f"""<section class="item">
  <h2><span class="id">{chr(65 + index)}</span>{title}</h2>
  <div class="flow">{steps}</div>
  <p class="cost">{cost}</p>
</section>""")

page = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>Party Size And Room Types</title>
<!-- Published tokens at the shell's values; the frame classes are the gold page's
     stylesheet with its own copy of the published palette removed (page 64 §8). -->
<link rel="stylesheet" href="../../ui/preview/tokens.css">
<style>{style}
  body{{background:var(--color-surface);color:var(--color-ink);padding:34px 22px 80px}}
  .page{{max-width:1040px;margin:0 auto;display:flex;flex-direction:column;gap:34px}}
  .lede h1{{font-size:22px;font-weight:600;margin:0 0 8px}}
  .lede p, .cost{{max-width:80ch;color:var(--color-ink-muted);line-height:1.6;margin:0}}
  .item h2{{font-size:16px;font-weight:600;margin:0 0 12px;display:flex;align-items:center;gap:10px}}
  .id{{border:1px solid var(--color-line-strong);border-radius:6px;font-size:12px;
      padding:1px 8px;color:var(--color-ink-muted)}}
  .flow{{display:flex;flex-direction:column;gap:12px;margin:0 0 12px}}
  .step{{display:flex;gap:12px;align-items:flex-start}}
  .step > .card{{flex:1}}
  .sn{{flex:0 0 22px;height:22px;border-radius:50%;border:1px solid var(--color-line-strong);
      display:flex;align-items:center;justify-content:center;font-size:12px;
      color:var(--color-ink-muted);margin-top:10px}}
  .tbl .tr.list{{grid-template-columns:2.4fr .8fr .6fr .8fr .8fr .6fr 1fr}}
  .tr.list.dim{{opacity:.55}}
  .sleeps{{color:var(--color-ink-faint);font-size:12px;margin-left:8px}}
  .note.warn{{border-left:2px solid var(--color-warn);padding-left:10px;margin-top:8px}}
  .row{{display:flex;gap:8px;justify-content:flex-end;margin-top:12px}}
  .ask{{color:var(--color-ink);font-size:14px}}
</style></head>
<body><div class="page">
<div class="lede">
  <h1>A party of three, and a room that sleeps two</h1>
  <p>New booking asks how many guests, and the room types now say how many each
  sleeps. Nothing joins the two yet, and what should happen is a decision about
  the desk rather than about the data.</p>
  <p><b>Each treatment is followed to the end — the booking made, or the place
  the desk stops.</b> The first edition drew only the list and a confirm card
  with no buttons on it, so two treatments looked identical and there was no way
  to see what happens after a warning. That is where they actually differ.</p>
  <p>The numbers are real: three types and what they sleep are the review data
  GuestOps renders today, Superior Double is the approved flow's fourth row, and
  Deluxe Twin's stock is changed so the too-small type has rooms free — sold out,
  it draws <i>none free</i> in every treatment and there is nothing to compare.</p>
</div>
{chr(10).join(sections)}
<section class="item">
  <h2><span class="id">?</span>What to decide, and the one I would choose</h2>
  <p class="cost"><b>Only B prevents.</b> A prevents by removing the row, and
  takes the eighteen twins off the desk's screen with it. C and D both allow the
  booking; the difference between them is the last card, not the mark — D's stay
  carries that it was booked over capacity, C's says nothing.</p>
  <p class="cost"><b>The recorded line in D is a claim, and it needs your word.</b>
  The stay would carry that a desk knowingly booked three guests into a room that
  sleeps two — the same shape as the overbooking record you ruled in September.
  If that should not be recorded, D becomes C with a mark, and then C and D
  really are one treatment.</p>
  <p class="ask">A, B, C or D — and for D, whether the stay records it.</p>
</section>
</div></body></html>
"""

assert_answerable(page)

with open(ROOT + r"\08-party-size-and-room-types.html", "wb") as out:
    out.write(page.encode("utf-8"))

print("written", len(page))
