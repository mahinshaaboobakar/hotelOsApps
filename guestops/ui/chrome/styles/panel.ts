/**
 * Cards, their header bands, label-value rows, and the activity timeline.
 */

/** Cards, their header bands, label–value rows, and the activity timeline. */
export const PANEL = `
.cols{display:grid;grid-template-columns:1.55fr 1fr;gap:14px;align-items:start}
.card{border:1px solid var(--color-line,rgba(255,255,255,.08));overflow:hidden;
  border-radius:var(--radius-panel,14px);background:var(--color-surface-raised,#11141f)}
.ch{padding:11px 15px;font-size:11px;text-transform:uppercase;letter-spacing:.08em;
  display:flex;align-items:center;gap:8px;color:var(--color-ink-faint,#5a6172);
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.ch .grow{margin-left:auto;text-transform:none;letter-spacing:0;font-size:11.5px}
.cb{padding:13px 15px;display:flex;flex-direction:column;gap:11px}
.fr{display:flex;align-items:flex-start;gap:10px;font-size:12.5px}
.fr .k{color:var(--color-ink-faint,#5a6172);width:118px;flex:none;font-size:12px}
.fr .v{color:var(--color-ink,#e8ebf4);display:flex;align-items:center;gap:7px;flex-wrap:wrap}
.ban{display:flex;gap:11px;align-items:center;border-radius:12px;padding:10px 14px;font-size:12.5px;
  border:1px solid var(--go-warn-edge);background:var(--go-warn-wash)}
.ban b{color:var(--color-ink,#e8ebf4)}
.ban .why{color:var(--color-ink-faint,#5a6172);display:block;margin-top:3px}
.ban .why b{color:var(--color-ink-muted,#8b93a7)}
/* What will NOT happen — frame 8's "Opera will not be told". Bad-toned rather
   than warn: it is not a condition to watch, it is a limit of what the button
   about to be pressed does, and the two must not read the same. */
.ban.gone{border-color:var(--go-bad-edge);background:var(--go-bad-wash);
  color:var(--color-bad,#f87171)}
.note{border:1px dashed var(--color-line-strong,rgba(255,255,255,.16));border-radius:12px;
  padding:10px 12px;font-size:12px;line-height:1.65;color:var(--color-ink-muted,#8b93a7)}
.note b{color:var(--color-ink,#e8ebf4)}
.hint{font-size:12px;line-height:1.65;color:var(--color-ink-faint,#5a6172)}
.hint b{color:var(--color-ink-muted,#8b93a7)}
.acts{display:flex;gap:9px}
.tl{display:flex;flex-direction:column}
.te{display:grid;grid-template-columns:58px 16px 1fr;font-size:12.5px}
.te .t{color:var(--color-ink-faint,#5a6172);padding:7px 0;font-size:11.5px}
.te .g{display:flex;flex-direction:column;align-items:center}
.te .g i{width:7px;height:7px;border-radius:50%;margin-top:12px;
  background:var(--color-ink-faint,#5a6172)}
.te .g u{flex:1;width:1px;background:var(--color-line-strong,rgba(255,255,255,.16))}
.te.pms .g i{background:var(--go-pms)}
.te.override .g i{background:var(--go-override)}
.te.disagrees .g i{background:var(--color-warn,#fbbf24)}
.te .d{padding:7px 0 7px 12px;display:flex;flex-direction:column;gap:3px}
.te .d span{color:var(--color-ink-faint,#5a6172);font-size:11.5px}
`;
