/**
 * The module's chrome, as a stylesheet — the current era: one window, top
 * tabs, no left rail (mockup 01, the intro). Every colour is a `var()` on a
 * token the shell publishes (ADR 0106); the fallbacks are for the capture
 * harness only and match the approved dark frames.
 */

/** One `<style>` holding the chrome and the screens' own sheets. */
export function stylesheet(parts: readonly string[] = []): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [CHROME, ...parts].join("\n");
  return style;
}

const CHROME = `
::-webkit-scrollbar{width:6px;height:6px}
::-webkit-scrollbar-track{background:transparent}
::-webkit-scrollbar-thumb{background:color-mix(in srgb, var(--color-ink-faint) 60%, transparent);border-radius:3px}
.jb{height:100vh;display:flex;flex-direction:column;min-width:0;
    background:var(--color-surface,#0b0d14);color:var(--color-ink,#e8ebf4);
    font:14px/1.5 var(--font-sans,system-ui, -apple-system, "Segoe UI", sans-serif);font-variant-numeric:tabular-nums}
.head{display:flex;align-items:center;gap:22px;padding:0 22px;height:56px;flex:none;
      border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.app{display:flex;align-items:center;gap:10px;font-weight:600;margin-right:14px}
.mark{width:22px;height:22px;border-radius:6px;display:grid;place-items:center;font-size:12px;
      color:var(--color-ink-on-accent,#0b0d14);
      background:linear-gradient(135deg, var(--color-brand,#818cf8),
                 color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171)))}
.tab{background:none;border:0;border-bottom:2px solid transparent;color:var(--color-ink-muted,#8b93a7);
     padding:19px 2px;font:inherit;font-size:13px;cursor:pointer}
.tab.on{color:var(--color-ink,#e8ebf4);border-bottom-color:var(--color-brand,#818cf8)}
.search{margin-left:auto;color:var(--color-ink-faint,#5a6172);border:1px solid var(--color-line,rgb(255 255 255 / 0.07));
        border-radius:8px;padding:6px 12px;font-size:12px;min-width:220px}
.who{color:var(--color-ink-faint,#5a6172);font-size:12px}
.body{padding:22px;overflow:auto;min-height:0}
.subnav{display:flex;gap:4px;margin-bottom:16px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.subnav .tab{padding:8px 12px;font-size:12px;margin-bottom:-1px}
.strip{display:flex;gap:26px;font-size:12px;color:var(--color-ink-muted,#8b93a7);padding:8px 12px;margin-bottom:12px;
       border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:8px}
.strip b{color:var(--color-ink,#e8ebf4);font-size:14px;margin-right:4px}
.strip .end{margin-left:auto}
.chips{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-bottom:12px}
.chip{background:none;border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:8px;padding:6px 10px;
      font:inherit;font-size:12px;color:var(--color-ink-muted,#8b93a7);cursor:pointer}
.chip.on{border-color:var(--color-brand,#818cf8);color:var(--color-ink,#e8ebf4)}
.grow{margin-left:auto}
.btn{background:none;border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:8px;padding:7px 14px;
     font:inherit;font-size:13px;color:var(--color-ink,#e8ebf4);cursor:pointer;text-align:start}
.btn.pri{border-color:transparent;color:var(--color-ink-on-accent,#0b0d14);text-align:start;
         background:linear-gradient(135deg, var(--color-brand,#818cf8),
                    color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171)))}
.btn.off{color:var(--color-ink-faint,#5a6172);border-style:dashed}
.btn.sm{padding:2px 8px;font-size:11px}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.row.act{margin:10px 0 14px}
.stack>*+*{margin-top:14px}
.cols+.cols,.cols3+.cols,.cols+table{margin-top:14px}
table{width:100%;border-collapse:collapse;font-size:13px}
th{text-align:left;color:var(--color-ink-faint,#5a6172);font-weight:500;font-size:11px;letter-spacing:.08em;
   text-transform:uppercase;padding:8px 10px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
td{padding:10px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));vertical-align:top}
tr.sel td{background:color-mix(in srgb, var(--color-brand,#818cf8) 8%, transparent)}
tr.pick{cursor:pointer}
/* The row you came back from, tinted as frame 1 tints it — a person returning
   to the board should not have to find their place again. Mixed from the
   published brand rather than written as a colour: a module may not invent
   one. Measured finding, 2026-09-05. */
tr.sel td{background:color-mix(in srgb, var(--color-brand,#818cf8) 8%, transparent)}
.num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7);white-space:nowrap}
.mono{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.dim{color:var(--color-ink-faint,#5a6172)}
table+.mono,.kv+.mono{margin-top:8px}
/* The pager is the list's floor — the standard's §6, ported 2026-09-05.
   Two halves, and neither works alone: the list grows so a short one still
   puts the pager at the bottom, and the pager sticks so a full page does not
   hide it behind a scroll. The growth is scoped to a list that HAS a pager,
   never to every table — a table that grew on the Live tab would push the
   concern note off the screen.
   The negative margins cancel .body's own 22px and re-supply it here, so the
   strip is full-width while stuck instead of inset and then jumping when the
   list ends. The ground is the published surface, never a literal: a hardcoded
   colour is a dark-theme decision frozen into a module a light property runs.
   The cost, stated: an opaque strip covers the last rows while scrolling. */
.body:has(> .pager){display:flex;flex-direction:column}
table:has(~ .pager){flex:1 1 auto;min-height:0}
.pager{display:flex;justify-content:space-between;align-items:center;font-size:12px;
       color:var(--color-ink-faint,#5a6172);position:sticky;bottom:0;
       background:var(--color-surface,#0b0d14);margin:0 -22px -22px;padding:10px 22px 22px}
.pg{background:none;border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:6px;padding:2px 8px;margin-left:4px;
    font:inherit;font-size:12px;color:var(--color-ink-muted,#8b93a7);cursor:pointer}
.pg.on{color:var(--color-ink,#e8ebf4);border-color:var(--color-brand,#818cf8)}
.pg[disabled]{color:var(--color-ink-faint,#5a6172);cursor:default;opacity:.5}
.cols{display:grid;grid-template-columns:1fr 1fr;gap:18px}
.cols3{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}
.card{border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:var(--radius-panel,1rem);padding:16px;
      background:var(--color-surface-raised,#11141f)}
.card h3{margin:0 0 10px;font-size:13px;font-weight:600;display:flex;gap:8px;align-items:center}
.kv{display:grid;grid-template-columns:150px 1fr;gap:6px 14px;font-size:13px}
.kv .k{color:var(--color-ink-faint,#5a6172)}
.field{border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:8px;padding:8px 12px;font-size:13px;
       background:var(--color-surface,#0b0d14);color:var(--color-ink,#e8ebf4);margin:4px 0 12px;display:flex;gap:8px}
.field.ph{color:var(--color-ink-faint,#5a6172)}
/* A field a person types into is the same field, drawn: same border, same
   ground, same size — so a form that acts looks like the form that was
   approved rather than like the browser's idea of one. */
input.field,select.field,textarea.field{width:100%;box-sizing:border-box;display:block;font:inherit;font-size:13px;
       appearance:none;outline:none}
input.field:focus,select.field:focus,textarea.field:focus{border-color:var(--color-brand,#818cf8)}
textarea.field{resize:vertical;min-height:64px}
input.tog{width:16px;height:16px;appearance:auto;margin:0 8px 0 0;accent-color:var(--color-brand,#818cf8);position:static}
.said{margin:8px 0 0;font-size:12px}
.said.bad{color:var(--color-bad,#f87171)}
.said.ok{color:var(--color-ok,#34d399)}
.ask{border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:var(--radius-panel,1rem);
     padding:14px 16px;margin:10px 0 14px;background:var(--color-surface-raised,#11141f)}
.field .hint{margin-left:auto}
.row>.field{margin:0}
.tl+.field{margin-top:12px}
label.lbl{font-size:11px;color:var(--color-ink-faint,#5a6172);letter-spacing:.08em;text-transform:uppercase;display:block}
.tl{border-left:2px solid var(--color-line,rgb(255 255 255 / 0.07));padding-left:16px;margin:8px 0 0 6px}
.tl .ev{margin-bottom:10px;font-size:13px}
.tl .ev b{display:block;font-weight:600}
.tl .ev span{color:var(--color-ink-faint,#5a6172);font-size:12px}
.timer{font:600 30px ui-monospace,Menlo,monospace;color:var(--color-brand,#818cf8);line-height:1;display:flex;align-items:center;gap:10px}
.timer i{display:inline-block;width:10px;height:10px;border-radius:50%;background:var(--color-ok,#34d399)}
.scroll{max-height:168px;overflow:auto}
.more{font-size:11px;color:var(--color-ink-faint,#5a6172);text-align:center;padding-top:4px}
.bar{height:6px;border-radius:3px;background:var(--color-line,rgb(255 255 255 / 0.07));overflow:hidden;margin-top:8px}
.bar i{display:block;height:100%;background:var(--color-ok,#34d399)}
.bar i.bad{background:var(--color-bad,#f87171)}
.wrow{display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));font-size:13px}
.wrow:last-child{border:0}
.tog{display:inline-block;width:34px;height:18px;border-radius:9px;background:var(--color-line,rgb(255 255 255 / 0.07));position:relative;vertical-align:middle;margin-right:8px}
.tog.on{background:var(--color-ok,#34d399)}
.tog i{position:absolute;top:2px;left:2px;width:14px;height:14px;border-radius:50%;background:var(--color-ink,#e8ebf4)}
.tog.on i{left:18px}
*+.dlg{margin-top:14px}
.dlg{border:1px solid var(--color-brand,#818cf8);border-radius:var(--radius-panel,1rem);padding:16px;
     background:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent)}
p.lede,.note{border-left:3px solid var(--color-brand,#818cf8);padding:10px 16px;color:var(--color-ink-muted,#8b93a7);font-size:13px;
      background:color-mix(in srgb, var(--color-brand,#818cf8) 5%, transparent)}
.note b{color:var(--color-ink,#e8ebf4)}
.stars{font-size:26px;letter-spacing:4px;color:var(--color-warn,#fbbf24)}
.sect{font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin:18px 0 8px}
`;
