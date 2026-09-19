// The page-64 audit's in-page probe — `docs/app-surface-checklist.md` (3d521cef),
// measured on the RENDERED page: computed styles and boxes, never CSS text.
//
// Evaluated by `run.mjs` in the capture harness after the drive has reached its
// state. Returns `{ lines: { <ID>: [{ v, why }] }, record: { <ID>: <what is built> } }`
// where `v` is PASS · FAIL · NA · OPEN. A line this page has nothing to say about
// is absent, never PASS — the checklist's own rule ("a line with no entry for a
// state is not a pass in that state").
//
// Roles are Room Care's, stated once here and in the report, because D2–D4 name
// ROLES (§5: "found by the rule that governs them — never by replacing a figure
// wherever it occurs"):
//
//   table header   th                                   .08em      L2 · D2
//   field label    label.lbl                            .07em      F2 · D2
//   section label  .sect · .card.accent > h3             .04em      D2
//   note text      .note · .legend · .tl .ev span       12px/19.8  D3
//   quiet text     .tl .ev span (the timeline's date     ink-muted  D4
//                  and basis line — the ruled case)
//
// 64b's own classes (.fail-*) are governed by §13's lines, not by §5's.
(ctx) => {
  const lines = {};
  const record = {};
  const add = (id, v, why) => (lines[id] ??= []).push({ v, why });
  const cs = (e) => getComputedStyle(e);
  const px = (s) => parseFloat(s);
  const text = (e) => (e.textContent || "").trim().replace(/\s+/g, " ");
  const name = (e) => {
    const cls = typeof e.className === "string" && e.className.trim() !== "" ? `.${e.className.trim().split(/\s+/).join(".")}` : "";
    return `${e.tagName.toLowerCase()}${cls} "${text(e).slice(0, 36)}"`;
  };
  const shown = (e) => { const r = e.getBoundingClientRect(); return r.width > 0 && r.height > 0 && cs(e).visibility !== "hidden"; };
  const all = (sel, root = document) => [...root.querySelectorAll(sel)].filter(shown);
  const C = {
    ink: "rgb(232, 235, 244)", muted: "rgb(139, 147, 167)", faint: "rgb(90, 97, 114)", onAccent: "rgb(11, 13, 20)",
    bad: "rgb(248, 113, 113)", brand: "rgb(129, 140, 248)", warn: "rgb(251, 191, 36)",
    lineStrong: "rgba(255, 255, 255, 0.14)", line: "rgba(255, 255, 255, 0.07)", none: "rgba(0, 0, 0, 0)",
  };
  /** Each failing node once, by what it is — a list of forty identical buttons is one finding with a count. */
  const judge = (id, nodes, test) => {
    if (nodes.length === 0) return;
    const bad = new Map();
    for (const n of nodes) {
      const why = test(n);
      if (why) { const key = `${name(n).replace(/ ".*/, "")}: ${why}`; bad.set(key, (bad.get(key) ?? 0) + 1); }
    }
    if (bad.size === 0) add(id, "PASS", `${nodes.length} measured`);
    for (const [why, n] of bad) add(id, "FAIL", n > 1 ? `${why} (×${n})` : why);
  };
  const box = (e, side) => px(cs(e)[`padding${side}`]);
  const pad = (e) => `${cs(e).paddingTop} ${cs(e).paddingRight} ${cs(e).paddingBottom} ${cs(e).paddingLeft}`;
  const em = (e) => Math.round((px(cs(e).letterSpacing) / px(cs(e).fontSize)) * 1000) / 1000;

  const body = document.querySelector(".rc .body");
  const failing = document.querySelector(".fail-body") !== null && !ctx.widgets;
  record.state = ctx.state;

  // ---------------------------------------------------------------- §2 controls
  const plain = all(".btn").filter((b) => !/\b(pri|sm|chip|pg|danger|off|on)\b/.test(b.className) && b.tagName === "BUTTON");
  judge("C1", plain, (b) => {
    const s = cs(b); const out = [];
    if (s.borderTopWidth !== "1px" || s.borderTopStyle !== "solid" || s.borderTopColor !== C.lineStrong) out.push(`border ${s.borderTopWidth} ${s.borderTopStyle} ${s.borderTopColor}`);
    if (s.borderTopLeftRadius !== "8px") out.push(`radius ${s.borderTopLeftRadius}`);
    if (pad(b) !== "7px 14px 7px 14px") out.push(`padding ${pad(b)}`);
    if (s.fontSize !== "13px") out.push(`font-size ${s.fontSize}`);
    if (s.color !== C.ink) out.push(`color ${s.color}`);
    if (s.backgroundColor !== C.none || s.backgroundImage !== "none") out.push(`background ${s.backgroundColor} ${s.backgroundImage.slice(0, 30)}`);
    return out.join("; ");
  });
  judge("C2", all(".btn.pri:not(.off)"), (b) => {
    const s = cs(b); const out = [];
    if (s.borderTopColor !== C.none) out.push(`border-color ${s.borderTopColor}`);
    if (s.color !== C.onAccent) out.push(`color ${s.color}`);
    if (!/^linear-gradient\(135deg, rgb\(129, 140, 248\)/.test(s.backgroundImage)) out.push(`background ${s.backgroundImage.slice(0, 60)}`);
    return out.join("; ");
  });
  judge("C3", all(".btn.off"), (b) => {
    const s = cs(b); const out = [];
    if (s.color !== C.faint) out.push(`color ${s.color}`);
    if (s.borderTopStyle !== "dashed") out.push(`border-style ${s.borderTopStyle}`);
    return out.join("; ");
  });
  const smalls = all(".btn.sm");
  judge("C4", smalls.filter((b) => b.closest("tr") !== null), (b) => {
    const s = cs(b); const out = [];
    if (pad(b) !== "2px 8px 2px 8px") out.push(`padding ${pad(b)}`);
    if (s.fontSize !== "11px") out.push(`font-size ${s.fontSize}`);
    return out.join("; ");
  });
  const inCard = smalls.filter((b) => b.closest("tr") === null && b.closest(".card, .sheet, .dlg") !== null);
  const loose = smalls.filter((b) => b.closest("tr") === null && b.closest(".card, .sheet, .dlg") === null);
  if (inCard.length > 0) add("C4", "OPEN", `card half (APPS-Q43): ${[...new Set(inCard.map((b) => text(b)))].join(" · ")} — .btn.sm inside a card, not a row`);
  for (const b of loose) add("C4", "FAIL", `${name(b)} is .btn.sm in neither a row nor a card`);
  judge("C5", all(".btn.danger:not(.confirm)"), (b) => {
    const s = cs(b); const out = [];
    if (s.color !== C.bad) out.push(`color ${s.color}`);
    const m = /(?:rgba?\(248, 113, 113(?:, ([\d.]+))?\)|color\(srgb 0\.97\d+ 0\.44\d+ 0\.44\d+ \/ ([\d.]+)\))/.exec(s.borderTopColor);
    const alpha = m === null ? null : Number(m[1] ?? m[2] ?? 1);
    if (alpha === null || Math.abs(alpha - 0.45) > 0.01) out.push(`border-color ${s.borderTopColor}`);
    return out.join("; ");
  });
  judge("C6", all(".dlg .btn.danger.confirm"), (b) => {
    const s = cs(b); const out = [];
    if (s.backgroundColor !== C.bad) out.push(`background ${s.backgroundColor}`);
    if (s.color !== C.onAccent) out.push(`color ${s.color}`);
    if (s.borderTopColor !== C.none) out.push(`border-color ${s.borderTopColor}`);
    if (s.fontWeight !== "600") out.push(`weight ${s.fontWeight}`);
    return out.join("; ");
  });
  // A destructive dialog whose confirm is not the filled twin.
  for (const d of all(".dlg")) {
    const confirm = d.querySelector(".df button:last-child");
    const destructive = /revoke|cancel|delete|remove/i.test(d.getAttribute("aria-label") ?? "");
    if (destructive && confirm !== null && !confirm.classList.contains("confirm")) add("C6", "FAIL", `dialog "${d.getAttribute("aria-label")}" confirms with ${name(confirm)}`);
  }

  // C8 — anything a person clicks that is not a real control.
  const clickable = [...document.querySelectorAll("*")].filter((e) => shown(e) && cs(e).cursor === "pointer"
    && !["BUTTON", "A", "INPUT", "SELECT", "LABEL", "SUMMARY", "TEXTAREA"].includes(e.tagName) && e.closest("button, a, label") === null
    && (e.parentElement === null || cs(e.parentElement).cursor !== "pointer"));
  if (clickable.length === 0) add("C8", "PASS", "every clickable node is a button");
  else judge("C8", clickable, (e) => (["TR", "TD"].includes(e.tagName)
    ? `a table ${e.tagName.toLowerCase()} opens something and is not a <button> — §2 against §4: a table row cannot be a button, and Jobs' baseline list opens rows the same way`
    : `clickable ${e.tagName.toLowerCase()} (cursor:pointer), not a <button>`));
  judge("C8", all("button.btn, button.tile, tr button"), (b) => {
    const s = cs(b); const out = [];
    if (b.parentElement && s.fontFamily !== cs(b.parentElement).fontFamily && !/monospace/.test(s.fontFamily)) out.push(`family ${s.fontFamily.slice(0, 30)}`);
    return out.join("; ");
  });

  // C9 — line-height inherited: never `normal`, and the parent's ratio.
  const selects = all("select").filter((c) => cs(c).lineHeight === "normal");
  if (selects.length > 0) add("C9", "NA", `${selects.length} <select>: Chromium computes a select's line-height as normal whatever is declared — an explicit line-height:21px measures normal (2026-09-19)`);
  judge("C9", all("button, input, textarea"), (c) => {
    const s = cs(c); const p = c.parentElement === null ? null : cs(c.parentElement);
    if (s.lineHeight === "normal") return "line-height normal";
    if (p === null || p.lineHeight === "normal") return "";
    const mine = px(s.lineHeight) / px(s.fontSize); const theirs = px(p.lineHeight) / px(p.fontSize);
    return Math.abs(mine - theirs) > 0.02 && Math.abs(px(s.lineHeight) - px(p.lineHeight)) > 0.5 ? `line-height ${s.lineHeight} at ${s.fontSize}; parent ${p.lineHeight} at ${p.fontSize}` : "";
  });

  // C11 — a primary drawn off carries its reason beside it.
  for (const b of all(".btn.pri.off")) {
    const around = text(b.parentElement ?? b).replace(text(b), "").trim();
    add("C11", around.length > 3 ? "PASS" : "FAIL", around.length > 3 ? `${name(b)} beside "${around.slice(0, 50)}"` : `${name(b)} has no reason beside it`);
  }
  for (const b of all("button.btn.pri[disabled]")) add("C11", "FAIL", `${name(b)} is disabled, not drawn off`);
  // A screen as it arrives has nothing edited: its Save is drawn off with its reason, never live.
  if (["ALL", "NL"].includes(ctx.state)) {
    for (const s of all(".save > button.btn.pri")) add("C11", "FAIL", `"${text(s)}" is live with nothing edited`);
    for (const s of all(".btn.off").filter((b) => /^Save — \S/.test(text(b)))) add("C11", "PASS", `"${text(s)}" drawn off, its reason in its words`);
  }
  if (ctx.empty !== null && ctx.empty !== undefined) {
    const e = ctx.empty;
    if (e.primary === null) add("C11", "NA", "the overlay has no primary action");
    else if (!e.primary.live) add("C11", / — \S/.test(e.primary.text) ? "PASS" : "FAIL", `nothing chosen: "${e.primary.text}" drawn ${e.primary.cls}${/ — \S/.test(e.primary.text) ? "" : ", with no reason"}`);
    else if (e.writes > 0) add("C11", "PASS", `nothing chosen still sends: "${e.primary.text}" wrote ${e.writes} (a complete act by default)`);
    else add("C11", "FAIL", `nothing chosen: "${e.primary.text}" is live and ${e.open ? `refuses in place${e.said ? ` ("${e.said.slice(0, 60)}")` : ""}` : "closes having sent nothing"}`);
  }

  // ---------------------------------------------------------------- §3 navigation
  const head = document.querySelector(".rc .head");
  if (head !== null && !ctx.widgets) {
    const s = cs(head); const out = [];
    if (Math.round(head.getBoundingClientRect().height) !== 56) out.push(`height ${head.getBoundingClientRect().height}`);
    if (s.paddingLeft !== "22px" || s.paddingRight !== "22px") out.push(`padding ${s.paddingLeft}/${s.paddingRight}`);
    if (s.borderBottomWidth !== "1px") out.push(`bottom rule ${s.borderBottomWidth}`);
    if (head.firstElementChild?.classList.contains("app") !== true) out.push("app mark is not first");
    const on = head.querySelector(".tab.on");
    if (on === null) out.push("no active tab"); else if (cs(on).borderBottomWidth !== "2px" || cs(on).borderBottomColor !== C.brand) out.push(`active underline ${cs(on).borderBottomWidth} ${cs(on).borderBottomColor}`);
    const who = head.querySelector(".who");
    if (who === null || Math.abs(who.getBoundingClientRect().right - (head.getBoundingClientRect().right - 22)) > 2) out.push("signed-in person not pushed right");
    add("N1", out.length === 0 ? "PASS" : "FAIL", out.length === 0 ? "56px bar, 22px padding, brand underline, person right" : out.join("; "));
    const rails = [...document.querySelectorAll("nav, aside, .rail, .sidebar")].filter(shown).filter((e) => e.getBoundingClientRect().height > innerHeight * 0.6 && e.getBoundingClientRect().width < 320);
    add("N2", rails.length === 0 ? "PASS" : "FAIL", rails.length === 0 ? "no vertical rail in the DOM" : rails.map(name).join(", "));
    const deep = document.querySelectorAll(".subnav .subnav, .body .subnav .tab .subnav").length;
    const second = document.querySelectorAll(".body .subnav").length;
    add("N3", deep === 0 ? "PASS" : "FAIL", `${second === 0 ? "one level" : "two levels, the second in the body"}${deep === 0 ? "" : `; ${deep} third-level`}`);
    const search = head.querySelectorAll("input").length;
    add("N4", search === 0 ? "PASS" : "FAIL", search === 0 ? "no search box in the bar" : `${search} input(s) in the bar`);
    if (who !== null && ctx.at === "me") record.X15 = `the bar's own read failed; the identity slot draws "${text(who)}"`;
    else if (who !== null) {
      const parts = text(who).split(" · ");
      add("N5", parts.length === 3 && parts.every((p) => p.trim() !== "") ? "PASS" : "FAIL", `"${text(who)}" — ${parts.length} clause(s)`);
    }
    const active = text(head.querySelector(".tab.on") ?? head).toLowerCase();
    const repeats = all(".body h1, .body h2, .body h3, .body .title").filter((h) => text(h).toLowerCase() === active);
    add("N6", repeats.length === 0 ? "PASS" : "FAIL", repeats.length === 0 ? "no heading repeats the active tab" : repeats.map(name).join(", "));
    if (body !== null) add("N8", px(cs(body).paddingTop) > 0 ? "PASS" : "FAIL", `.body padding-top ${cs(body).paddingTop}`);
    record.N7 = { strip: document.querySelector(".body .strip") !== null, chips: document.querySelector(".body .chips") !== null };
  }

  // ---------------------------------------------------------------- §4 · §6 the list
  const lists = all(".body .list > table, .body .card table:has(~ .pager)");
  for (const table of lists) {
    const where = table.closest(".card") === null ? "the list" : "the card's list";
    const s = cs(table);
    // L1: nothing between the list and the body carries a fill, a border or a radius.
    const wrappers = [];
    for (let a = table.parentElement; a !== null && a !== body; a = a.parentElement) {
      const w = cs(a);
      if (w.backgroundColor !== C.none || px(w.borderTopWidth) > 0 || px(w.borderTopLeftRadius) > 0) wrappers.push(name(a).replace(/ ".*/, ""));
    }
    const l1 = [];
    if (Math.abs(table.getBoundingClientRect().width - table.parentElement.clientWidth + box(table.parentElement, "Left") + box(table.parentElement, "Right")) > 1) l1.push(`width ${Math.round(table.getBoundingClientRect().width)} of ${table.parentElement.clientWidth}`);
    if (s.borderCollapse !== "collapse") l1.push(`border-collapse ${s.borderCollapse}`);
    if (s.fontSize !== "13px") l1.push(`font-size ${s.fontSize}`);
    if (wrappers.length > 0) l1.push(`wrapped in ${wrappers.join(" › ")}`);
    add("L1", l1.length === 0 ? "PASS" : "FAIL", `${where}: ${l1.length === 0 ? "bare, 100%, collapse, 13px" : l1.join("; ")}`);
    judge("L2", [...table.querySelectorAll("th")].filter(shown), (th) => {
      const t = cs(th); const out = [];
      if (pad(th) !== "8px 10px 8px 10px") out.push(`padding ${pad(th)}`);
      if (t.fontSize !== "11px") out.push(`font-size ${t.fontSize}`);
      if (t.fontWeight !== "500") out.push(`weight ${t.fontWeight}`);
      if (t.textTransform !== "uppercase") out.push(t.textTransform);
      if (Math.abs(em(th) - 0.08) > 0.003) out.push(`tracking ${em(th)}em`);
      if (t.color !== C.faint) out.push(`color ${t.color}`);
      if (t.borderBottomWidth !== "1px" || t.borderBottomColor !== C.line) out.push(`rule ${t.borderBottomWidth} ${t.borderBottomColor}`);
      return out.join("; ");
    });
    const cells = [...table.querySelectorAll("td")].filter(shown);
    judge("L3", cells, (td) => {
      const t = cs(td); const out = [];
      if (pad(td) !== "10px 10px 10px 10px") out.push(`padding ${pad(td)}`);
      if (t.borderBottomWidth !== "1px" || t.borderBottomColor !== C.line) out.push(`rule ${t.borderBottomWidth} ${t.borderBottomColor}`);
      if (t.verticalAlign !== "top") out.push(`vertical-align ${t.verticalAlign}`);
      return out.join("; ");
    });
    const rows = [...table.querySelectorAll("tr")].filter((tr) => tr.querySelector("td") !== null);
    if (rows.length > 0) {
      const last = rows[rows.length - 1].querySelector("td");
      add("L5", cs(last).borderBottomWidth === "1px" ? "PASS" : "FAIL", `${where}: last row's rule ${cs(last).borderBottomWidth}`);
    }
  }

  // L4 — every selected row on the page, paged or not: a tint and nothing else.
  const selected = all("tr.sel");
  judge("L4", selected.flatMap((tr) => [...tr.children]), (td) => {
    const out = [];
    if (!/color\(srgb 0\.50\d+ 0\.54\d+ 0\.97\d+ \/ 0\.08\)|rgba\(129, 140, 248, 0\.08\)/.test(cs(td).backgroundColor)) out.push(`tint ${cs(td).backgroundColor}`);
    if (cs(td).boxShadow !== "none" || px(cs(td).borderLeftWidth) > 0) out.push("an inset bar beside the tint");
    return out.join("; ");
  });

  // ---------------------------------------------------------------- §5 density (roles above)
  judge("D2", all("th"), (th) => (Math.abs(em(th) - 0.08) > 0.003 ? `table header at ${em(th)}em` : ""));
  judge("D2", all("label.lbl"), (l) => (Math.abs(em(l) - 0.07) > 0.003 ? `field label at ${em(l)}em` : ""));
  judge("D2", all(".sect, .card.accent > h3"), (l) => (Math.abs(em(l) - 0.04) > 0.003 ? `section label at ${em(l)}em` : ""));
  judge("D3", all(".note, .legend, .tl .ev span"), (n) => {
    const s = cs(n); const out = [];
    if (s.fontSize !== "12px") out.push(`note text at ${s.fontSize}`);
    if (s.lineHeight !== "19.8px") out.push(`line-height ${s.lineHeight}`);
    return out.join("; ");
  });
  judge("D4", all(".tl .ev span"), (n) => (cs(n).color !== C.muted ? `quiet text at ${cs(n).color === C.faint ? "ink-faint" : cs(n).color}` : ""));
  const faintText = [...document.querySelectorAll("body *")].filter((e) => shown(e) && cs(e).color === C.faint && [...e.childNodes].some((c) => c.nodeType === 3 && c.textContent.trim() !== "")
    && !e.matches("th, label.lbl, .sect, .card.accent > h3, .btn.off, .btn.pg[disabled], .fail-label, .fail-fk, .kv .k"));
  record.faint = [...new Set(faintText.map((e) => name(e).replace(/ ".*/, "")))];
  const noteBold = all(".note b, .fail-why b, .fail-note b");
  if (noteBold.length > 0) add("D5", "OPEN", `emphasis in a note built at ${[...new Set(noteBold.map((b) => cs(b).fontWeight))].join("/")} (64c)`);

  // ---------------------------------------------------------------- §6 the pager
  const pagers = all(".pager");
  for (const p of pagers) {
    const list = p.previousElementSibling;
    const said = text(p.firstElementChild ?? p);
    const arrows = [...p.querySelectorAll("button[aria-label]")];
    record.pager = said;
    add("G9", list !== null && (list.matches(".list") || list.matches("table")) ? "PASS" : "FAIL",
      list === null ? "the pager has no previous sibling" : `the pager follows ${name(list).replace(/ ".*/, "")}`);
    if (ctx.state === "1P") add("G3", /^showing \d+–\d+ of \d+/.test(said) && arrows.every((a) => a.disabled) ? "PASS" : "FAIL", `"${said}", arrows ${arrows.map((a) => (a.disabled ? "off" : "live")).join("/")}`);
    if (ctx.state === "MP" || ctx.state === "ML") {
      const m = /showing (\d+)–(\d+) of (\d+)/.exec(said);
      const shownRows = list?.querySelectorAll("tr:has(td)").length ?? -1;
      add("G4", m !== null && Number(m[2]) - Number(m[1]) + 1 === shownRows ? "PASS" : "FAIL", `"${said}" over ${shownRows} rows`);
    }
    if (ctx.state === "E1") add("G5", /^no rows on this page · \d+ in the list$/.test(said) ? "PASS" : "FAIL", `"${said}"`);
    if (ctx.state === "E0") add("G11", "OPEN", `E0 draws "${said}" in the pager at the list's floor, page button 1 disabled`);
    const inCardList = p.parentElement !== body;
    if (inCardList && ["MP", "1P", "E0", "E1", "ML"].includes(ctx.state)) {
      add("G6", "NA", "a paged list inside a card: §6's screen list does not govern it — the page of cards scrolls as a page");
      add("G7", "NA", "a paged list inside a card: the card is sized by its content, so there is no free space for the list to take");
    }
    // G6 — only the list scrolls: the body does not, the list is the scroller, nothing sticky.
    if (!inCardList && body !== null && list !== null && ["MP", "1P", "E0"].includes(ctx.state)) {
      const out = [];
      if (body.scrollHeight > body.clientHeight + 1) out.push(`body scrolls ${body.scrollHeight}/${body.clientHeight}`);
      if (cs(p).position === "sticky") out.push("pager is sticky");
      if (!["auto", "scroll"].includes(cs(list).overflowY)) out.push(`the list is not the scroll container (overflow-y ${cs(list).overflowY})`);
      if (list.scrollHeight > list.clientHeight + 1 && !["auto", "scroll"].includes(cs(list).overflowY)) out.push(`list overflows unscrollably ${list.scrollHeight}/${list.clientHeight}`);
      add("G6", out.length === 0 ? "PASS" : "FAIL", out.length === 0 ? "the body holds still; the list scrolls" : out.join("; "));
    }
    // G7 — the pager is the floor, and the list GREW to put it there (measured, not inferred).
    // What follows the pager is allowed (§6's CORE-Q28 measurement keeps a panel below the
    // list on screen), so the floor is the END of the container's content: the list must
    // have grown until nothing below the last thing drawn is left unused.
    if (!inCardList && body !== null && list !== null && ["1P", "E0", "E1", "ML"].includes(ctx.state)) {
      const holder = p.parentElement;
      const kids = [...holder.children].filter(shown);
      const lastKid = kids[kids.length - 1];
      const end = lastKid.getBoundingClientRect().bottom + px(cs(lastKid).marginBottom);
      const floor = holder.getBoundingClientRect().bottom - box(holder, "Bottom") - px(cs(holder).borderBottomWidth);
      const unused = Math.round(floor - end);
      const rowsEnd = [...list.querySelectorAll("tr")].reduce((y, tr) => Math.max(y, tr.getBoundingClientRect().bottom), list.getBoundingClientRect().top);
      const grew = Math.round(list.getBoundingClientRect().bottom - rowsEnd);
      const ok = Math.abs(unused) <= 2 && grew > 0;
      const after = lastKid === p ? "the pager is last" : `${kids.length - 1 - kids.indexOf(p)} element(s) after the pager`;
      record.G7 = { floor: Math.round(floor), end: Math.round(end), holder: name(holder).replace(/ ".*/, ""), holderBottom: Math.round(holder.getBoundingClientRect().bottom), viewport: innerHeight };
      add("G7", ok ? "PASS" : "FAIL", `${unused}px unused below the content (${after}); list box ${grew}px taller than its rows${holder.classList.contains("card") ? "; in a card, the card is the floor" : ""}`);
    }
  }
  // G8 — a screen WITHOUT a pager clips nothing past the body; and a body clipped for a pager has nothing beyond it.
  if (body !== null && !failing) {
    const clipped = cs(body).overflowY === "hidden" && body.scrollHeight > body.clientHeight + 1;
    if (pagers.length === 0) add("G8", clipped ? "FAIL" : "PASS", clipped ? `body clips ${body.scrollHeight - body.clientHeight}px with no pager` : `body ${cs(body).overflowY}, nothing clipped`);
    else if (clipped) add("G8", "FAIL", `body:has(.pager) clips ${body.scrollHeight - body.clientHeight}px of this screen — content below the fold is unreachable`);
  }

  // ---------------------------------------------------------------- §9 overlays
  for (const o of all(".sheet, .dlg")) {
    const scrim = o.parentElement; const r = o.getBoundingClientRect(); const out = [];
    const frame = scrim.parentElement.getBoundingClientRect();
    if (o.classList.contains("sheet")) {
      if (Math.round(r.width) !== 440) out.push(`sheet width ${Math.round(r.width)}`);
      if (Math.abs(r.right - frame.right) > 1 || Math.abs(r.height - frame.height) > 1) out.push("not full height at the right edge");
    } else {
      if (Math.round(r.width) !== 520) out.push(`dialog width ${Math.round(r.width)}`);
      if (Math.abs((r.left + r.right) / 2 - (frame.left + frame.right) / 2) > 2) out.push("not centred");
    }
    for (const part of [".dh", ".db", ".df"]) if (o.querySelector(`:scope > ${part}`) === null) out.push(`no ${part}`);
    add("O1", out.length === 0 ? "PASS" : "FAIL", `${o.classList.contains("sheet") ? "sheet" : "dialog"} "${o.getAttribute("aria-label")}": ${out.length === 0 ? "as §9" : out.join("; ")}`);
    const pos = [cs(scrim).position, cs(o).position];
    add("O2", pos.every((x) => x === "absolute" || x === "relative" || x === "static") && !pos.includes("fixed") ? "PASS" : "FAIL", `scrim ${pos[0]}, surface ${pos[1]}`);
    add("O3", scrim.parentElement.querySelector(":scope > .body") !== null || scrim.previousElementSibling?.classList.contains("body") ? "PASS" : "FAIL", `the scrim's parent holds ${scrim.parentElement.querySelector(":scope > .body") ? "the .body too" : "no .body"}`);
    if (ctx.write && ctx.writes === 0) add("O5", "NA", `"${o.getAttribute("aria-label")}": its action sends no write — it keeps a choice for the tab's own Save`);
    else if (ctx.write) {
      const refused = o.querySelector(".said.bad");
      add("O5", refused !== null ? "PASS" : "FAIL", refused !== null ? `still open, saying "${text(refused).slice(0, 70)}"` : "no reason drawn in the open overlay");
    }
  }
  if (ctx.write && ctx.writes > 0 && all(".sheet, .dlg").length === 0) add("O5", "FAIL", `the overlay closed on a failed write (${ctx.writes} write(s) refused)`);

  // ---------------------------------------------------------------- §10 fields
  judge("F2", all("label.lbl"), (l) => {
    const s = cs(l); const out = [];
    if (s.fontSize !== "11px") out.push(`label ${s.fontSize}`);
    if (s.textTransform !== "uppercase") out.push(s.textTransform);
    if (Math.abs(em(l) - 0.07) > 0.003) out.push(`${em(l)}em`);
    if (s.color !== C.faint) out.push(`color ${s.color}`);
    return out.join("; ");
  });
  judge("F2", all(".field"), (f) => {
    const s = cs(f); const out = [];
    if (pad(f) !== "9px 12px 9px 12px") out.push(`padding ${pad(f)}`);
    if (s.borderTopLeftRadius !== "10px") out.push(`radius ${s.borderTopLeftRadius}`);
    if (s.borderTopColor !== C.lineStrong) out.push(`border ${s.borderTopColor}`);
    if (!/255, 255, 255|232, 235, 244|0\.9098|0\.92/.test(s.backgroundColor) && !/color\(srgb/.test(s.backgroundColor)) out.push(`background ${s.backgroundColor}`);
    return out.join("; ");
  });

  // ---------------------------------------------------------------- §13 a screen that cannot read
  if (failing) {
    const fb = document.querySelector(".fail-body"); const st = fb.querySelector(".fail");
    const rows = [...document.querySelectorAll(".body td")].filter(shown).length;
    add("X1", rows === 0 ? "PASS" : "FAIL", rows === 0 ? "the failure is drawn and no row is" : `${rows} cells drawn beside the failure`);
    const fr = fb.getBoundingClientRect(); const sr = st.getBoundingClientRect();
    const centred = Math.abs((sr.left + sr.right) / 2 - (fr.left + fr.right) / 2) <= 2 && Math.abs((sr.top + sr.bottom) / 2 - (fr.top + fr.bottom) / 2) <= 2;
    const wanted = Math.min(560, fr.width * 0.92);
    add("X2", centred && Math.abs(sr.width - wanted) <= 1 ? "PASS" : "FAIL", `state ${Math.round(sr.width)}px (want ${Math.round(wanted)}), ${centred ? "centred" : "off centre"}`);
  }
  const want = { unanswered: C.warn, forbidden: C.muted, unadmitted: C.muted, ungranted: C.muted, undecidable: C.bad, faulted: C.bad };
  for (const m of all(".fail-mark, .wf-mark")) {
    const cause = [...m.classList].find((c) => c.startsWith("fail-") && c !== "fail-mark")?.slice(5);
    const svg = m.querySelector("svg");
    const out = [];
    if (svg === null || svg.getAttribute("stroke") !== "currentColor") out.push("no svg stroked currentColor");
    if (cs(m).color !== want[cause]) out.push(`${cause} drawn ${cs(m).color}`);
    add("X3", out.length === 0 ? "PASS" : "FAIL", `${m.classList.contains("wf-mark") ? "widget" : "screen"} ${cause}: ${out.length === 0 ? "line glyph, its colour" : out.join("; ")}`);
  }
  for (const l of all(".fail-label")) {
    const s = cs(l); const out = [];
    if (s.fontSize !== "11px") out.push(s.fontSize);
    if (Math.abs(em(l) - 0.1) > 0.003) out.push(`${em(l)}em`);
    if (s.textTransform !== "uppercase") out.push(s.textTransform);
    if (s.color !== C.faint) out.push(`color ${s.color}`);
    add("X4", out.length === 0 ? "PASS" : "FAIL", out.length === 0 ? "11px .1em uppercase faint" : out.join("; "));
    add("X4", "OPEN", `mono stack built as ${s.fontFamily.slice(0, 40)} (64d item 4)`);
  }
  for (const n of all(".fail-said")) add("X5", cs(n).fontSize === "19px" && cs(n).fontWeight === "600" ? "PASS" : "FAIL", `${cs(n).fontSize} ${cs(n).fontWeight}`);
  for (const n of all(".fail-why")) add("X6", cs(n).fontSize === "14px" && cs(n).color === C.muted ? "PASS" : "FAIL", `${cs(n).fontSize} ${cs(n).color === C.muted ? "muted" : cs(n).color}; ${n.querySelectorAll("b").length} emphasised run(s)`);
  if (failing && ctx.cause) {
    const acts = [...document.querySelectorAll(".fail-acts button")].map(text);
    const expect = ctx.cause === "unanswered" ? "retry" : ["faulted", "undecidable"].includes(ctx.cause) ? "copy" : "none";
    const got = acts.length === 0 ? "none" : acts.some((a) => /try again/i.test(a)) ? "retry" : acts.some((a) => /copy/i.test(a)) ? "copy" : acts.join("/");
    add("X7", got === expect ? "PASS" : "FAIL", `${ctx.cause}: ${got}${acts.length ? ` ("${acts.join('", "')}")` : ""}`);
    const btn = document.querySelector(".fail-acts button");
    if (btn) add("X7", "OPEN", `button fill built as ${btn.className} (64d item 1)`);
    const dts = [...document.querySelectorAll(".fail-facts dt")].map(text);
    const permission = document.querySelector(".fail-facts dd b");
    add("X8", dts.length > 0 && document.querySelector(".fail-facts").tagName === "DL" && permission !== null ? "PASS" : "FAIL", `dl: ${dts.join(" · ")}; permission ${permission ? `set apart "${text(permission)}"` : "not set apart"}`);
    const at = [...document.querySelectorAll(".fail-facts dt")].find((d) => /at/i.test(text(d)));
    if (at) add("X8", "OPEN", `the moment built as "${text(at.nextElementSibling)}" (64d item 8)`);
    const said = text(document.querySelector(".fail") ?? document.body);
    if (["forbidden", "unadmitted", "ungranted"].includes(ctx.cause)) {
      const routes = /administrator|ask (your|a|the)|contact|manager|supervisor/i.exec(said);
      add("X9", routes === null ? "PASS" : "FAIL", routes === null ? `${ctx.cause}: names the grant and stops` : `routes to a person: "${routes[0]}"`);
    }
    const namesApp = /Room Care/.test(text(document.querySelector(".fail-said")));
    const out11 = ctx.cause === "unadmitted" ? (namesApp ? "" : "does not name the application")
      : ctx.cause === "ungranted" ? (/account/i.test(said) ? "" : "does not name the account")
      : ctx.cause === "undecidable" ? (/\byou\b|account/i.test(text(document.querySelector(".fail-said"))) ? "names the person" : "") : null;
    if (out11 !== null) add("X11", out11 === "" ? "PASS" : "FAIL", `${ctx.cause}: "${text(document.querySelector(".fail-said"))}"${out11 ? ` — ${out11}` : ""}`);
  }
  if (ctx.widgets) {
    for (const card of all(".wcard")) {
      if (card.querySelector("dl") !== null) add("X12", "FAIL", `${name(card)} carries the facts on the card`);
    }
    record.X13 = [...new Set(all(".wcard").map((c) => Math.round(c.getBoundingClientRect().height)))];
    record.X14 = [...new Set(all(".wf-open").map(text))];
  }
  record.partial = !failing && document.querySelector(".body .fail-body, .body .fail") !== null;
  return { lines, record };
}
