/**
 * The module's stylesheet — the approved design, on published tokens.
 *
 * # It is the gold mockup's own CSS, retargeted
 *
 * The frames were drawn against private variables, and each is a shell token
 * wearing a different name — the mapping GuestOps established, and these frames
 * use the same palette:
 *
 * ```text
 * --indigo #818cf8  →  --color-brand        --text  #eef1f8  →  --color-ink
 * --ok/warn/bad     →  --color-ok/warn/bad  --dim   #98a0b4  →  --color-ink-muted
 * --line2           →  --color-line-strong  --faint #5c6375  →  --color-ink-faint
 * ```
 *
 * **Only published token names are referenced** — `SHELL-Q30`. The radius is
 * `--r-md`, never `--radius-md`: the shell deliberately avoids that spelling
 * because it would redefine Tailwind's `rounded-md` for 48 call sites, so a
 * module asking for it always falls back and never matches the platform.
 *
 * Fallbacks remain on every colour. A hosted module is styled by whatever the
 * host injects, and one that assumed a token was present would render
 * unreadable text on a transparent ground the first time it was not.
 *
 * # Scrollbars are styled once, here
 *
 * ADR 0111. Trackless, arrowless, a thin thumb — and never restyled in a
 * screen, because two scrollbar rules in one module is how a design acquires
 * two scrollbars.
 */

/**
 * The stylesheet element, built once and re-attached with each screen.
 *
 * **Held, not appended once.** A screen change replaces the root's children, so
 * a stylesheet appended at mount is deleted by the first render — the module
 * then draws itself as an unstyled column, and neither the type-check nor the
 * suite can see it. GuestOps found that one with a capture.
 *
 * @param parts each screen's own rules, composed into the single element
 * @returns the style element
 */
export function stylesheet(parts: readonly string[] = []): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [CHROME, ...parts].join("\n");
  return style;
}

const CHROME = `
*{box-sizing:border-box;margin:0}
::-webkit-scrollbar{width:6px;height:6px}
::-webkit-scrollbar-track{background:transparent}
::-webkit-scrollbar-thumb{background:color-mix(in srgb, var(--color-ink-faint) 60%, transparent);border-radius:3px}
::-webkit-scrollbar-button{display:none}

.wf{height:100vh;display:grid;grid-template-columns:240px 1fr;
    background:var(--color-surface,#0b0d14);color:var(--color-ink,#e8ebf4);
    font:13.5px/1.55 var(--font-sans,"Segoe UI",system-ui,sans-serif);
    font-variant-numeric:tabular-nums}

.rail{border-right:1px solid var(--color-line,rgb(255 255 255/.07));padding:20px 12px;
      display:flex;flex-direction:column;gap:1px;min-width:0}
.app{display:flex;gap:11px;align-items:center;font-weight:600;font-size:14.5px;padding:0 10px 18px}
.mark{width:26px;height:26px;border-radius:8px;
      background:var(--color-surface-raised,#11141f);
      border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
      display:grid;place-items:center;font-size:12px;color:var(--color-brand,#818cf8)}
.ri{display:flex;gap:11px;align-items:center;padding:8px 12px;border-radius:10px;
    color:var(--color-ink-muted,#8b93a7);font-size:13.5px;cursor:pointer}
.ri.on{background:var(--color-surface-raised,#11141f);color:var(--color-ink,#e8ebf4);
       box-shadow:inset 2.5px 0 0 var(--color-brand,#818cf8)}
.ri .cnt{margin-left:auto;font-size:11px;color:var(--color-ink-faint,#5a6172)}
.me{margin-top:auto;padding:12px;border-top:1px solid var(--color-line,rgb(255 255 255/.07));
    font-size:12px;color:var(--color-ink-faint,#5a6172);line-height:1.5}
.me b{display:block;color:var(--color-ink-muted,#8b93a7);font-weight:500}

.main{position:relative;overflow:hidden;display:flex;flex-direction:column;min-width:0}
.head{display:flex;align-items:center;gap:10px;padding:15px 24px 11px}
.ht{font-size:17px;font-weight:600;letter-spacing:-.01em}
.hsub{color:var(--color-ink-faint,#5a6172);font-size:12px;margin-top:2px}
.grow{margin-left:auto}
.body{padding:0 24px 20px;overflow:auto;display:flex;flex-direction:column;gap:12px}

.btn{display:flex;gap:6px;align-items:center;border-radius:9px;padding:5px 11px;
     font-size:12px;white-space:nowrap;cursor:pointer;
     border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
     color:var(--color-ink-muted,#8b93a7)}
.btn.go{background:var(--color-brand,#818cf8);color:var(--color-ink-on-accent,#0b0d14);
        font-weight:600;border-color:transparent}
.sel{display:flex;gap:8px;align-items:center;justify-content:space-between;
     background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line,rgb(255 255 255/.07));
     border-radius:9px;padding:5px 11px;font-size:12.5px;min-width:150px}
.sel i{color:var(--color-ink-faint,#5a6172);font-style:normal}

.pill{padding:3px 11px;border-radius:99px;font-size:11px;font-weight:600;
      width:fit-content;white-space:nowrap}
.pill.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent));color:var(--color-ok,#34d399)}
.pill.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent));color:var(--color-warn,#fbbf24)}
.pill.bad{background:color-mix(in srgb, var(--color-bad) 13%, transparent));color:var(--color-bad,#f87171)}
.pill.neu{background:var(--color-surface-raised,#11141f);color:var(--color-ink-muted,#8b93a7)}

.panel{background:var(--color-surface-raised,#11141f);
       border:1px solid var(--color-line,rgb(255 255 255/.07));
       border-radius:var(--radius-panel,1rem);padding:14px 16px}
.note{font-size:12px;line-height:1.6;color:var(--color-ink-muted,#8b93a7)}
.note b{color:var(--color-ink,#e8ebf4)}
`;
