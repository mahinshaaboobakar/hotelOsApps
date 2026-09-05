/**
 * The marks, the locks, the pills and the inline links.
 */

/** The marks, the locks, the pills and the inline links. */
export const MARKS = `
.sh{display:inline-flex;gap:6px;align-items:center;padding:3px 9px;border-radius:7px;
  font-size:11.5px;font-weight:500;white-space:nowrap;border:1px solid transparent}
.sh i{width:6px;height:6px;border-radius:50%;display:inline-block}
.sh.pms{background:var(--go-pms-wash);color:var(--go-pms)}
.sh.pms i{background:var(--go-pms)}
.sh.override{background:var(--go-override-wash);color:var(--go-override)}
.sh.override i{background:var(--go-override)}
.sh.disagrees{background:var(--go-warn-wash);color:var(--color-warn,#fbbf24)}
.sh.disagrees i{background:var(--color-warn,#fbbf24)}
.sh.other,.sh.dayuse{background:var(--go-ok-wash);color:var(--color-ok,#34d399)}
/* A stay this desk created — frame 2. It shares the ok tone with \`other\` in
   the drawing and is its own word here, because the two say different things
   and a later theme may want to separate them. */
.sh.walkin{background:var(--go-ok-wash);color:var(--color-ok,#34d399)}
.sh.other i{background:var(--color-ok,#34d399)}
.sh.unknown{background:var(--go-bad-wash);color:var(--color-bad,#f87171);
  border-color:var(--go-bad-edge)}
.sh.missing{color:var(--color-ink-faint,#5a6172);border-style:dashed;
  border-color:var(--color-line,rgba(255,255,255,.08))}
.lock{font-size:10px;letter-spacing:.04em;padding:1px 5px;border-radius:5px;
  color:var(--color-ink-faint,#5a6172);
  border:1px solid var(--color-line,rgba(255,255,255,.08))}
.pill{padding:3px 11px;border-radius:99px;font-size:11px;font-weight:600;white-space:nowrap}
.pill.neutral{background:var(--go-ink-wash);color:var(--color-ink-muted,#8b93a7)}
.pill.ok{background:var(--go-ok-wash);color:var(--color-ok,#34d399)}
.pill.warn{background:var(--go-warn-wash);color:var(--color-warn,#fbbf24)}
/* Cancelled, and no-show — frame 2. A state that is over, not a state that is
   wrong: the row stays in the list either way. */
.pill.bad{background:var(--go-bad-wash);color:var(--color-bad,#f87171)}
.link{color:var(--color-brand,#818cf8);background:none;border:0;padding:0;font:inherit;
  cursor:pointer}
.un{color:var(--color-ink-faint,#5a6172);font-style:italic}
`;
