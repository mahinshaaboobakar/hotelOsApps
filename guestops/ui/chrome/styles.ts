/**
 * The module's stylesheet — the approved design, on published tokens.
 *
 * # It is the gold mockup's own CSS, retargeted
 *
 * The mockup was drawn against its own private variables, and every one of them
 * turned out to be a shell token wearing a different name:
 *
 * ```text
 * --indigo #818cf8  →  --color-brand         --text  #eef1f8  →  --color-ink
 * --cyan   #7dd3fc  →  --color-aurora-1      --dim   #98a0b4  →  --color-ink-muted
 * --violet #c084fc  →  --color-aurora-3      --faint #5c6375  →  --color-ink-faint
 * --ok/warn/bad     →  --color-ok/warn/bad   --line2          →  --color-line-strong
 * ```
 *
 * So the design and the platform already agreed, and this file only has to
 * spell the agreement out in names the shell publishes.
 *
 * **Only published token names are referenced** — SHELL-Q30's contract. The
 * radius is `--r-md`, never `--radius-md`: the shell deliberately avoids that
 * spelling because it would redefine Tailwind's `rounded-md` for 48 call sites
 * (`apps/desktop/src/styles.css:26`), so a module asking for it always falls
 * back and never matches the platform's corners.
 *
 * Fallbacks remain on every colour. A hosted module is styled by whatever the
 * host injects, and a module that assumed a token was present would render
 * unreadable text on a transparent ground the first time it was not.
 */

/** The stylesheet element, built once and re-attached with each screen. */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [SHELL, TABLE, PANEL, MARKS].join("");
  return style;
}

/** The window: rail, header, body — the shape every frame shares. */
const SHELL = `
.go{display:flex;height:100vh;font-size:13px;color:var(--color-ink,#e8ebf4);
  background:var(--color-surface,#0b0d14)}
.rail{width:212px;flex:none;padding:20px 12px;display:flex;flex-direction:column;gap:1px;
  border-right:1px solid var(--color-line,rgba(255,255,255,.08))}
.app{display:flex;gap:11px;align-items:center;font-weight:600;font-size:14.5px;padding:0 10px 18px}
.mark{width:26px;height:26px;border-radius:8px;display:grid;place-items:center;font-size:11px;
  font-weight:700;color:var(--color-aurora-1,#7dd3fc);
  border:1px solid var(--color-line-strong,rgba(255,255,255,.16));
  background:linear-gradient(120deg,rgba(125,211,252,.25),rgba(192,132,252,.25))}
.ri{display:flex;gap:11px;align-items:center;padding:8px 12px;border-radius:10px;
  color:var(--color-ink-muted,#8b93a7);font-size:13.5px;cursor:pointer;
  background:none;border:0;width:100%;text-align:left;font-family:inherit}
.ri:hover{color:var(--color-ink,#e8ebf4)}
.ri.on{color:var(--color-ink,#e8ebf4);font-weight:500;
  background:linear-gradient(90deg,rgba(129,140,248,.18),rgba(129,140,248,.05));
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
  color:var(--color-ink-on-accent,#0b0d14);
  background:linear-gradient(120deg,var(--color-aurora-1,#7dd3fc),
    var(--color-brand,#818cf8) 55%,var(--color-aurora-3,#c084fc));
  box-shadow:0 6px 18px rgba(129,140,248,.35)}
.danger{border:1px solid rgba(248,113,113,.45);border-radius:11px;padding:8px 15px;
  font-size:12.5px;color:var(--color-bad,#f87171);background:none}
.mini{border:1px solid var(--color-line-strong,rgba(255,255,255,.16));border-radius:9px;
  padding:6px 12px;font-size:12px;color:var(--color-ink,#e8ebf4);background:none}
.mini.pri{background:rgba(129,140,248,.18);border-color:rgba(129,140,248,.5)}
.strip{display:flex;gap:10px}
.stat{flex:1;padding:11px 14px;border-radius:var(--r-md,12px);
  border:1px solid var(--color-line,rgba(255,255,255,.08));
  background:var(--color-surface-raised,rgba(22,26,40,.62))}
.stat b{display:block;font-size:15px;font-weight:600}
.stat span{color:var(--color-ink-faint,#5a6172);font-size:11.5px}
.stat.on{box-shadow:inset 0 0 0 1.5px rgba(129,140,248,.5)}
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
.tbl{border:1px solid var(--color-line,rgba(255,255,255,.08));border-radius:14px;overflow:hidden;
  background:var(--color-surface-raised,rgba(22,26,40,.62))}
.tr{display:grid;grid-template-columns:1.5fr .9fr .8fr .7fr .8fr 1.5fr;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.tr:last-child{border-bottom:none}
.tr>div{padding:11px 13px;display:flex;align-items:center;gap:7px;min-width:0}
.tr.hd>div{padding:8px 13px;font-size:10.5px;text-transform:uppercase;letter-spacing:.08em;
  color:var(--color-ink-faint,#5a6172);background:var(--color-surface-sunken,rgba(10,12,18,.32))}
.tr.act{cursor:pointer}
.tr.act:hover{background:rgba(129,140,248,.06)}
.tr .nm{display:flex;flex-direction:column;gap:2px;align-items:flex-start}
.tr .nm b{font-weight:600}
.tr .nm span{font-size:11px;color:var(--color-ink-faint,#5a6172)}
`;

/** Cards, their header bands, label–value rows, and the activity timeline. */
const PANEL = `
.cols{display:grid;grid-template-columns:1.55fr 1fr;gap:14px;align-items:start}
.card{border:1px solid var(--color-line,rgba(255,255,255,.08));border-radius:14px;overflow:hidden;
  background:var(--color-surface-raised,rgba(22,26,40,.62))}
.ch{padding:11px 15px;font-size:11px;text-transform:uppercase;letter-spacing:.08em;
  display:flex;align-items:center;gap:8px;color:var(--color-ink-faint,#5a6172);
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.ch .grow{margin-left:auto;text-transform:none;letter-spacing:0;font-size:11.5px}
.cb{padding:13px 15px;display:flex;flex-direction:column;gap:11px}
.fr{display:flex;align-items:flex-start;gap:10px;font-size:12.5px}
.fr .k{color:var(--color-ink-faint,#5a6172);width:118px;flex:none;font-size:12px}
.fr .v{color:var(--color-ink,#e8ebf4);display:flex;align-items:center;gap:7px;flex-wrap:wrap}
.ban{display:flex;gap:11px;align-items:center;border-radius:12px;padding:10px 14px;font-size:12.5px;
  border:1px solid rgba(251,191,36,.35);background:rgba(251,191,36,.08)}
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
.te.pms .g i{background:var(--color-aurora-1,#7dd3fc)}
.te.override .g i{background:var(--color-aurora-3,#c084fc)}
.te.disagrees .g i{background:var(--color-warn,#fbbf24)}
.te .d{padding:7px 0 7px 12px;display:flex;flex-direction:column;gap:3px}
.te .d span{color:var(--color-ink-faint,#5a6172);font-size:11.5px}
`;

/** The marks, the locks, the pills and the inline links. */
const MARKS = `
.sh{display:inline-flex;gap:6px;align-items:center;padding:3px 9px;border-radius:7px;
  font-size:11.5px;font-weight:500;white-space:nowrap;border:1px solid transparent}
.sh i{width:6px;height:6px;border-radius:50%;display:inline-block}
.sh.pms{background:rgba(125,211,252,.13);color:var(--color-aurora-1,#7dd3fc)}
.sh.pms i{background:var(--color-aurora-1,#7dd3fc)}
.sh.override{background:rgba(192,132,252,.15);color:var(--color-aurora-3,#c084fc)}
.sh.override i{background:var(--color-aurora-3,#c084fc)}
.sh.disagrees{background:rgba(251,191,36,.14);color:var(--color-warn,#fbbf24)}
.sh.disagrees i{background:var(--color-warn,#fbbf24)}
.sh.other{background:rgba(52,211,153,.12);color:var(--color-ok,#34d399)}
.sh.other i{background:var(--color-ok,#34d399)}
.sh.dayuse{background:rgba(52,211,153,.12);color:var(--color-ok,#34d399)}
.sh.unknown{background:rgba(248,113,113,.10);color:var(--color-bad,#f87171);
  border-color:rgba(248,113,113,.3)}
.sh.missing{color:var(--color-ink-faint,#5a6172);border-style:dashed;
  border-color:var(--color-line,rgba(255,255,255,.08))}
.lock{font-size:10px;letter-spacing:.04em;padding:1px 5px;border-radius:5px;
  color:var(--color-ink-faint,#5a6172);
  border:1px solid var(--color-line,rgba(255,255,255,.08))}
.pill{padding:3px 11px;border-radius:99px;font-size:11px;font-weight:600;white-space:nowrap}
.pill.neutral{background:rgba(255,255,255,.06);color:var(--color-ink-muted,#8b93a7)}
.pill.ok{background:rgba(52,211,153,.14);color:var(--color-ok,#34d399)}
.pill.warn{background:rgba(251,191,36,.14);color:var(--color-warn,#fbbf24)}
.link{color:var(--color-brand,#818cf8);background:none;border:0;padding:0;font:inherit;
  cursor:pointer}
.un{color:var(--color-ink-faint,#5a6172);font-style:italic}
`;
