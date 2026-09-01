/** The Staff Schedule's own rules — the four figures and the month grid. */
export const SCHEDULE_CSS = `
.figs{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}
.fig{background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line,rgb(255 255 255/.07));
     border-radius:var(--radius-panel,1rem);padding:12px 14px}
.fig b{font-size:20px;font-weight:600;letter-spacing:-.02em}
.fig div{font-size:12px;color:var(--color-ink-muted,#8b93a7);margin-top:2px}

.cal{display:grid;grid-template-columns:repeat(7,1fr);gap:6px}
.cday{min-height:62px;border-radius:10px;padding:6px 7px;position:relative;
      background:var(--color-surface-raised,#11141f);
      border:1px solid var(--color-line,rgb(255 255 255/.07))}
/* A day from an adjacent month is present and quiet — removing it would shift
   every column and make the weekday headings lie. */
.cday.out{background:transparent;border-style:dashed;opacity:.45}
.cday s{text-decoration:none;font-size:11px;color:var(--color-ink-faint,#5a6172)}
.cm{display:block;margin-top:6px;font-size:12px;font-weight:600;
    border-radius:6px;padding:2px 6px;width:fit-content}
.cm.brand{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
          color:var(--color-brand,#818cf8)}
.cm.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent);
       color:var(--color-ok,#34d399)}
.cm.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent);
         color:var(--color-warn,#fbbf24)}
.cm.neutral{color:var(--color-ink-faint,#5a6172)}
.cm.leave{background:color-mix(in srgb, var(--color-bad) 13%, transparent);
          color:var(--color-bad,#f87171)}
.cduty{position:absolute;top:6px;right:7px;font-style:normal;font-size:11px;
       color:var(--color-warn,#fbbf24)}
.ctail{position:absolute;top:6px;right:7px;font-style:normal;font-size:10px;
       color:var(--color-ink-faint,#5a6172)}
`;
