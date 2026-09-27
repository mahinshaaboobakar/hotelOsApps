// How the audit measures a rendered page, and how it records a finding — the vocabulary
// every checklist line is written in.
//
// **Not a module**: `run.mjs` reads this file and evaluates it in the page beside
// `probe.js` and `cannotread.js`, which is why it is a bare function expression and `.js`
// rather than `.mjs` like the harness's own modules. It is called once per case and hands
// back the vocabulary, the two collections a line writes into, and the two facts every
// section asks first (is there a body; is this page a failure).
//
// **Changing anything here changes what every line measures**, which is the one edit in
// this directory whose effect is not local: `cs` is the only reader of computed style,
// `shown` the only definition of visible, `all` the only selector that excludes what is
// not, and `judge` the only counter of a repeated finding. A previous run's output stops
// being comparable the moment one of them moves, so the split that created this file was
// proved against a run whose answer was already known, with nothing in it changed.
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
  return { ctx, lines, record, add, cs, px, text, name, shown, all, C, judge, box, pad, em, body, failing };
}
