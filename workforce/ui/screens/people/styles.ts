/** People's own rules — the department chips on a posting row. */
export const PEOPLE_CSS = `
/* The consequence panel. The consequence, before the button. */
.conseq{border:1px solid color-mix(in srgb, var(--color-bad) 35%, transparent);
        background:color-mix(in srgb, var(--color-bad) 6%, transparent);
        border-radius:12px;padding:12px 14px;display:flex;flex-direction:column}
.conseq em{font-style:normal;font-size:10.5px;font-weight:700;letter-spacing:.08em;
           text-transform:uppercase;color:var(--color-bad,#f87171);margin-bottom:8px}
.conseq .cr{display:flex;gap:8px;align-items:center;font-size:12.5px;padding:4px 0}
.conseq .note{margin-top:8px}
.conseq .quiet{color:var(--color-ink-faint,#5a6172)}
.deps{display:flex;gap:4px;flex-wrap:wrap}

.first{display:flex;flex-direction:column;align-items:center;gap:10px;
       text-align:center;padding:44px 24px;max-width:520px;margin:0 auto}
.fmark{width:52px;height:52px;border-radius:14px;display:grid;place-items:center;
       font-size:22px;color:var(--color-brand,#818cf8);
       background:var(--color-surface-raised,#11141f);
       border:1px solid var(--color-line-strong,rgb(255 255 255/.14))}
.ft{font-size:16px;font-weight:600}
.first .note{text-align:center}
.first .btn{margin-top:6px}
`;
