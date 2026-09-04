/** The Duty Register's own rules — now/next, and the two-band week. */
export const DUTY_CSS = `
.nn{display:grid;grid-template-columns:1fr 1fr;gap:10px}
/* The two ends of a duty span. One instant per box, on one line. */
.ends{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.ends .finput{white-space:nowrap}
.nl{display:flex;gap:12px;align-items:center;padding:12px 14px;
    background:var(--color-surface-raised,#11141f);
    border:1px solid var(--color-line,rgb(255 255 255/.07));
    border-radius:var(--radius-panel,1rem)}
.nl.on{border-color:var(--color-warn,#fbbf24)}
.nl em{font-style:normal;font-size:10.5px;font-weight:700;letter-spacing:.09em;
       color:var(--color-ink-faint,#5a6172)}
.nl.on em{color:var(--color-warn,#fbbf24)}
.nl b{font-size:14px;font-weight:600}
.nl s{text-decoration:none;display:block;font-size:11.5px;
      color:var(--color-ink-faint,#5a6172);margin-top:2px}

.dgrid{display:grid;grid-template-columns:110px repeat(7,1fr);gap:6px}
.dstack{display:flex;flex-direction:column;gap:4px}
.dband{border-radius:8px;padding:5px 8px;font-size:11px;
       background:color-mix(in srgb, var(--color-warn) 13%, transparent);
       color:var(--color-warn,#fbbf24)}
/* Night duties read one step quieter than day duties, so the two bands are
   distinguishable without colour — which is what the printed sheet needs. */
.dband.night{background:color-mix(in srgb, var(--color-warn) 7%, transparent)}
.dband.none{background:transparent;color:var(--color-ink-faint,#5a6172);
            border:1px dashed var(--color-line-strong,rgb(255 255 255/.14))}
.dband b{display:block;font-weight:600}
.dband s{text-decoration:none;font-size:10px;opacity:.8}
`;
