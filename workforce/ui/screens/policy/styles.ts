/** Policy's own rules — sections, the short-code cell, the threshold fields. */
export const POLICY_CSS = `
.sect{display:flex;flex-direction:column;gap:8px;padding-bottom:6px}
.stitle{display:flex;gap:10px;align-items:center;font-size:13px;font-weight:600;
        color:var(--color-ink,#e8ebf4);padding-top:6px}
.otrow{display:flex;gap:10px;align-items:center;flex-wrap:wrap;font-size:12.5px}
.field{border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
       border-radius:8px;padding:5px 11px;font-size:12.5px;font-weight:600;
       background:var(--color-surface-raised,#11141f)}

.scrim{position:absolute;inset:0;display:grid;place-items:center;padding:24px;
      background:color-mix(in srgb, var(--color-surface) 72%, transparent)}
.dlg{width:min(560px,100%);max-height:100%;overflow:auto;display:flex;
     flex-direction:column;gap:14px;padding:20px;
     background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
     border-radius:var(--radius-panel,1rem)}
.fld{display:flex;flex-direction:column;gap:5px}
.flab{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
      color:var(--color-ink-faint,#5a6172)}
.finput{border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
        border-radius:8px;padding:7px 11px;font-size:13px;
        background:var(--color-surface,#0b0d14)}
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
