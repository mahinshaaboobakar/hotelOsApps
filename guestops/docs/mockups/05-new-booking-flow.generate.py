"""Generate guestops/docs/mockups/05-new-booking-flow.html from the gold frame's style."""
import re

ROOT = r"C:\Users\Mahin Aboobakker\PycharmProjects\HotelOsApps\guestops\docs\mockups"
gold = open(ROOT + r"\01-guestops-gold.html", encoding="utf-8").read()
style = gold[gold.index("<style>") + len("<style>"):gold.index("</style>")]

# Drop the published names from the drawing's own :root — they come from the
# harness's tokens.css, the published set at the shell's values (page 64 §8).
published = re.compile(r"^\s*(--color-[a-z-]+|--radius-panel|--font-sans):[^;]*;", re.M)
style = published.sub("", style)
style = re.sub(r"/\* The three soft tones[^*]*\*/", "", style)
style = re.sub(r"/\* The fourteen published tokens[\s\S]*?\*/", "", style, count=1)
for name in ["--color-surface:", "--color-ink:", "--color-brand:"]:
    assert name not in style.split(":root{")[1].split("}")[0], name

HEAD = """  <div class="head">
    <div class="app"><div class="mark">GO</div>GuestOps</div>
    <button class="tab" type="button">Today</button>
    <button class="tab on" type="button">Bookings</button>
    <button class="tab" type="button">Guests</button>
    <button class="tab" type="button">Attention</button>
    <div class="who">Anitha Menon · Front Office · Avenue Regent</div>
  </div>"""

TITLE = """    <div class="title"><div><div class="ht">New booking</div></div><div class="grow"></div><button class="btn" type="button">Walk-in</button></div>"""

def win(body, h=560):
    return f'<div class="win" style="min-height:{h}px">\n{HEAD}\n  <div class="main">\n{body}\n  </div>\n</div>'

QUERY_EDITING = """      <div class="fltr" style="display:flex;gap:10px;align-items:flex-end">
        <div class="fld" style="flex:1"><label>Arrive</label><div class="inp" style="border-color:var(--color-brand)"><b>03 Sept 2026</b><span class="grow">▾</span></div></div>
        <div class="fld" style="flex:1"><label>Depart</label><div class="inp"><b>07 Sept 2026</b><span class="grow">4 nights</span></div></div>
        <div class="fld" style="flex:1"><label>Guests</label><div class="inp">1 room · 2 adults · 0 children <span class="grow">▾</span></div></div>
        <button class="btn pri" type="button">Check availability</button>
      </div>"""

CALENDAR = """      <div class="card" style="width:300px;margin-top:-6px">
        <div class="ch">September 2026<span class="grow"></span>‹ ›</div>
        <div class="cb" style="display:grid;grid-template-columns:repeat(7,1fr);gap:4px;font-size:12px;text-align:center">
          <span class="hint">Mo</span><span class="hint">Tu</span><span class="hint">We</span><span class="hint">Th</span><span class="hint">Fr</span><span class="hint">Sa</span><span class="hint">Su</span>
          <span></span><span>1</span><span>2</span><span style="border-radius:6px;background:var(--color-brand);color:var(--color-ink-on-accent)">3</span><span style="background:var(--brand-wash)">4</span><span style="background:var(--brand-wash)">5</span><span style="background:var(--brand-wash)">6</span>
          <span style="border-radius:6px;background:var(--color-brand);color:var(--color-ink-on-accent)">7</span><span>8</span><span>9</span><span>10</span><span>11</span><span>12</span><span>13</span>
        </div>
      </div>"""

QUERY_DONE = """      <div class="fltr" style="display:flex;gap:10px;align-items:flex-end">
        <div class="fld" style="flex:1"><label>Arrive</label><div class="inp"><b>03 Sept 2026</b></div></div>
        <div class="fld" style="flex:1"><label>Depart</label><div class="inp"><b>07 Sept 2026</b><span class="grow">4 nights</span></div></div>
        <div class="fld" style="flex:1"><label>Guests</label><div class="inp">1 room · 2 adults <span class="grow">▾</span></div></div>
        <button class="btn" type="button">Check again</button>
      </div>"""

def row(name, total, sold, ooo, stop, free, chosen=False):
    free_colour = "var(--ok)" if free > 0 else "var(--faint)"
    choose = ('<button class="btn sm pri" type="button">Chosen</button>' if chosen else
              '<button class="btn sm" type="button">Choose</button>' if free > 0 else
              '<span class="hint">none free</span>')
    sel = " sel" if chosen else ""
    return (f'        <div class="tr list{sel}"><div class="nm"><b>{name}</b></div><div>{total}</div><div>{sold}</div>'
            f'<div>{ooo}</div><div>{stop}</div><div style="display:flex;gap:12px;align-items:center">'
            f'<b style="color:{free_colour};font-size:15px">{free}</b>{choose}</div></div>')

LIST = lambda chosen: "\n".join([
    '      <div class="tbl">',
    '        <div class="tr list hd"><div>Room type</div><div>Total rooms</div><div>Sold</div><div>Out of order</div><div>Held back</div><div>Free</div></div>',
    row("Deluxe King", 24, 19, 1, 0, 4, chosen),
    row("Deluxe Twin", 18, 18, 0, 0, 0),
    row("Executive Suite", 6, 2, 0, 4, 0),
    row("Superior Double", 12, 7, 0, 0, 5),
    '      </div>'])

GUEST_SHEET = """    <div class="scrim">
      <div class="sheet">
        <div class="sh_h"><b>Guest details</b><span>Deluxe King · 03 → 07 Sept 2026 · 4 nights · 2 adults</span></div>
        <div class="sh_b">
          <div class="fld"><label>Guest name</label><div class="inp" style="border-color:var(--color-brand)">Fatima Sheikh</div>
            <div class="hint">As the guest gives it. A name can be added later — a booking without one shows "Not yet named".</div></div>
          <div class="row2">
            <div class="fld"><label>Phone</label><div class="inp">+91 98470 11234</div></div>
            <div class="fld"><label>Email</label><div class="inp ph">optional</div></div>
          </div>
          <div class="fld"><label>Second guest</label><div class="inp ph">＋ add a guest in this room</div></div>
        </div>
        <div class="sh_f"><div class="grow"></div><button class="btn sm" type="button">Back</button><button class="btn sm pri" type="button">Review booking</button></div>
      </div>
    </div>"""

CONFIRM_SHEET = """    <div class="scrim">
      <div class="sheet">
        <div class="sh_h"><b>Create this booking?</b><span>Check it with the guest before you confirm</span></div>
        <div class="sh_b">
          <div class="fr"><div class="k">Guest</div><div class="v"><b>Fatima Sheikh</b> · +91 98470 11234</div></div>
          <div class="fr"><div class="k">Room type</div><div class="v">Deluxe King · one room</div></div>
          <div class="fr"><div class="k">Dates</div><div class="v"><b>03 Sept</b> → <b>07 Sept 2026</b> · 4 nights</div></div>
          <div class="fr"><div class="k">Guests</div><div class="v">2 adults</div></div>
          <div class="fr"><div class="k">Room</div><div class="v">assigned at check-in</div></div>
          <div class="note" style="padding:10px 12px">No payment is taken and no price is quoted here.</div>
        </div>
        <div class="sh_f"><div class="hint">Recorded against your name.</div><div class="grow"></div><button class="btn sm" type="button">Back</button><button class="btn sm pri" type="button">Create booking</button></div>
      </div>
    </div>"""

DONE = """    <div class="title"><div><div class="ht">Fatima Sheikh · booking created here</div><div class="hsub">One stay · 03 → 07 Sept 2026</div></div><div class="grow"></div><button class="btn danger" type="button">Cancel…</button></div>
    <div class="body">
      <div class="ban info"><div><b>Booking created.</b> It is in Bookings and on the arrivals list for 03 Sept.</div></div>
      <div class="tbl">
        <div class="tr list stays hd"><div>Guest</div><div>Stay</div><div>Room type</div><div>Room</div><div>Dates</div><div>Status</div><div></div></div>
        <div class="tr list stays"><div class="nm"><b>Fatima Sheikh</b><span>2 adults</span></div><div>01J9Q…8C21</div><div>Deluxe King</div><div>—</div><div>03 Sept → 07 Sept</div><div><span class="pill">Booked</span></div><div><span class="sh miss"><i></i>no room</span></div></div>
      </div>
    </div>"""

def refused(banner, tone="no"):
    edge = {"no": "var(--color-line-strong)", "fault": "var(--bad-edge)", "wait": "var(--color-warn)"}[tone]
    return f"""    <div class="scrim">
      <div class="sheet">
        <div class="sh_h"><b>Create this booking?</b><span>Check it with the guest before you confirm</span></div>
        <div class="sh_b">
          <div class="note" style="padding:10px 12px;border-color:{edge}">{banner}</div>
          <div class="fr"><div class="k">Guest</div><div class="v"><b>Fatima Sheikh</b> · +91 98470 11234</div></div>
          <div class="fr"><div class="k">Room type</div><div class="v">Deluxe King · one room</div></div>
          <div class="fr"><div class="k">Dates</div><div class="v"><b>03 Sept</b> → <b>07 Sept 2026</b> · 4 nights</div></div>
        </div>
        <div class="sh_f"><div class="grow"></div><button class="btn sm" type="button">Back</button><button class="btn sm pri" type="button">Try again</button></div>
      </div>
    </div>"""

INVALID = """      <div class="fltr" style="display:flex;gap:10px;align-items:flex-end">
        <div class="fld" style="flex:1"><label>Arrive</label><div class="inp"><b>07 Sept 2026</b></div></div>
        <div class="fld" style="flex:1"><label>Depart</label><div class="inp" style="border-color:var(--bad-edge)"><b>03 Sept 2026</b></div>
          <div class="hint" style="color:var(--bad)">The departure is before the arrival.</div></div>
        <div class="fld" style="flex:1"><label>Guests</label><div class="inp">1 room · 2 adults <span class="grow">▾</span></div></div>
        <button class="btn pri off" type="button" disabled>Check availability</button>
      </div>"""

def base(inner): return TITLE + '\n    <div class="body">\n' + inner + '\n    </div>'

STEPS = [
    ("1", "Dates and guests",
     "The person picks the arrival and departure on a calendar and says how many rooms and guests. Nothing is searched until they press <b>Check availability</b>.",
     win(base(QUERY_EDITING + "\n" + CALENDAR), 460),
     ["<code>availability</code> read — <b>exists</b> (<code>reservation.read</code>), takes <code>arrive</code>/<code>depart</code> ISO days. Today the screen sends none, so the service refuses; this step is what supplies them.",
      "The guest count is sent and <b>not used</b> — availability counts rooms, not capacity. Say whether it should filter room types by occupancy, or only be carried to the booking."]),
    ("2", "Room type",
     "Every room type, with what is free for those dates. Only a type with rooms free can be chosen; the others say none are free.",
     win(base(QUERY_DONE + "\n" + LIST(False)), 460),
     ["Same <code>availability</code> read. <b>Missing on the wire:</b> each row sends the room type's name and not its <b>id</b>, which creating a booking needs.",
      "Column \"Held back\" is the stop-sell count, renamed from \"Stop-sell\" for the desk — the owner's call."]),
    ("3", "Guest details",
     "The person types the guest's name as given, and a phone or email. A second guest in the room can be added. The room and dates stay visible at the top.",
     win(base(QUERY_DONE + "\n" + LIST(True)) + "\n" + GUEST_SHEET, 640),
     ["Nothing is called here. The fields are the ones <code>BookingService.CreateAsync</code>'s <code>NewGuest</code> already takes (name as given · given · family · phone · email · primary); contacts are encrypted on write by the existing protector."]),
    ("4", "Confirm",
     "One screen that says exactly what will be created, before anything is. Nothing is charged and no price is shown.",
     win(base(QUERY_DONE + "\n" + LIST(True)) + "\n" + CONFIRM_SHEET, 560),
     ["<b>Create booking</b> calls <code>BookingService.CreateAsync</code> — <b>exists</b>, and is reachable over gRPC and inside the walk-in command, but has <b>no module door</b>: the screen cannot call it. Needs a module method under <code>stay.create</code>, as <code>walkIn</code> has.",
      "<code>CreateAsync</code> does <b>not re-check availability</b>: a type sold out between steps 2 and 4 is booked anyway. Say whether creation must refuse, or the desk may overbook knowingly.",
      "It asks Context for the business day first, so on the owner's platform it fails until the SDK authenticator defect (BB) is fixed.",
      "No price: rates are out of this round (GUEST-Q7). A booking made here carries no commercial terms."]),
    ("5", "Done",
     "The booking opens, with a line saying it was created and where to find it. The room is assigned at check-in.",
     win(DONE, 380),
     ["Opens with the existing <code>booking</code> read. A booking made at the desk has no PMS reference, so it is titled by guest and the stay's short id."]),
]

REFUSALS = [
    ("Not permitted",
     refused("<b>That was not permitted, so nothing was created.</b> Your role does not allow creating bookings — an administrator can grant it."),
     "The service refuses before writing. Wording as GuestOps already says it for every refused write."),
    ("Could not be checked",
     refused("<b>Whether that is permitted could not be checked, so nothing was created.</b> Try again in a moment.", "fault"),
     "The permission check itself failed. Red, and never worded as the person lacking permission."),
    ("No answer",
     refused("<b>GuestOps did not answer, so whether the booking was made is not known.</b> Check Bookings for Fatima Sheikh before trying again — trying again could make a second booking.", "wait"),
     "The request may have reached the service. The screen does not claim nothing happened, and says what trying again could do."),
    ("Dates the wrong way round",
     win(base(INVALID), 300),
     "Caught before anything is sent; the search stays off until the dates make sense. The service refuses the same case with the same sentence."),
]

parts = []
for num, name, says, frame, notes in STEPS:
    note_items = "".join(f"<li>{n}</li>" for n in notes)
    parts.append(f"""<section class="step">
  <h2><span class="num">{num}</span>{name}</h2>
  <p class="says">{says}</p>
  {frame}
  <aside class="dev"><b>Note to the architect — not on the screen.</b><ul>{note_items}</ul></aside>
</section>""")

ref_html = []
for name, frame, says in REFUSALS:
    body = frame if frame.startswith("<div class=\"win\"") else win(base(QUERY_DONE + "\n" + LIST(True)) + "\n" + frame, 520)
    ref_html.append(f"""<div class="ref"><h3>{name}</h3><p class="says">{says}</p>{body}</div>""")

page = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>New Booking Flow</title>
<!-- The published tokens at the shell's own values — the harness's file, held equal to
     TOKEN_NAMES by tests/tokens.test.ts. The frame chrome below is the approved gold
     frame's stylesheet with its own copy of the published palette removed (page 64 §8). -->
<link rel="stylesheet" href="../../ui/preview/tokens.css">
<style>{style}
  /* This page's own layout, around the frames. */
  body{{background:var(--color-surface);color:var(--color-ink);padding:34px 22px 80px}}
  .page{{max-width:1240px;margin:0 auto;display:flex;flex-direction:column;gap:40px}}
  .lede h1{{font-size:24px;font-weight:600;margin:0 0 8px}}
  .lede p, .says{{max-width:80ch;color:var(--color-ink-muted);line-height:1.6;margin:0 0 12px}}
  .lede p b, .says b{{color:var(--color-ink)}}
  .step h2{{display:flex;align-items:center;gap:12px;font-size:18px;font-weight:600;margin:0 0 8px}}
  .num{{display:inline-grid;place-items:center;width:28px;height:28px;border-radius:50%;
    background:var(--brand-wash);color:var(--color-brand);font-size:14px}}
  .dev{{margin-top:14px;max-width:90ch;border:1px dashed var(--color-warn);border-radius:12px;
    padding:12px 16px;font-size:13px;line-height:1.6;color:var(--color-ink-muted);
    background:color-mix(in srgb, var(--color-warn) 5%, transparent)}}
  .dev b{{color:var(--color-warn)}}
  .dev ul{{margin:6px 0 0 18px;padding:0;display:flex;flex-direction:column;gap:4px}}
  .dev code{{color:var(--color-ink)}}
  .refs{{display:grid;grid-template-columns:repeat(auto-fit,minmax(560px,1fr));gap:26px}}
  .ref h3{{font-size:15px;margin:0 0 6px}}
  .win{{position:relative}}
</style></head>
<body><div class="page">
<div class="lede">
  <h1>New booking — the booking flow, drawn for you</h1>
  <p><b>Today New booking cannot take a guest's own dates, so it cannot make a booking.</b> This is the flow drawn in the approved frames' style, for you to approve before anything is built: dates and guests, room type, guest details, confirm, and what a refusal looks like.</p>
  <p>Each step is one screen as a person at the desk would see it. <b>The yellow dashed boxes are notes for the architect</b> — which part of GuestOps each step would call, and what does not exist yet. They are not part of the screen.</p>
  <p>Dates are drawn as a property in India would show them (en-IN). Nothing here is priced: rates are not part of this round.</p>
</div>
{chr(10).join(parts)}
<section class="step">
  <h2><span class="num">!</span>When it cannot be done</h2>
  <p class="says">Every refusal keeps what the person typed, says in plain words what happened and whether anything was created, and offers the action that could help.</p>
  <div class="refs">{chr(10).join(ref_html)}</div>
</section>
</div></body></html>
"""
out = ROOT + r"\05-new-booking-flow.html"
open(out, "w", encoding="utf-8", newline="\n").write(page)
print("written", len(page))
