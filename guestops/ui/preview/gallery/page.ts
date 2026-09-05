/**
 * The gallery page itself — seventeen pairs, and what separates them.
 */

import type { Pair } from "./frames";
import { PROPERTIES, script } from "./measure";

/** One pair, ready to draw. */
export interface Built {
  pair: Pair;
  heading: string;
  drawn: string;
  built: string;
}

/**
 * Assemble the page.
 *
 * @param pairs every frame and its screen
 * @param goldCss the gold file's own stylesheet
 * @param tokenCss the seventeen published tokens, at the shell's values
 * @param realmCss what the shell writes into a module's document
 * @returns the page content — **no document wrapper**, because the artifact
 * host supplies one and a second `<html>` inside it is a document nobody parses
 */
export function page(
  pairs: readonly Built[],
  goldCss: string,
  tokenCss: string,
  realmCss: string,
): string {
  const sections = pairs.map((one) => section(one, goldCss, tokenCss, realmCss)).join("\n");

  return `<title>Seventeen Pairs</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Newsreader:opsz,wght@6..72,400;6..72,500;6..72,600&display=swap">
<style>${PAGE}</style>

<header>
  <p class="eyebrow">GuestOps &middot; APPS-Q4 Part A</p>
  <h1>Every approved frame, beside what was built.</h1>

  <p class="lede">Seventeen pairs. On the left, the gold file's own markup under
  its own stylesheet. On the right, <code>application.ts</code> mounted against
  the preview harness's host and driven to the screen by the same clicks a
  person would make &mdash; the seventeen published tokens injected, and nothing
  else.</p>

  <p>Both panes are <b>live documents, not pictures</b>. Select the text, zoom
  the page, read the markup. They are separate frames because both stylesheets
  define <code>.tr</code>, <code>.btn</code> and <code>.card</code>: in one
  document the later one would win, and the gallery would be comparing a hybrid
  against itself.</p>

  <h2 class="rule">Seventeen properties, on all seventeen pairs</h2>

  <p>Every figure below is read with <code>getComputedStyle</code> from the two
  documents on this page, <b>when you open it</b> &mdash; not written in when the
  page was generated. A table baked in at build time is a claim about a build
  that may since have changed; this cannot disagree with what it is showing.</p>

  <div id="tally" class="tally"><span><b>&middot;&middot;&middot;</b>measuring</span></div>

  <details>
    <summary>Everything that is not a match</summary>
    <ul id="notes"><li>measuring&hellip;</li></ul>
  </details>

  <p class="quiet">Dark only, deliberately. Both panes are HotelOS surfaces and
  the harness injects the dark theme's published values; a light frame around
  them would be a colour scheme no property runs.</p>
</header>

${sections}
<script>${script(PROPERTIES)}</script>`;
}

/** One pair. */
function section(one: Built, goldCss: string, tokenCss: string, realmCss: string): string {
  return `<section>
  <h2>
    <span class="n">${one.pair.number}</span>
    <span class="t">${escaped(one.heading)}</span>
    <span class="verdict">&middot;&middot;&middot;</span>
  </h2>
  <div class="pair">
    ${pane("The approved frame", frameDoc(goldCss, one.drawn))}
    ${pane("What was built", builtDoc(tokenCss, realmCss, one.built))}
  </div>
</section>`;
}

/** A pane, holding a whole document. */
function pane(label: string, document_: string): string {
  return `<figure><figcaption>${label}</figcaption>`
    + `<iframe loading="lazy" srcdoc="${escaped(document_)}"></iframe></figure>`;
}

/**
 * The drawing, in the gold file's own terms.
 *
 * The window's own margin and border come off, because the gold file lays its
 * frames down a scrolling page and here each one is alone in a pane. The only
 * other change is a background: the mockup page paints one and the frame does
 * not.
 */
function frameDoc(css: string, body: string): string {
  return `<!doctype html><html><head><meta charset="utf-8"><style>${css}
    body{margin:0;padding:0;background:#0b0d14}
    .win{margin:0;border-radius:0;border:0}</style></head><body>${body}</body></html>`;
}

/**
 * The module, in a realm.
 *
 * The realm's own rules are injected exactly as `realm.ts` writes them, because
 * a pane that painted its own background would hide the defect SHELL-Q33
 * closed — the root paints, and the body is transparent so the root's paint
 * shows through rather than offering a second opinion one rule below the one
 * that has it.
 */
function builtDoc(tokenCss: string, realmCss: string, body: string): string {
  return `<!doctype html><html><head><meta charset="utf-8"><style>${tokenCss}
    ${realmCss}</style></head><body>${body}</body></html>`;
}

/** Attribute-safe: an attribute is not a text node. */
function escaped(text: string): string {
  return text
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

/**
 * The gallery's own chrome.
 *
 * **Deliberately not the product's.** The panes are HotelOS surfaces and the
 * frame around them is a document *about* them — so the headings are set in a
 * serif that appears nowhere in the product, and the ground is a shade cooler
 * and darker than a module's, which is what makes each pane read as an object
 * sitting on the page rather than as more page.
 */
const PAGE = `
:root{
  color-scheme:dark;
  --ground:#07080d;
  --raised:#0e1017;
  --edge:rgb(255 255 255 / .09);
  --ink:#e8ebf4;
  --muted:#8b93a7;
  --faint:#5a6172;
  --accent:#818cf8;
  --ok:#34d399;
  --warn:#fbbf24;
  --bad:#f87171;
  --sans:ui-sans-serif,system-ui,-apple-system,"Segoe UI",sans-serif;
  --serif:"Newsreader",Georgia,"Times New Roman",serif;
  --mono:ui-monospace,SFMono-Regular,Menlo,monospace;
}
body{margin:0;padding:40px 26px 80px;background:var(--ground);color:var(--ink);
  font:14px/1.65 var(--sans);-webkit-font-smoothing:antialiased}
header{max-width:76ch;margin:0 auto 46px}
.eyebrow{margin:0 0 14px;font-size:11px;letter-spacing:.14em;text-transform:uppercase;
  color:var(--accent)}
h1{margin:0 0 18px;font:400 34px/1.2 var(--serif);letter-spacing:-.015em;
  text-wrap:balance;color:var(--ink)}
header p{margin:0 0 12px;color:var(--muted);font-size:13.5px}
header p.lede{color:var(--ink);font-size:15px;line-height:1.6}
header p.quiet{margin-top:20px;color:var(--faint);font-size:12.5px}
code{font:12.5px var(--mono);color:#a8b0c4}
b{font-weight:600;color:var(--ink)}
h2.rule{margin:34px 0 10px;padding-top:22px;border-top:1px solid var(--edge);
  font:500 17px/1.3 var(--serif);letter-spacing:-.01em;color:var(--ink)}

.tally{display:flex;flex-wrap:wrap;gap:26px;margin:16px 0;padding:15px 20px;
  border:1px solid var(--edge);border-radius:12px;background:var(--raised)}
.tally span{display:flex;flex-direction:column-reverse;gap:3px;font-size:10.5px;
  letter-spacing:.1em;text-transform:uppercase;color:var(--faint)}
.tally b{font:400 26px/1 var(--serif);color:var(--ink);
  font-variant-numeric:tabular-nums}
.tally .ok b{color:var(--ok)}
.tally .bad b{color:var(--bad)}

details{margin-top:6px}
summary{cursor:pointer;font-size:12.5px;color:var(--muted);padding:4px 0}
summary:hover{color:var(--ink)}
#notes{margin:8px 0 0;padding-left:20px;color:var(--muted);font-size:12.5px;
  line-height:1.85;font-variant-numeric:tabular-nums}

section{max-width:2760px;margin:0 auto 44px}
h2{display:flex;align-items:baseline;gap:13px;margin:0 0 13px;padding-bottom:10px;
  border-bottom:1px solid var(--edge);font:500 16px/1.35 var(--serif);
  letter-spacing:-.005em}
h2 .n{flex:none;display:inline-grid;place-items:center;min-width:32px;height:23px;
  padding:0 8px;border-radius:7px;background:rgb(129 140 248 / .15);color:var(--accent);
  font:600 11.5px/1 var(--sans);font-variant-numeric:tabular-nums;letter-spacing:.02em}
h2 .t{min-width:0;color:var(--ink)}
.verdict{margin-left:auto;flex:none;font:400 11.5px/1 var(--sans);color:var(--faint);
  font-variant-numeric:tabular-nums;letter-spacing:.02em;white-space:nowrap}
.verdict.ok{color:var(--ok)}
.verdict.part{color:var(--warn)}
.verdict.bad{color:var(--bad)}

.pair{display:grid;grid-template-columns:1fr 1fr;gap:18px;align-items:start}
figure{margin:0;min-width:0}
figcaption{margin-bottom:8px;font-size:10.5px;letter-spacing:.1em;
  text-transform:uppercase;color:var(--faint)}
iframe{display:block;width:100%;height:780px;border:1px solid var(--edge);
  border-radius:12px;background:#0b0d14}

@media (max-width:1180px){.pair{grid-template-columns:1fr}}
@media (prefers-reduced-motion:reduce){*{animation:none!important;transition:none!important}}
`;
