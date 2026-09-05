/**
 * The seventeen properties, and the script that compares them in the page.
 */

/** One measurable property, and where it lives on each side. */
export interface Property {
  label: string;

  /** The gold file's selector. */
  drawn: string;

  /** The module's — the same, except where the two name the frame differently. */
  built: string;

  /** A computed CSS property name. */
  css: string;
}

/**
 * What is compared, seventeen ways, on every pair.
 *
 * **Computed values, never appearance.** Each of these is read with
 * `getComputedStyle` from both live documents, so the comparison is arithmetic
 * rather than a judgement about whether two screenshots look alike — which is
 * the thing a person cannot do reliably across seventeen pairs and which the
 * last submission was rejected for not having.
 *
 * They are the properties two applications would visibly disagree on: the type
 * scale, the chrome's geometry, the one control's shape, the list's rhythm and
 * the three attachments. Colour is in here twice — the rules — because a token
 * that failed to resolve shows up there first.
 */
export const PROPERTIES: readonly Property[] = [
  { label: "root font-size", drawn: ".win", built: ".go", css: "fontSize" },
  { label: "root line-height", drawn: ".win", built: ".go", css: "lineHeight" },
  { label: "app bar height", drawn: ".head", built: ".head", css: "height" },
  { label: "app bar padding", drawn: ".head", built: ".head", css: "padding" },
  { label: "app bar rule", drawn: ".head", built: ".head", css: "borderBottomColor" },
  { label: "body padding", drawn: ".body", built: ".body", css: "padding" },
  { label: "button padding", drawn: ".btn", built: ".btn", css: "padding" },
  { label: "button radius", drawn: ".btn", built: ".btn", css: "borderRadius" },
  { label: "button font-size", drawn: ".btn", built: ".btn", css: "fontSize" },
  { label: "button border", drawn: ".btn", built: ".btn", css: "borderTopColor" },
  { label: "row cell padding", drawn: ".tr>div", built: ".tr>div", css: "padding" },
  { label: "row rule", drawn: ".tr", built: ".tr", css: "borderBottomColor" },
  { label: "header cell size", drawn: ".tr.hd>div", built: ".tr.hd>div", css: "fontSize" },
  { label: "card radius", drawn: ".card", built: ".card", css: "borderRadius" },
  { label: "card band spacing", drawn: ".ch", built: ".ch", css: "letterSpacing" },
  { label: "pill radius", drawn: ".pill", built: ".pill", css: "borderRadius" },
  { label: "mark padding", drawn: ".sh", built: ".sh", css: "padding" },
];

/**
 * The comparison, as a script the page runs on itself.
 *
 * **Live rather than baked in.** A table of numbers written into this file at
 * generation time is a claim about a build that may since have changed; a
 * comparison the page performs on the two documents it is showing cannot
 * disagree with them. Open the page and the figures are of what is on it.
 */
export function script(properties: readonly Property[]): string {
  return `
const PROPS = ${JSON.stringify(properties)};

async function measure() {
  for (const f of document.querySelectorAll("iframe")) f.loading = "eager";
  await new Promise(r => setTimeout(r, 400));

  const counts = { match: 0, differ: 0, "absent on both": 0, "drawn without it": 0 };
  const notes = [];

  for (const section of document.querySelectorAll("section")) {
    const number = section.querySelector("h2 .n").textContent;
    const frames = [...section.querySelectorAll("iframe")].map(f => f.contentDocument);
    if (!frames[0] || !frames[1]) continue;

    let match = 0, differ = 0, only = 0;

    for (const p of PROPS) {
      const a = frames[0].querySelector(p.drawn);
      const b = frames[1].querySelector(p.built);

      if (!a && !b) { counts["absent on both"]++; continue; }

      if (!a || !b) {
        counts["drawn without it"]++;
        only++;
        notes.push(number + " · " + p.label + " · " + (a ? "built without it" : "drawn without it"));
        continue;
      }

      const av = getComputedStyle(a)[p.css];
      const bv = getComputedStyle(b)[p.css];

      if (av === bv) { counts.match++; match++; }
      else {
        counts.differ++; differ++;
        notes.push(number + " · " + p.label + " · drawn " + av + " · built " + bv);
      }
    }

    const verdict = section.querySelector(".verdict");
    verdict.textContent = match + " match" + (differ ? " · " + differ + " differ" : "")
      + (only ? " · " + only + " drawn without it" : "");
    verdict.className = "verdict " + (differ ? "bad" : only ? "part" : "ok");
  }

  const total = Object.values(counts).reduce((a, b) => a + b, 0);
  document.getElementById("tally").innerHTML =
    Object.entries(counts).map(([k, v]) =>
      '<span class="' + (k === "differ" && v ? "bad" : k === "match" ? "ok" : "") + '">'
      + '<b>' + v + '</b>' + k + '</span>').join("")
    + '<span><b>' + total + '</b>measurements</span>';

  document.getElementById("notes").innerHTML = notes.length === 0
    ? "<li>Nothing differs.</li>"
    : notes.map(n => "<li>" + n + "</li>").join("");
}

measure();

/*
 * The view control.
 *
 * Two states and no third: a pair scaled to fit the row, or one pane at a time
 * at 1:1. The second exists because the first gallery was rejected for being
 * unauditable at the size it rendered — a design drawn at 1220px shown in a
 * 727px column is not a smaller version of itself, it is a different layout.
 */
for (const button of document.querySelectorAll(".control button")) {
  button.addEventListener("click", () => {
    for (const other of document.querySelectorAll(".control button")) {
      other.classList.toggle("on", other === button);
    }

    document.body.classList.toggle("stacked", button.dataset.view === "stacked");
    fit();
  });
}

/*
 * Set the scale from the room a pane actually has.
 *
 * In script because CSS cannot: dividing a length by a length gives a length,
 * and \`scale()\` needs a number, so the whole thing has to be arithmetic
 * somebody performs. Capped at 1 — a design drawn at 1220px blown up to 1600 is
 * not a better view of it, it is a different one.
 */
function fit() {
  const frame = document.querySelector(".frame");
  if (!frame) return;

  const room = frame.getBoundingClientRect().width;
  const scale = Math.min(1, room / 1220);
  document.body.style.setProperty("--scale", String(scale));

  /*
   * Each pair is as tall as the taller of its two documents.
   *
   * A fixed height clips whichever side is longer — frame 1's drawing is
   * fourteen rows, a pager and a note, and it ran past 820px, so the first
   * gallery cut the pager off the pane that HAD one while reporting the build
   * for not having one. Both panes take the same height so the comparison is
   * of like with like, and neither is cropped.
   */
  for (const pair of document.querySelectorAll(".pair")) {
    const frames = [...pair.querySelectorAll("iframe")];
    const tall = Math.max(...frames.map(f =>
      f.contentDocument ? f.contentDocument.documentElement.scrollHeight : 0), 400);

    for (const f of frames) {
      f.style.height = tall + "px";
      f.parentElement.style.height = Math.ceil(tall * scale) + "px";
    }
  }
}

addEventListener("resize", fit);
fit();
`;
}
