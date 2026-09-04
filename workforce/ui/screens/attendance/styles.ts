/** Attendance's own rules — the four marks, and the source tag on a row. */
export const ATTENDANCE_CSS = `
/* ONE THIN BAR, not four cards — the app surface standard §3, Jobs' board.
   Four stat cards cost about 68px at the top of the screen, which is two rows'
   worth of attendance, and they repeat figures the rows below already carry.
   Same facts, one line. Jobs and GuestOps both draw this; Workforce was the
   last application still spending a card on a number. */
.marks{display:flex;gap:24px;align-items:center;flex-wrap:wrap;font-size:12px;
       color:var(--color-ink-muted,#8b93a7);padding:8px 12px;border-radius:8px;
       border:1px solid var(--color-line,rgb(255 255 255/.07))}
.mk{display:flex;gap:6px;align-items:baseline;white-space:nowrap}
.mk b{font-size:14px;font-weight:600;color:var(--color-ink,#e8ebf4)}
.mk div{font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.mk.ok b{color:var(--color-ok,#34d399)}
.mk.warn b{color:var(--color-warn,#fbbf24)}
.mk.bad b{color:var(--color-bad,#f87171)}
.ag{display:flex;gap:8px;align-items:center}
.ag .src{text-decoration:none;font-size:10.5px;letter-spacing:.04em;
         text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin:0}
.postedcell{display:flex;gap:8px;align-items:center}
`;