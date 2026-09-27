// §13 of the page-64 checklist — what a screen that cannot read must draw. X1–X14, and
// the record of a failure inside a working body.
//
// **64b's own classes (`.fail-*`) are governed by §13's lines, not by §5's**, which is why
// this is a file and not a size split: its selectors are disjoint from every other
// section's, and the density roles §5 measures deliberately do not reach them.
//
// **Not a module**, like `measure.js`: `run.mjs` evaluates it in the page and `probe.js`
// calls it with the vocabulary, so every line here writes into the same collections as
// §2–§10 and cannot disagree with them about what is visible.
({ ctx, lines, record, add, cs, px, text, name, shown, all, C, judge, box, pad, em, body, failing }) => {
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
}
