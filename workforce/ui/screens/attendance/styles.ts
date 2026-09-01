/** Attendance's own rules — the four marks, and the source tag on a row. */
export const ATTENDANCE_CSS = `
.marks{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}
.mk{background:var(--color-surface-raised,#11141f);
    border:1px solid var(--color-line,rgb(255 255 255/.07));
    border-radius:var(--radius-panel,1rem);padding:12px 14px}
.mk b{font-size:20px;font-weight:600;letter-spacing:-.02em}
.mk div{font-size:12px;color:var(--color-ink-muted,#8b93a7);margin-top:2px}
.mk.ok b{color:var(--color-ok,#34d399)}
.mk.warn b{color:var(--color-warn,#fbbf24)}
.mk.bad b{color:var(--color-bad,#f87171)}
.ag{display:flex;gap:8px;align-items:center}
.ag .src{text-decoration:none;font-size:10.5px;letter-spacing:.04em;
         text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin:0}
.dim{color:var(--color-ink-muted,#8b93a7)}
`;
