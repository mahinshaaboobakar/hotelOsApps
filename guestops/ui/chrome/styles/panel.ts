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
/* A note about a refusal rather than about a rule — frame 7 s folio. */
.note.no{border-color:var(--go-bad-edge);background:var(--go-bad-wash)}
.note.no b{color:var(--color-bad,#f87171)}
.hint{font-size:12px;line-height:1.65;color:var(--color-ink-faint,#5a6172)}
.hint b{color:var(--color-ink-muted,#8b93a7)}
/* A hint pushed to the end of a control row — frame 4 states the list order
   and whose clock the times are on, beside the source filters. */
.hint.grow{margin-left:auto;display:block}
.acts{display:flex;gap:9px}
/* Two equal columns, where neither side is the subordinate one — frames 5
   and 7 put a panel we own beside a panel we only report. */
.cols.even{grid-template-columns:1fr 1fr}
/* A column of rows inside a card body that is itself two columns. */
.stack{display:flex;flex-direction:column;gap:11px}
/* Three equal cards — frame 9's facts about a group. Not the two-column
   grid, because these are peers with no principal among them: that grid is
   1.55fr/1fr and says one side is the subject. */
.grid3{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;align-items:start}
/* A card whose subject came from somewhere else — frame 15's foreign-national
   block. It shares the info banner's tone for the same reason: it is an
   attribution, not a warning. */
.card.info{border-color:var(--go-brand-edge);
  background:color-mix(in srgb, var(--color-brand,#818cf8) 5%, transparent)}
.ch.info{color:var(--color-brand,#818cf8)}
.ch.info .grow{color:var(--color-ink-faint,#5a6172)}
/* A panel whose subject is not here: dashed, and washed back. */
.card.ghost{border-style:dashed;
  background:color-mix(in srgb, var(--color-ink,#e8ebf4) 1.5%, transparent)}
.ch.no{color:var(--color-bad,#f87171)}
.fr.big .v b{font-size:15px}
/* A tab whose subject needs an application this property has not installed.
   Dimmed, never removed: which tabs a stay HAS is itself information. */
.tab.gone{opacity:.38}
/* Something read from elsewhere, stated as such — frame 6's band. Brand-toned
   rather than warn: it is not a problem, it is an attribution. */
.ban.info{border-color:var(--go-brand-edge);background:var(--go-brand-wash);
  color:var(--color-ink,#e8ebf4)}
/* The invitation shown where a neighbour would be — ADR 0106 §4. */
.empty{margin:auto;max-width:480px;text-align:center;padding:26px 20px;
  display:flex;flex-direction:column;align-items:center;gap:10px}
.empty .ic{width:52px;height:52px;display:grid;place-items:center;font-size:20px;
  border-radius:16px;color:var(--color-ink-faint,#5a6172);
  border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));
  background:color-mix(in srgb, var(--color-ink,#e8ebf4) 3%, transparent)}
.empty b{font-size:13.5px}
.empty p{margin:0;font-size:12.5px;line-height:1.7;color:var(--color-ink-faint,#5a6172)}
.empty p b{color:var(--color-ink-muted,#8b93a7)}
/* The second paragraph, which is reassurance rather than fact. */
.empty p.quiet{color:var(--color-ink-faint,#5a6172);opacity:.85}
/* Frame 13 fills the window: the book is being built and there is nothing
   else to look at. */
.main>.empty{margin:auto;padding:60px 20px;gap:14px}
.main>.empty b{font-size:16px}
/* One cell per night — frame 6. \`grid-auto-columns\`, not the drawing's
   \`repeat(5,1fr)\`: five is that stay's night count plus its arrival, not a
   constant, and a two-night stay laid out in five columns would leave three
   empty ones. */
.nights{display:grid;grid-auto-flow:column;grid-auto-columns:1fr;overflow:hidden;
  border:1px solid var(--color-line,rgba(255,255,255,.08));
  border-radius:var(--radius-panel,14px);
  background:var(--color-surface-raised,#11141f)}
.ng{padding:11px 12px;min-height:104px;display:flex;flex-direction:column;gap:7px;
  border-right:1px solid var(--color-line,rgba(255,255,255,.08))}
.ng:last-child{border-right:none}
.ng .dt{font-size:11px;text-transform:uppercase;letter-spacing:.06em;
  color:var(--color-ink-faint,#5a6172)}
.ng .dt b{color:var(--color-ink-muted,#8b93a7);font-weight:600}
.ng .st{font-size:12px;display:flex;flex-direction:column;gap:5px;align-items:flex-start}
.ng.now{box-shadow:inset 0 0 0 1.5px var(--go-brand-edge)}
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
