/**
 * The window: app bar, body, buttons — the shape every screen shares.
 */

/** The window: app bar, body — the shape every screen shares. */
export const SHELL = `
/* A COLUMN, not a row — docs/working/64 §3. An installed application
   navigates from the top; the platform's own four keep their left rail,
   because they are the desktop's own furniture and a guest application
   drawing its own 212px rail competes with the shell's chrome for the same
   edge of the same screen. */
/* 13.5/1.55 — the frame's own computed values, adjudicated 2026-09-05. The
   drawing's window sets no size; it inherits font:13.5px/1.55 from the mockup
   page. Taking the leading from that declaration and refusing the size would be
   incoherent, so both are the frame's. The font FAMILY in the same shorthand
   stays overridden: a module is typed by --font-sans, never by a stack a
   drawing happened to name. */
.go{display:flex;flex-direction:column;height:100vh;font-size:13.5px;line-height:1.55;
  color:var(--color-ink,#e8ebf4);
  font-family:var(--font-sans,system-ui,sans-serif);background:var(--color-surface,#0b0d14)}
.head{display:flex;align-items:center;gap:22px;padding:0 22px;height:56px;flex:none;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.app{display:flex;gap:10px;align-items:center;font-weight:600;margin-right:14px}
.mark{width:22px;height:22px;border-radius:6px;display:grid;place-items:center;font-size:11px;
  font-weight:700;color:var(--color-ink-on-accent,#0b0d14);background:var(--go-accent)}
/* Scoped to the bar, because \`.tab\` already means the body's view switcher.
   Two meanings, one class, kept apart by specificity rather than by a rename
   that would say the same word twice. */
.head .tab{display:flex;align-items:center;gap:7px;padding:19px 2px;font-size:13px;
  font-weight:400;color:var(--color-ink-muted,#8b93a7);border-bottom:2px solid transparent;
  border-left:0;border-right:0;border-top:0;background:none;font-family:inherit;
  line-height:inherit;cursor:pointer}
.head .tab:hover{color:var(--color-ink,#e8ebf4)}
.head .tab.on{color:var(--color-ink,#e8ebf4);border-bottom-color:var(--color-brand,#818cf8)}
.head .tab .n{margin-left:0;font-size:11px;color:var(--color-ink-faint,#5a6172)}
.head .tab .n.att{color:var(--color-warn,#fbbf24)}
.who{margin-left:auto;color:var(--color-ink-faint,#5a6172);font-size:12px;white-space:nowrap}
.main{flex:1;min-width:0;display:flex;flex-direction:column}
/* The page's own title row, for a screen naming a RECORD. A screen does not
   print its section name — the bar already says it (§3). */
.title{display:flex;align-items:center;gap:12px;padding:22px 26px 14px}
.ht{font-size:19px;font-weight:600;letter-spacing:-.01em}
.hsub{color:var(--color-ink-faint,#5a6172);font-size:12px;margin-top:2px;
  display:flex;align-items:center;gap:7px;flex-wrap:wrap}
.hsub b{color:var(--color-ink-muted,#8b93a7);font-weight:500}
.grow{margin-left:auto;display:flex;gap:8px}
/* Padded on all four sides. It was 0 on top because the page title supplied
   it; with the title gone from section screens, the strip would otherwise sit
   flush against the bar's rule (§3). */
.body{padding:22px 26px;overflow:auto;display:flex;flex-direction:column;gap:14px}
/* A flex child shrinks by default, so once the column overflows every card
   is compressed and its own overflow:hidden clips the controls off the
   bottom — the buttons vanish and the card still looks deliberate. */
.body>*{flex:none}
/* ONE control, modified — §2. This was four base classes at three geometries
   (.btn2 .create .danger .mini); a second base class is a second geometry
   within a week. Geometry is Jobs', colour is the shell's. */
/* line-height:inherit is the FOURTH UA reset, and the one docs/working/64 §2
   lists three of. A button carries the UA's border, centred text and its own
   font family — and the UA's font shorthand also resets line-height to normal,
   so a control that inherited family and size still sat 2.5px shorter than the
   same control drawn as a div in the gold. Measured, not eyeballed. */
.btn{font-family:inherit;line-height:inherit;cursor:pointer;white-space:nowrap;
  display:inline-flex;gap:8px;
  align-items:center;border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));
  border-radius:8px;padding:7px 14px;font-size:13px;color:var(--color-ink,#e8ebf4);
  background:none}
.btn.pri{border-color:transparent;font-weight:600;
  color:var(--color-ink-on-accent,#0b0d14);background:var(--go-accent)}
.btn.off{color:var(--color-ink-faint,#5a6172);border-style:dashed}
.btn.danger{border-color:var(--go-bad-edge);color:var(--color-bad,#f87171)}
/* The confirm step of a destructive flow is FILLED — docs/working/64 §2,
   amended 2026-09-04. An outline danger button sitting where a person has
   already decided to delete something is quieter than the Cancel beside it,
   which inverts the weight of the choice. Outline stays for the inline
   affordance that STARTS the flow; this is the one that performs it. */
.btn.danger.confirm{background:var(--color-bad,#f87171);border-color:transparent;
  color:var(--color-ink-on-accent,#0b0d14);font-weight:600}
.btn.sm{padding:2px 8px;font-size:11px;gap:6px}
.btn.sm.pri{background:var(--go-brand-wash);border-color:var(--go-brand-edge);
  color:var(--color-ink,#e8ebf4);font-weight:500}
/* One thin bar, not four cards — §3/§5. Four stat cards cost ~68px at the top
   of every list screen, which is two guests' worth of rows, and repeated the
   counts the tabs immediately below already carry. */
.strip{display:flex;gap:24px;align-items:center;flex-wrap:wrap;font-size:12px;
  color:var(--color-ink-muted,#8b93a7);padding:8px 12px;border-radius:8px;
  border:1px solid var(--color-line,rgba(255,255,255,.08))}
.strip b{color:var(--color-ink,#e8ebf4);font-size:14px;font-weight:600;margin-right:5px}
.strip .on{color:var(--color-ink,#e8ebf4)}
.strip .on b{color:var(--color-brand,#818cf8)}
/* The day's context, pushed right — Jobs' board carries its date here. */
.strip .ctx{margin-left:auto;color:var(--color-ink-faint,#5a6172)}
.strip .ctx b{font-size:12px;color:var(--color-ink-muted,#8b93a7);margin-right:0}
.tabs{display:flex;gap:4px;align-items:center;flex-wrap:wrap;margin-top:2px;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.tab{padding:8px 14px;font-size:12.5px;color:var(--color-ink-faint,#5a6172);
  border:0;border-bottom:2px solid transparent;background:none;font-family:inherit;
  line-height:inherit;cursor:pointer}
.tab.on{color:var(--color-ink,#e8ebf4);font-weight:600;
  border-bottom-color:var(--color-brand,#818cf8)}
.tab .n{color:var(--color-ink-faint,#5a6172);font-weight:400;margin-left:5px;font-size:11px}
/* The actions sharing the view-switcher row sit clear of its rule. Present in
   the gold and missing here — found by measuring the row's height, not by
   looking at it. */
.tabs .btn{margin-bottom:6px}
.stand{display:flex;align-items:center;gap:8px;padding:8px 12px;border-radius:9px;font-size:11.5px;
  border:1px dashed var(--color-line-strong,rgba(255,255,255,.16));
  color:var(--color-ink-faint,#5a6172)}
`;
