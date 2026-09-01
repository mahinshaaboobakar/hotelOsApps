/** People's own rules — the department chips on a posting row. */
export const PEOPLE_CSS = `
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
