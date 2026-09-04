/** Policy's own rules — sections, the short-code cell, the threshold fields. */
export const POLICY_CSS = `
.sect{display:flex;flex-direction:column;gap:8px;padding-bottom:6px}
.stitle{display:flex;gap:10px;align-items:center;font-size:13px;font-weight:600;
        color:var(--color-ink,#e8ebf4);padding-top:6px}
.otrow{display:flex;gap:10px;align-items:center;flex-wrap:wrap;font-size:12.5px}
.field{border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
       border-radius:8px;padding:5px 11px;font-size:12.5px;font-weight:600;
       background:var(--color-surface-raised,#11141f)}
.spans{display:grid;grid-template-columns:repeat(4,1fr);gap:6px}
.choices{display:flex;gap:6px}
.choice{border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
        border-radius:8px;padding:6px 14px;font-size:12.5px;cursor:pointer;
        color:var(--color-ink-muted,#8b93a7)}
.choice.on{border-color:var(--color-brand,#818cf8);color:var(--color-brand,#818cf8);
           background:color-mix(in srgb, var(--color-brand) 13%, transparent)}
.swatches{display:flex;gap:8px}
.sw{width:26px;height:26px;border-radius:8px;cursor:pointer;
    border:1px solid var(--color-line-strong,rgb(255 255 255/.14))}
.sw.on{outline:2px solid var(--color-ink,#e8ebf4);outline-offset:2px}
.sw.brand{background:var(--color-brand,#818cf8)}
.sw.ok{background:var(--color-ok,#34d399)}
.sw.warn{background:var(--color-warn,#fbbf24)}
.sw.bad{background:var(--color-bad,#f87171)}
.sw.neutral{background:var(--color-ink-faint,#5a6172)}
`;
