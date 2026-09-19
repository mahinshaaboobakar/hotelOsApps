// What a rendered GuestOps surface IS, for the app surface checklist's measured
// lines — read from the live DOM, never inferred from CSS text.
//
// Runs inside the page (or a widget's realm) and returns plain data. The rules
// are applied in Node afterwards (`rules.mjs`), so a rule can change without
// re-driving the browser, and the facts a verdict rested on are kept beside it.
//
// `doc` is the document to read: the page for a screen, a widget's own realm
// for a card — a card is judged against the sheet it mounts (X12), never the
// module's.

export const PROBE = String.raw`(doc) => {
  const cs = (e) => e ? getComputedStyle(e) : null;
  const box = (e) => {
    if (!e) return null;
    const r = e.getBoundingClientRect();
    return { top: r.top, bottom: r.bottom, left: r.left, right: r.right, width: r.width, height: r.height };
  };
  const pick = (e, props) => {
    const s = cs(e);
    return s ? Object.fromEntries(props.map((p) => [p, s.getPropertyValue(p)])) : null;
  };
  const view = doc.defaultView;
  const all = (sel) => Array.from(doc.querySelectorAll(sel));

  // The screen's body: the one that holds the list, or the main's last.
  const bodies = all(".main > .body");
  const body = bodies.at(-1) ?? null;
  const pager = doc.querySelector(".pager");
  const list = pager ? pager.previousElementSibling : null;

  // Rows of whatever the list is — table rows, card rows, or cards.
  // GuestOps' rows are .tr and its header row .tr.hd (chrome/styles/table.ts).
  // No backticks anywhere in this template: one closes it.
  const rows = list ? Array.from(list.querySelectorAll(".tr:not(.hd), .card, .row"))
    .filter((r) => r.offsetParent !== null) : [];
  const rowsHeight = rows.reduce((sum, r) => sum + r.getBoundingClientRect().height, 0);
  const header = list ? list.querySelector(".tr.hd") : null;

  // L1: anything between the list and the body that draws a box.
  const between = [];
  for (let e = list?.parentElement; e && e !== body && e !== doc.body; e = e.parentElement) {
    const s = cs(e);
    between.push({ cls: e.className, bg: s.backgroundColor, border: s.borderTopWidth, radius: s.borderTopLeftRadius });
  }

  // C9: every control's line-height against its parent's.
  const controls = all("button").filter((b) => b.offsetParent !== null).map((b) => ({
    cls: b.className, text: (b.textContent ?? "").trim().slice(0, 24),
    lh: cs(b).lineHeight, parentLh: cs(b.parentElement).lineHeight,
    fs: cs(b).fontSize, parentFs: cs(b.parentElement).fontSize,
    family: cs(b).fontFamily === cs(b.parentElement).fontFamily,
  }));

  const head = doc.querySelector(".head");
  const activeTab = doc.querySelector(".head .tab.on");
  const fail = doc.querySelector(".fail");
  const stage = doc.querySelector(".fs");
  const mark = doc.querySelector(".fg, .wg");
  const wx = doc.querySelector(".wb.wx");

  return {
    reached: !doc.body.textContent.includes("drive failed for screen="),
    driveError: doc.body.textContent.includes("drive failed for screen=") ? doc.body.textContent.slice(0, 300) : null,
    viewport: { w: view.innerWidth, h: view.innerHeight },
    pageScrolls: (doc.scrollingElement ?? doc.documentElement).scrollHeight > view.innerHeight + 1,
    head: head ? { box: box(head), ...pick(head, ["padding-left", "padding-right", "border-bottom-width", "border-bottom-style"]) } : null,
    activeTab: activeTab ? pick(activeTab, ["border-bottom-width", "border-bottom-color"]) : null,
    body: body ? {
      box: box(body), scrollHeight: body.scrollHeight, clientHeight: body.clientHeight,
      ...pick(body, ["padding-top", "overflow-y", "overflow-x"]),
      clipped: body.scrollHeight > body.clientHeight + 1 && cs(body).overflowY === "hidden",
    } : null,
    pager: pager ? {
      box: box(pager), text: pager.textContent.replace(/\s+/g, " ").trim(),
      // The range is its own span. Reading it out of the whole strip ran it
      // into "10 per page" and read "of 4" as "of 410" — the instrument's
      // fault, found by checking a verdict before believing it.
      range: (pager.firstElementChild?.textContent ?? "").replace(/\s+/g, " ").trim(),
      position: cs(pager).position, prev: list ? list.tagName + "." + list.className : null,
      followedBy: pager.nextElementSibling ? pager.nextElementSibling.className : null,
      nextOfList: list?.nextElementSibling === pager,
      arrows: Array.from(pager.querySelectorAll(".pg")).map((b) => ({ t: b.textContent, disabled: b.hasAttribute("disabled"), label: b.getAttribute("aria-label") })),
    } : null,
    // A stack of cards (Attention) is a list whose rows are the body's cards,
    // not the rows inside the last one.
    // Attention's cards moved into one .stack (CORE-Q28: one list to scroll);
    // counted in either place so the probe reads before and after alike.
    stackCards: body ? body.querySelectorAll(":scope > .card, :scope > .stack > .card").length : 0,
    list: list ? {
      box: box(list), cls: list.className, rows: rows.length, rowsHeight,
      headerHeight: header ? header.getBoundingClientRect().height : 0,
      scrollHeight: list.scrollHeight, clientHeight: list.clientHeight,
      ...pick(list, ["overflow-y", "flex-grow", "min-height", "background-color", "border-top-left-radius"]),
    } : null,
    between,
    th: pick(doc.querySelector(".tr.hd > div"), ["padding-top", "padding-left", "font-size", "font-weight", "text-transform", "letter-spacing", "color"]),
    thRow: pick(doc.querySelector(".tr.hd"), ["border-bottom-width", "align-items"]),
    td: pick(doc.querySelector(".tr:not(.hd) > div"), ["padding-top", "padding-left", "align-self"]),
    tdRow: pick(doc.querySelector(".tr:not(.hd)"), ["border-bottom-width", "align-items"]),
    lastRow: rows.length ? pick(rows.at(-1), ["border-bottom-width", "border-bottom-style"]) : null,
    btn: pick(doc.querySelector(".btn:not(.pri):not(.danger):not(.sm):not(.off)"), ["border-top-width", "border-top-color", "border-top-left-radius", "padding-top", "padding-left", "font-size", "color", "background-color"]),
    // A LIVE primary — .btn.pri.off is C11's shape, not C2's, and measuring it
    // as C2 failed Setup's deliberately unavailable Save for having no fill.
    btnPri: pick(doc.querySelector(".btn.pri:not(.sm):not(.off)"), ["border-top-color", "color", "background-image"]),
    btnPriOff: pick(doc.querySelector(".btn.pri.off"), ["background-image", "border-top-style", "color", "cursor"]),
    btnDanger: pick(doc.querySelector(".btn.danger:not(.confirm)"), ["color", "border-top-color"]),
    controls,
    rowButtons: all(".tr.act, .row.act").map((r) => r.tagName),
    // C8 as Jobs' board reads it: the row keeps its click, and its key text is
    // a real button. Measured on that button: no UA border, left-aligned, the
    // row's family.
    rowOpeners: all(".tr.act, .row.act").map((r) => {
      const b = r.querySelector("button.opener");
      return b ? { border: cs(b).borderTopWidth, align: cs(b).textAlign, family: cs(b).fontFamily === cs(r).fontFamily } : null;
    }),
    // D4's ruled role, and only it (64a: the Activity date column — faint text
    // elsewhere is unclassified and OPEN, not swept off one ruling). 64a names
    // it by the DRAWING's class, .act .tm b; the build draws the role as
    // .ev .tm b (chrome/styles/table.ts). The first probe used the drawing's
    // and found nothing — a selector that finds nothing can neither pass nor fail.
    activityDate: pick(doc.querySelector(".ev .tm b"), ["color"]),
    barrenSays: (() => { const t = list?.querySelector(".tr:not(.hd) .hint, .tr:not(.hd) .empty, .empty"); return t ? t.textContent.trim() : null; })(),
    notes: all(".note, .hint, .fn").filter((n) => n.offsetParent !== null).slice(0, 12).map((n) => ({ cls: n.className, ...pick(n, ["font-size", "line-height", "color"]) })),
    failure: fail ? {
      stage: box(stage), state: box(fail),
      mark: mark ? { tag: mark.tagName.toLowerCase(), stroke: mark.getAttribute("stroke"), color: cs(mark).color, w: box(mark).width } : null,
      label: pick(doc.querySelector(".fl"), ["font-size", "letter-spacing", "text-transform", "color", "font-family"]),
      said: pick(doc.querySelector(".fh"), ["font-size", "font-weight"]),
      why: pick(doc.querySelector(".fb"), ["font-size", "color"]),
      buttons: Array.from(fail.querySelectorAll(".fd button")).map((b) => b.textContent),
      facts: Array.from(fail.querySelectorAll(".fp dt")).map((d) => d.textContent),
      tone: fail.className,
      text: fail.textContent.replace(/\s+/g, " ").trim(),
    } : null,
    card: wx ? {
      tone: wx.className,
      mark: mark ? { tag: mark.tagName.toLowerCase(), color: cs(mark).color, w: box(mark).width, h: box(mark).height } : null,
      children: Array.from(wx.children).map((c) => c.getAttribute("class")),
      facts: wx.querySelectorAll(".fp, dl").length,
      open: wx.querySelector(".wo")?.textContent ?? null,
      card: box(doc.querySelector(".w")),
    } : null,
    sheet: (() => { const s = doc.querySelector(".sheet, .dlg"); return s ? { cls: s.className, box: box(s), position: cs(s).position, scrim: cs(s.parentElement).position, siblingOfBody: s.parentElement?.parentElement?.querySelector(":scope > .body") !== null } : null; })(),
  };
}`;
