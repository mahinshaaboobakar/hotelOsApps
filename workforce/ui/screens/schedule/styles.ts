/** The Staff Schedule's own rules — the four figures and the month grid. */
export const SCHEDULE_CSS = `
.meta{display:flex;gap:22px;align-items:baseline;flex-wrap:wrap;
      padding:9px 14px;border-radius:10px;font-size:12.5px;
      color:var(--color-ink-muted,#8b93a7);
      background:var(--color-surface-raised,#11141f);
      border:1px solid var(--color-line,rgb(255 255 255/.07))}
.mfig,.mpush{display:flex;gap:7px;align-items:baseline}
.mpush{margin-left:auto}
.meta i{font-style:normal;font-size:15px;font-weight:600;color:var(--color-ink,#e8ebf4);
        letter-spacing:-.01em}

.cal{display:grid;grid-template-columns:repeat(7,1fr);gap:6px}
.cday{min-height:78px;border-radius:10px;padding:6px 7px;position:relative;
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
.cday.today{border-color:var(--color-brand,#818cf8)}
.cduty{margin-top:4px;font-size:9.5px;font-weight:600;letter-spacing:.01em;
       border-radius:5px;padding:2px 5px;width:fit-content;
       background:color-mix(in srgb, var(--color-warn) 15%, transparent);
       color:var(--color-warn,#fbbf24)}
.cduty.tail{opacity:.55}
`;
