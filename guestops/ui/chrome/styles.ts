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
  --go-warn-wash:color-mix(in srgb, var(--color-warn,#fbbf24) 14%, transparent);
  --go-ok-wash:color-mix(in srgb, var(--color-ok,#34d399) 12%, transparent);
  --go-bad-wash:color-mix(in srgb, var(--color-bad,#f87171) 10%, transparent);
  --go-brand-wash:color-mix(in srgb, var(--color-brand,#818cf8) 18%, transparent);
  --go-brand-edge:color-mix(in srgb, var(--color-brand,#818cf8) 50%, transparent);
  --go-bad-edge:color-mix(in srgb, var(--color-bad,#f87171) 45%, transparent);
  --go-warn-edge:color-mix(in srgb, var(--color-warn,#fbbf24) 35%, transparent);
  --go-ink-wash:color-mix(in srgb, var(--color-ink,#e8ebf4) 6%, transparent);
  --go-row-hover:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent);
  --go-accent:linear-gradient(120deg,var(--go-pms),var(--go-override));
}
`;

/** The window: rail, header, body — the shape every frame shares. */
const SHELL = `
.go{display:flex;height:100vh;font-size:13px;color:var(--color-ink,#e8ebf4);
  font-family:var(--font-sans,system-ui,sans-serif);background:var(--color-surface,#0b0d14)}
.rail{width:212px;flex:none;padding:20px 12px;display:flex;flex-direction:column;gap:1px;
  border-right:1px solid var(--color-line,rgba(255,255,255,.08))}
.app{display:flex;gap:11px;align-items:center;font-weight:600;font-size:14.5px;padding:0 10px 18px}
.mark{width:26px;height:26px;border-radius:8px;display:grid;place-items:center;font-size:11px;
  font-weight:700;color:var(--color-ink-on-accent,#0b0d14);background:var(--go-accent);
  border:1px solid var(--color-line-strong,rgba(255,255,255,.16))}
.ri{display:flex;gap:11px;align-items:center;padding:8px 12px;border-radius:10px;
  color:var(--color-ink-muted,#8b93a7);font-size:13.5px;cursor:pointer;
  background:none;border:0;width:100%;text-align:left;font-family:inherit}
.ri:hover{color:var(--color-ink,#e8ebf4)}
.ri.on{color:var(--color-ink,#e8ebf4);font-weight:500;background:var(--go-brand-wash);
  box-shadow:inset 2.5px 0 0 var(--color-brand,#818cf8)}
.ri .cnt{margin-left:auto;font-size:11px;color:var(--color-ink-faint,#5a6172)}
.ri .cnt.att{color:var(--color-warn,#fbbf24)}
.me{margin-top:auto;padding:12px;font-size:12px;line-height:1.5;
  border-top:1px solid var(--color-line,rgba(255,255,255,.08));color:var(--color-ink-faint,#5a6172)}
.me b{display:block;color:var(--color-ink-muted,#8b93a7);font-weight:500}
.main{flex:1;min-width:0;display:flex;flex-direction:column}
.head{display:flex;align-items:center;gap:12px;padding:22px 26px 14px}
.ht{font-size:19px;font-weight:600;letter-spacing:-.01em}
.hsub{color:var(--color-ink-faint,#5a6172);font-size:12px;margin-top:2px;
  display:flex;align-items:center;gap:7px;flex-wrap:wrap}
.hsub b{color:var(--color-ink-muted,#8b93a7);font-weight:500}
.grow{margin-left:auto;display:flex;gap:8px}
.body{padding:0 26px 22px;overflow:auto;display:flex;flex-direction:column;gap:14px}
/* A flex child shrinks by default, so once the column overflows every card
   is compressed and its own overflow:hidden clips the controls off the
   bottom — the buttons vanish and the card still looks deliberate. */
.body>*{flex:none}
.btn2,.create,.danger,.mini{font-family:inherit;cursor:pointer;white-space:nowrap;
  display:inline-flex;gap:8px;align-items:center}
.btn2{border:1px solid var(--color-line-strong,rgba(255,255,255,.16));border-radius:11px;
  padding:8px 15px;font-size:12.5px;color:var(--color-ink-muted,#8b93a7);background:none}
.create{border:0;border-radius:11px;padding:8px 18px;font-size:13px;font-weight:600;
  color:var(--color-ink-on-accent,#0b0d14);background:var(--go-accent)}
.danger{border:1px solid var(--go-bad-edge);border-radius:11px;padding:8px 15px;
  font-size:12.5px;color:var(--color-bad,#f87171);background:none}
.mini{border:1px solid var(--color-line-strong,rgba(255,255,255,.16));border-radius:9px;
  padding:6px 12px;font-size:12px;color:var(--color-ink,#e8ebf4);background:none}
.mini.pri{background:var(--go-brand-wash);border-color:var(--go-brand-edge)}
.strip{display:flex;gap:10px}
.stat{flex:1;padding:11px 14px;border-radius:var(--radius-panel,14px);
  border:1px solid var(--color-line,rgba(255,255,255,.08));
  background:var(--color-surface-raised,#11141f)}
.stat b{display:block;font-size:15px;font-weight:600}
.stat span{color:var(--color-ink-faint,#5a6172);font-size:11.5px}
.stat.on{box-shadow:inset 0 0 0 1.5px var(--go-brand-edge)}
.tabs{display:flex;gap:4px;flex-wrap:wrap;margin-top:2px;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.tab{padding:8px 14px;font-size:12.5px;color:var(--color-ink-faint,#5a6172);
  border:0;border-bottom:2px solid transparent;background:none;font-family:inherit;cursor:pointer}
.tab.on{color:var(--color-ink,#e8ebf4);font-weight:600;
  border-bottom-color:var(--color-brand,#818cf8)}
.tab .n{color:var(--color-ink-faint,#5a6172);font-weight:400;margin-left:5px;font-size:11px}
.stand{display:flex;align-items:center;gap:8px;padding:8px 12px;border-radius:9px;font-size:11.5px;
  border:1px dashed var(--color-line-strong,rgba(255,255,255,.16));
  color:var(--color-ink-faint,#5a6172)}
`;

/** The day's table — a real table, with a header row and hairline dividers. */
const TABLE = `
.tbl{border:1px solid var(--color-line,rgba(255,255,255,.08));overflow:hidden;
  border-radius:var(--radius-panel,14px);background:var(--color-surface-raised,#11141f)}
.tr{display:grid;grid-template-columns:1.5fr .9fr .8fr .7fr .8fr 1.5fr;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.tr:last-child{border-bottom:none}
.tr>div{padding:11px 13px;display:flex;align-items:center;gap:7px;min-width:0}
.tr.hd>div{padding:8px 13px;font-size:10.5px;text-transform:uppercase;letter-spacing:.08em;
  color:var(--color-ink-faint,#5a6172);background:var(--go-ink-wash)}
.tr.act{cursor:pointer}
.tr.act:hover{background:var(--go-row-hover)}
.tr .nm{display:flex;flex-direction:column;gap:2px;align-items:flex-start}
.tr .nm b{font-weight:600}
.tr .nm span{font-size:11px;color:var(--color-ink-faint,#5a6172)}
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
