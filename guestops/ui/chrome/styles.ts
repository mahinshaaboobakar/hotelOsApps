/**
 * The module's stylesheet — the approved design, on the published contract.
 *
 * # Two rules, and the second is the one that was broken
 *
 * 1. **Consume only published token names.** `@hotelos/sdk`'s `TOKENS` is the
 *    contract; a `var()` on anything else silently takes its fallback, and the
 *    module is then styled by nobody — SHELL-Q33's class, in a module rather
 *    than in the realm.
 * 2. **Derive everything else from those names.** The first build hardcoded
 *    seventeen `rgba(…)` literals for the chip tints and borrowed four
 *    unpublished names (`--color-aurora-1`, `--color-aurora-3`,
 *    `--color-surface-sunken`, `--r-md`). Literals cannot follow a theme: in a
 *    light property every chip kept its dark-theme wash.
 *
 * All derivation is the `DERIVED` block below, so the rest of the stylesheet
 * reads as colour names and the contract is visible in one place. Those
 * `--go-*` variables are the **module's own** — it may define what it likes;
 * what it may not do is consume a name the host never promised.
 *
 * # The one place the contract is short, reported rather than worked around
 *
 * The design's marks need **two non-semantic accents** — `from Opera` (cyan in
 * the gold) and `override` (violet) — and the contract publishes exactly one
 * accent, `color-brand`. Rather than invent a token, `override` is mixed from
 * two published colours so it is a distinct hue that still follows the theme.
 * It is close to the drawn violet and it is not the drawn violet. A second
 * published accent would restore it exactly.
 */

/** The stylesheet element, built once and re-attached with each screen. */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [DERIVED, SHELL, TABLE, PANEL, MARKS].join("");
  return style;
}

/**
 * Everything the design needs that the contract does not publish, derived.
 *
 * `color-mix` on a published token follows the theme the host injected, which
 * is the whole point: a tint written as `rgba(52,211,153,.12)` is a dark-theme
 * decision frozen into a module that a light property will also run.
 */
const DERIVED = `
.go{
  --go-pms:var(--color-brand,#818cf8);
  --go-override:color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171));
  --go-pms-wash:color-mix(in srgb, var(--color-brand,#818cf8) 13%, transparent);
  --go-override-wash:color-mix(in srgb, var(--go-override) 15%, transparent);
  /* The three state tints are PUBLISHED now — 894e230, docs/working/64 §1.
     These were hand-mixed at 14%, 12% and 10%: three numbers nobody chose
     together, in an application that is one of three doing the same thing.
     Aliased rather than deleted, because ten selectors name them and the
     shell's tone is the value either way; the fallback is the old mix, so a
     host that has not published the tones yet still renders. */
  --go-warn-wash:var(--color-warn-soft, color-mix(in srgb, var(--color-warn,#fbbf24) 14%, transparent));
  --go-ok-wash:var(--color-ok-soft, color-mix(in srgb, var(--color-ok,#34d399) 12%, transparent));
  --go-bad-wash:var(--color-bad-soft, color-mix(in srgb, var(--color-bad,#f87171) 10%, transparent));
  --go-brand-wash:color-mix(in srgb, var(--color-brand,#818cf8) 18%, transparent);
  --go-brand-edge:color-mix(in srgb, var(--color-brand,#818cf8) 50%, transparent);
  --go-bad-edge:color-mix(in srgb, var(--color-bad,#f87171) 45%, transparent);
  --go-warn-edge:color-mix(in srgb, var(--color-warn,#fbbf24) 35%, transparent);
  --go-ink-wash:color-mix(in srgb, var(--color-ink,#e8ebf4) 6%, transparent);
  --go-row-hover:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent);
  /* 135deg, not 120 — docs/working/64 §2. Jobs and GuestOps had independently
     derived the same fill down to the 62% and differed only in the angle;
     Jobs' is the baseline, because the geometry is. */
  --go-accent:linear-gradient(135deg,var(--go-pms),var(--go-override));
}
`;

/** The window: app bar, body — the shape every screen shares. */
const SHELL = `
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

/** The day's table — bare on the page, and its pager. */
const TABLE = `
/* NO CARD — docs/working/64 §4. This was a filled, bordered, 14px-radius box
   with the header row filled again inside it. Jobs' board sits bare and
   separates rows with one rule, and two applications drawing a list two ways
   is more visible than two buttons, because a list is most of what an
   operator looks at. */
.tbl{font-size:13px}
.tr{display:grid;grid-template-columns:1.5fr .9fr .8fr .7fr .8fr 1.5fr;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
/* The last row keeps its rule: with no card around the list, that final line
   is what closes it. */
.tr>div{padding:6px 10px;display:flex;align-items:flex-start;gap:7px;min-width:0}
.tr.hd>div{align-items:center;padding:8px 10px;font-size:11px;font-weight:500;
  text-transform:uppercase;letter-spacing:.08em;color:var(--color-ink-faint,#5a6172)}
.tr.act{cursor:pointer}
.tr.act:hover{background:var(--go-row-hover)}
.tr .nm{display:flex;flex-direction:column;gap:0;align-items:flex-start;line-height:1.25}
.tr .nm b{font-weight:600}
.tr .nm span{font-size:10.5px;color:var(--color-ink-faint,#5a6172)}
/* The pager — §6, numbered because the wire now carries a total.
   \`ListStays\` pages on \`PagedRequest\`/\`PagedResponse\`, so an ordinal and a
   count both exist and "showing 1-25 of 47" is something the service can
   actually answer.
   It MATCHES components/design/pager.tsx rather than importing it: a hosted
   module is styled by tokens and never by importing components across a
   realm, so the match is a rendering obligation, not a dependency. */
.pager{display:flex;justify-content:space-between;align-items:center;gap:9px;
  padding:10px 4px 0;font-size:12px;color:var(--color-ink-faint,#5a6172)}
.pager .pnav{display:flex;align-items:center}
.pager .pg{display:inline-block;padding:2px 8px;margin-left:4px;border-radius:6px;
  border:1px solid var(--color-line,rgba(255,255,255,.08));font-family:inherit;
  font-size:12px;background:none;cursor:pointer;color:var(--color-ink-muted,#8b93a7)}
.pager .pg.on{color:var(--color-ink,#e8ebf4);border-color:var(--color-brand,#818cf8)}
.pager .pg[disabled]{color:var(--color-ink-faint,#5a6172);opacity:.45;cursor:default}
.pager .gap{margin-left:4px;color:var(--color-ink-faint,#5a6172)}
`;

/** Cards, their header bands, label–value rows, and the activity timeline. */
const PANEL = `
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

/** The marks, the locks, the pills and the inline links. */
const MARKS = `
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
.link{color:var(--color-brand,#818cf8);background:none;border:0;padding:0;font:inherit;
  cursor:pointer}
.un{color:var(--color-ink-faint,#5a6172);font-style:italic}
`;
