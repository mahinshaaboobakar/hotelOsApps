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
    background:var(--color-surface,#0b0d14);color:var(--color-ink,#e9ecf5);
    font:13.5px/1.5 var(--font-sans,"Segoe UI",system-ui,sans-serif);font-variant-numeric:tabular-nums}
.head{display:flex;align-items:center;gap:22px;padding:0 22px;height:56px;flex:none;
      border-bottom:1px solid var(--color-line,rgb(255 255 255/.09))}
.app{display:flex;align-items:center;gap:10px;font-weight:600;margin-right:14px}
.mark{width:22px;height:22px;border-radius:6px;display:grid;place-items:center;font-size:12px;
      background:var(--color-brand,#6b7cff);color:var(--color-ink-on-accent,#0b0d14)}
.tab{background:none;border:0;border-bottom:2px solid transparent;color:var(--color-ink-muted,#9aa3b8);
     padding:19px 2px;font:inherit;font-size:13px;cursor:pointer}
.tab.on{color:var(--color-ink,#e9ecf5);border-bottom-color:var(--color-brand,#6b7cff)}
.search{margin-left:auto;color:var(--color-ink-faint,#5d657a);border:1px solid var(--color-line,rgb(255 255 255/.09));
        border-radius:8px;padding:6px 12px;font-size:12px;min-width:220px}
.who{color:var(--color-ink-faint,#5d657a);font-size:12px}
.body{padding:22px;overflow:auto;display:flex;flex-direction:column;gap:12px;min-height:0}
.subnav{display:flex;gap:4px;border-bottom:1px solid var(--color-line,rgb(255 255 255/.09))}
.subnav .tab{padding:8px 12px;font-size:12px;margin-bottom:-1px}
.strip{display:flex;gap:26px;font-size:12px;color:var(--color-ink-muted,#9aa3b8);padding:8px 12px;
       border:1px solid var(--color-line,rgb(255 255 255/.09));border-radius:8px}
.strip b{color:var(--color-ink,#e9ecf5);font-size:14px;margin-right:4px}
.strip .end{margin-left:auto}
.chips{display:flex;flex-wrap:wrap;gap:8px;align-items:center}
.chip{background:none;border:1px solid var(--color-line,rgb(255 255 255/.09));border-radius:8px;padding:6px 10px;
      font:inherit;font-size:12px;color:var(--color-ink-muted,#9aa3b8);cursor:pointer}
.chip.on{border-color:var(--color-brand,#6b7cff);color:var(--color-ink,#e9ecf5)}
.grow{margin-left:auto}
.btn{background:none;border:1px solid var(--color-line-strong,rgb(255 255 255/.14));border-radius:8px;padding:7px 14px;
     font:inherit;font-size:13px;color:var(--color-ink,#e9ecf5);cursor:pointer}
.btn.pri{background:var(--color-brand,#6b7cff);border-color:transparent;color:var(--color-ink-on-accent,#0b0d14)}
.btn.off{color:var(--color-ink-faint,#5d657a);border-style:dashed}
.btn.sm{padding:2px 8px;font-size:11px}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
table{width:100%;border-collapse:collapse;font-size:13px}
th{text-align:left;color:var(--color-ink-faint,#5d657a);font-weight:500;font-size:11px;letter-spacing:.08em;
   text-transform:uppercase;padding:8px 10px;border-bottom:1px solid var(--color-line,rgb(255 255 255/.09))}
td{padding:10px;border-bottom:1px solid var(--color-line,rgb(255 255 255/.09));vertical-align:top}
tr.sel td{background:color-mix(in srgb, var(--color-brand,#6b7cff) 8%, transparent)}
tr.pick{cursor:pointer}
.num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#9aa3b8)}
.mono{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#9aa3b8)}
.dim{color:var(--color-ink-faint,#5d657a)}
.pager{display:flex;justify-content:space-between;align-items:center;font-size:12px;color:var(--color-ink-faint,#5d657a);padding-top:4px}
.pg{background:none;border:1px solid var(--color-line,rgb(255 255 255/.09));border-radius:6px;padding:2px 8px;margin-left:4px;
    font:inherit;font-size:12px;color:var(--color-ink-muted,#9aa3b8);cursor:pointer}
.pg.on{color:var(--color-ink,#e9ecf5);border-color:var(--color-brand,#6b7cff)}
.cols{display:grid;grid-template-columns:1fr 1fr;gap:18px}
.cols3{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}
.card{border:1px solid var(--color-line,rgb(255 255 255/.09));border-radius:var(--radius-panel,12px);padding:16px;
      background:var(--color-surface-raised,#11141f)}
.card h3{margin:0 0 10px;font-size:13px;font-weight:600;display:flex;gap:8px;align-items:center}
.kv{display:grid;grid-template-columns:150px 1fr;gap:6px 14px;font-size:13px}
.kv .k{color:var(--color-ink-faint,#5d657a)}
.field{border:1px solid var(--color-line,rgb(255 255 255/.09));border-radius:8px;padding:8px 12px;font-size:13px;
       background:var(--color-surface,#0b0d14);color:var(--color-ink,#e9ecf5);margin:4px 0 12px;display:flex;gap:8px}
.field.ph{color:var(--color-ink-faint,#5d657a)}
.field .hint{margin-left:auto}
label.lbl{font-size:11px;color:var(--color-ink-faint,#5d657a);letter-spacing:.08em;text-transform:uppercase;display:block}
.tl{border-left:2px solid var(--color-line,rgb(255 255 255/.09));padding-left:16px;margin:8px 0 0 6px}
.tl .ev{margin-bottom:10px;font-size:13px}
.tl .ev b{display:block;font-weight:600}
.tl .ev span{color:var(--color-ink-faint,#5d657a);font-size:12px}
.timer{font:600 30px ui-monospace,Menlo,monospace;color:var(--color-brand,#6b7cff);line-height:1;display:flex;align-items:center;gap:10px}
.timer i{display:inline-block;width:10px;height:10px;border-radius:50%;background:var(--color-ok,#3ecf8e)}
.scroll{max-height:168px;overflow:auto}
.more{font-size:11px;color:var(--color-ink-faint,#5d657a);text-align:center;padding-top:4px}
.bar{height:6px;border-radius:3px;background:var(--color-line,rgb(255 255 255/.09));overflow:hidden;margin-top:8px}
.bar i{display:block;height:100%;background:var(--color-ok,#3ecf8e)}
.bar i.bad{background:var(--color-bad,#ff5c7a)}
.wrow{display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255/.09));font-size:13px}
.wrow:last-child{border:0}
.tog{display:inline-block;width:34px;height:18px;border-radius:9px;background:var(--color-line,rgb(255 255 255/.09));position:relative;vertical-align:middle;margin-right:8px}
.tog.on{background:var(--color-ok,#3ecf8e)}
.tog i{position:absolute;top:2px;left:2px;width:14px;height:14px;border-radius:50%;background:var(--color-ink,#e9ecf5)}
.tog.on i{left:18px}
.dlg{border:1px solid var(--color-brand,#6b7cff);border-radius:var(--radius-panel,12px);padding:16px;
     background:color-mix(in srgb, var(--color-brand,#6b7cff) 6%, transparent)}
.note{border-left:3px solid var(--color-brand,#6b7cff);padding:10px 16px;color:var(--color-ink-muted,#9aa3b8);font-size:13px;
      background:color-mix(in srgb, var(--color-brand,#6b7cff) 5%, transparent)}
.note b{color:var(--color-ink,#e9ecf5)}
.stars{font-size:26px;letter-spacing:4px;color:var(--color-warn,#f5b53f)}
.sect{font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:var(--color-ink-faint,#5d657a);margin:6px 0 2px}
`;
