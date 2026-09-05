/**
 * The two conformance tables, drawn.
 */

import { PAGINATION, WIDGETS, WIDGET_CANVAS, WIDGET_FINDING } from "./conformance";

/** Attribute- and text-safe. */
function escaped(text: string): string {
  return text
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

/** Emphasis the source marks with `**`, kept rather than stripped. */
function rich(text: string): string {
  return escaped(text).replace(/\*\*(.+?)\*\*/gu, "<b>$1</b>");
}

/** The pagination conformance table — one row per screen that draws a list. */
export function pagination(): string {
  const rows = PAGINATION.map((row) => `<tr>
    <th scope="row">${escaped(row.screen)}</th>
    <td>${escaped(row.draws)}</td>
    <td class="q">${escaped(row.why)}</td>
    <td class="e"><span class="pill">pair ${escaped(row.pair)}</span>
      <span class="pill">pagination.test.ts</span></td>
    <td class="r">${escaped(row.rule)}</td>
  </tr>`).join("\n");

  return `<section class="conform">
  <h2><span class="n">§6</span><span class="t">Pagination conformance</span></h2>

  <p class="note">One row per screen that draws a list. <b>A screen with a list
  and no row is a finding</b> — and this table is not kept by hand:
  <code>tests/pagination.test.ts</code> walks <code>screens/</code>, and fails
  when a list-bearing screen is unclassified, when a row outlives its screen,
  and when the set of screens calling <code>pager()</code> is not exactly the
  set marked numbered. Verified by mutation in all four directions.</p>

  <table>
    <thead><tr>
      <th>Screen</th><th>What it draws</th><th>Why</th>
      <th>Evidence</th><th>Rule</th>
    </tr></thead>
    <tbody>${rows}</tbody>
  </table>
</section>`;
}

/** The widget conformance section, and the finding that covers all five. */
export function widgets(captures: readonly { entry: string; html: string }[],
  tokenCss: string, realmCss: string): string {
  const drawn = new Map(captures.map((one) => [one.entry, one.html]));

  const cards = WIDGETS.map((widget) => {
    const html = drawn.get(widget.entry) ?? "";

    // **No padding, and the frame is exactly the canvas.**
    //
    // A widget sets `height:100vh` and clips its own content, which is page
    // 56's rule — the shell gives it a guaranteed size and the widget does the
    // cutting. Padding the capture's body put a 420px-tall widget inside a
    // 420px frame plus 28px of padding, so the *capture* overflowed and drew a
    // scrollbar the product never has. The widget was never wrong; the frame
    // around it was.
    const document_ = `<!doctype html><html><head><meta charset="utf-8"><style>${tokenCss}
      ${realmCss}</style></head><body>${html}</body></html>`;

    return `<figure class="widget">
      <div class="canvas"><iframe loading="lazy" srcdoc="${escaped(document_)}"></iframe></div>
      <figcaption>
        <b>${escaped(widget.name)}</b>
        <p class="ask">${escaped(widget.answers)}</p>
        <dl>
          <dt>Proposed frame</dt><dd>frame ${escaped(widget.frame)} of
            <code>03-guestops-widgets.html</code> <span class="none">not yet approved</span></dd>
          <dt>Stacks</dt><dd>${escaped(widget.stacks)}</dd>
          <dt>Taps through to</dt><dd>${escaped(widget.target)}</dd>
          <dt>Carrying</dt><dd><code>${escaped(widget.filter)}</code></dd>
        </dl>
      </figcaption>
    </figure>`;
  }).join("\n");

  return `<section class="conform">
  <h2><span class="n">5</span><span class="t">Widget conformance</span></h2>

  <div class="finding">
    <p class="lead">${escaped(WIDGET_CANVAS.title)}</p>
    <p>${rich(WIDGET_CANVAS.body)}</p>
    <p>${rich(WIDGET_CANVAS.consequence)}</p>
  </div>

  <div class="finding">
    <p class="lead">${escaped(WIDGET_FINDING.title)}</p>
    <p>${rich(WIDGET_FINDING.body)}</p>
    <p>${rich(WIDGET_FINDING.consequence)}</p>
  </div>

  <div class="widgets">${cards}</div>
</section>`;
}

/** The two sections' own chrome. */
export const TABLES = `
.conform{max-width:1180px;margin:0 auto 44px}
.conform .note{max-width:76ch;color:var(--muted);font-size:13px;margin:0 0 16px}
table{width:100%;border-collapse:collapse;font-size:12.5px}
thead th{text-align:left;padding:0 12px 9px;font-size:10.5px;letter-spacing:.09em;
  text-transform:uppercase;color:var(--faint);font-weight:400;
  border-bottom:1px solid var(--edge)}
tbody th,tbody td{padding:12px;vertical-align:top;border-bottom:1px solid var(--edge)}
tbody th{text-align:left;font-weight:600;color:var(--ink);white-space:nowrap}
td.q{color:var(--muted);max-width:38ch}
td.r{color:var(--faint);max-width:24ch}
td.e{white-space:nowrap}
.pill{display:inline-block;margin:0 4px 4px 0;padding:2px 8px;border-radius:6px;
  font-size:11px;background:rgb(129 140 248 / .13);color:var(--accent)}

.finding{max-width:76ch;margin:0 0 22px;padding:16px 20px;border-radius:12px;
  border:1px solid rgb(248 113 113 / .3);background:rgb(248 113 113 / .05)}
.finding .lead{margin:0 0 10px;font:500 15px/1.4 var(--serif);color:var(--bad)}
.finding p{margin:0 0 10px;color:var(--muted);font-size:13px}
.finding p:last-child{margin:0}

.widgets{display:grid;grid-template-columns:repeat(auto-fill,minmax(320px,1fr));gap:18px}
.widget{margin:0;border:1px solid var(--edge);border-radius:12px;overflow:hidden;
  background:var(--raised)}
/* The canvas, at its guaranteed size — 320 x 384, page 56. */
.widget .canvas{padding:18px 0;background:#0b0d14}
.widget iframe{display:block;width:320px;height:384px;border:0;background:#0b0d14;
  margin:0 auto}
.widget figcaption{padding:14px 16px;border-top:1px solid var(--edge)}
.widget figcaption b{font-size:13.5px}
.widget .ask{margin:5px 0 12px;color:var(--muted);font-size:12.5px;font-style:italic}
.widget dl{display:grid;grid-template-columns:auto 1fr;gap:5px 12px;margin:0;
  font-size:12px}
.widget dt{color:var(--faint)}
.widget dd{margin:0;color:var(--ink)}
.widget dd.none{color:var(--bad)}
.widget code{font-size:11.5px;color:#a8b0c4;word-break:break-word}
`;
