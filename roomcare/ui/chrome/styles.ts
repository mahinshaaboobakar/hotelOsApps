/**
 * Room Care's one stylesheet — page 64 as it stands on 2026-09-13, on the
 * published tokens only, every fallback the shell's own value.
 *
 * Three rulings shape it beyond Jobs' sheet it started from: only the list
 * scrolls (`CORE-Q28`, 2026-09-09), so a body with a pager clips and the list is
 * the scroll container; the overlay pair (§9), a sheet that composes and a
 * dialog that confirms, positioned absolutely as a sibling of the body; and the
 * whole-house views — the board's wall and the Room states sheet — scroll inside
 * the window with no pager at all, the stated departure the owner ruled.
 * (No backticks inside the sheet: it is a template literal.)
 */

export function stylesheet(parts: readonly string[] = []): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [CHROME, TABLES, HOUSE, OVERLAY, SETUP, ...parts].join("\n");
  return style;
}

const CHROME = `
*{box-sizing:border-box}
.rc{height:100vh;display:flex;flex-direction:column;min-width:0;position:relative;
    background:var(--color-surface,#0b0d14);color:var(--color-ink,#e8ebf4);
    font:14px/1.5 var(--font-sans,system-ui, -apple-system, "Segoe UI", sans-serif);font-variant-numeric:tabular-nums}
.head{display:flex;align-items:center;gap:22px;padding:0 22px;height:56px;flex:none;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.app{display:flex;align-items:center;gap:10px;font-weight:600;margin-right:14px}
.mark{width:22px;height:22px;border-radius:6px;display:grid;place-items:center;font-size:12px;color:var(--color-ink-on-accent,#0b0d14);
      background:linear-gradient(135deg, var(--color-brand,#818cf8), color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171)))}
.tab{background:none;border:0;border-bottom:2px solid transparent;color:var(--color-ink-muted,#8b93a7);padding:19px 2px;
     font:inherit;line-height:inherit;font-size:13px;cursor:pointer}
.tab.on{color:var(--color-ink,#e8ebf4);border-bottom-color:var(--color-brand,#818cf8)}
.who{margin-left:auto;color:var(--color-ink-faint,#5a6172);font-size:12px}
.body{flex:1 1 auto;padding:22px;overflow:auto;min-height:0;display:flex;flex-direction:column}
.body:has(.pager),.body:has(.house){overflow:hidden}
.subnav{display:flex;gap:4px;margin-bottom:16px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));flex:none}
.subnav .tab{padding:8px 12px;font-size:12px;margin-bottom:-1px}
.strip{display:flex;flex-wrap:wrap;gap:22px;font-size:12px;color:var(--color-ink-muted,#8b93a7);padding:8px 12px;margin-bottom:12px;align-items:center;flex:none;
       border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:8px}
.strip b{color:var(--color-ink,#e8ebf4);font-size:14px;margin-right:4px}
.strip .end{margin-left:auto}
.chips{display:flex;flex-wrap:wrap;gap:6px;align-items:center;margin-bottom:10px;flex:none}
.chips .lbl{font-size:11px;color:var(--color-ink-faint,#5a6172);margin:0 4px 0 10px}
.grow{margin-left:auto}
.btn{background:none;border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:8px;padding:7px 14px;
     font:inherit;line-height:inherit;font-size:13px;color:var(--color-ink,#e8ebf4);cursor:pointer;text-align:start}
.btn.pri{border-color:transparent;color:var(--color-ink-on-accent,#0b0d14);
         background:linear-gradient(135deg, var(--color-brand,#818cf8), color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171)))}
.btn.off{color:var(--color-ink-faint,#5a6172);border-style:dashed;cursor:default}
.btn.danger{color:var(--color-bad,#f87171);border-color:color-mix(in srgb, var(--color-bad,#f87171) 45%, transparent)}
.btn.danger.confirm{border-color:transparent;background:var(--color-bad,#f87171);color:var(--color-ink-on-accent,#0b0d14);font-weight:600}
.btn.sm{padding:2px 8px;font-size:11px}
.btn.chip{padding:5px 10px;font-size:12px;color:var(--color-ink-muted,#8b93a7)}
select.btn.chip{background:var(--color-surface,#0b0d14)}
.btn.chip.on{border-color:var(--color-brand,#818cf8);color:var(--color-ink,#e8ebf4)}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.cols{display:grid;grid-template-columns:1fr 1fr;gap:18px}
.card{border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:var(--radius-panel,1rem);padding:16px;background:var(--color-surface-raised,#11141f)}
.card h3{margin:0 0 10px;font-size:13px;font-weight:600}
.kv{display:grid;grid-template-columns:170px 1fr;gap:6px 14px;font-size:13px}
.kv .k{color:var(--color-ink-faint,#5a6172)}
.sect{font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin:18px 0 8px}
.note{border-left:3px solid var(--color-brand,#818cf8);padding:10px 16px;margin:10px 0;color:var(--color-ink-muted,#8b93a7);font-size:13px;
      background:color-mix(in srgb, var(--color-brand,#818cf8) 5%, transparent)}
.note b{color:var(--color-ink,#e8ebf4)}
.note.bad{border-color:var(--color-bad,#f87171);background:var(--color-bad-soft,rgb(248 113 113 / 0.12))}
.note.warn{border-color:var(--color-warn,#fbbf24);background:var(--color-warn-soft,rgb(251 191 36 / 0.12))}
.num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7);white-space:nowrap}
.mono{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.dim{color:var(--color-ink-faint,#5a6172)}
.tl{border-left:2px solid var(--color-line,rgb(255 255 255 / 0.07));padding-left:16px;margin:8px 0 0 6px}
.tl .ev{margin-bottom:10px;font-size:13px}
.tl .ev b{display:block;font-weight:600}
.tl .ev span{color:var(--color-ink-faint,#5a6172);font-size:12px}
.big{font-size:34px;font-weight:600;line-height:1}
label.lbl{font-size:11px;color:var(--color-ink-faint,#5a6172);letter-spacing:.07em;text-transform:uppercase;display:block}
.field{border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:10px;padding:9px 12px;font-size:13px;
       background:color-mix(in srgb, var(--color-ink,#e8ebf4) 2%, transparent);color:var(--color-ink,#e8ebf4);margin:4px 0 12px}
input.field,select.field,textarea.field{width:100%;display:block;font:inherit;line-height:inherit;font-size:13px;outline:none}
input.field:focus,select.field:focus,textarea.field:focus{border-color:var(--color-brand,#818cf8)}
.said{margin:8px 0 0;font-size:12px}
.said.bad{color:var(--color-bad,#f87171)}
.said.ok{color:var(--color-ok,#34d399)}
`;

const TABLES = `
table{width:100%;border-collapse:collapse;font-size:13px}
th{text-align:left;color:var(--color-ink-faint,#5a6172);font-weight:500;font-size:11px;letter-spacing:.08em;text-transform:uppercase;
   padding:8px 10px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
td{padding:10px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));vertical-align:top}
tr.pick{cursor:pointer}
tr.sel td{background:color-mix(in srgb, var(--color-brand,#818cf8) 8%, transparent)}
.body > .list:has(~ .pager){flex:1 1 auto;min-height:0;overflow-y:auto;margin:0 -22px;padding:0 22px}
.card .pager{padding-top:8px}
.pager{flex:0 0 auto;display:flex;justify-content:space-between;align-items:center;font-size:12px;color:var(--color-ink-faint,#5a6172);padding:10px 0 0}
.btn.pg{border-radius:6px;padding:2px 8px;margin-left:4px;font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.btn.pg.on{color:var(--color-ink,#e8ebf4);border-color:var(--color-brand,#818cf8)}
.btn.pg[disabled]{color:var(--color-ink-faint,#5a6172);cursor:default;opacity:.5}
.pill{display:inline-block;padding:2px 9px;border-radius:999px;font-size:11px;font-weight:600;letter-spacing:.04em;white-space:nowrap;
      border:1px solid var(--color-line,rgb(255 255 255 / 0.07));color:var(--color-ink-muted,#8b93a7)}
.pill.ok{color:var(--color-ok,#34d399);border-color:var(--color-ok,#34d399)}
.pill.warn{color:var(--color-warn,#fbbf24);border-color:var(--color-warn,#fbbf24)}
.pill.bad{color:var(--color-bad,#f87171);border-color:var(--color-bad,#f87171)}
.pill.run{color:var(--color-brand,#818cf8);border-color:var(--color-brand,#818cf8)}
.pill.soft-ok{background:var(--color-ok-soft,rgb(52 211 153 / 0.12));color:var(--color-ok,#34d399);border-color:transparent}
.pill.soft-warn{background:var(--color-warn-soft,rgb(251 191 36 / 0.12));color:var(--color-warn,#fbbf24);border-color:transparent}
.pill.soft-bad{background:var(--color-bad-soft,rgb(248 113 113 / 0.12));color:var(--color-bad,#f87171);border-color:transparent}
tr.dim td{color:var(--color-ink-faint,#5a6172)}
.pill.p1{background:var(--color-bad,#f87171);color:var(--color-ink-on-accent,#0b0d14);border-color:transparent}
.pill.p2{background:var(--color-warn,#fbbf24);color:var(--color-ink-on-accent,#0b0d14);border-color:transparent}
.pill.p3{background:var(--color-brand,#818cf8);color:var(--color-ink-on-accent,#0b0d14);border-color:transparent}
.tag{font-size:10px;letter-spacing:.1em;text-transform:uppercase;padding:1px 5px;border-radius:4px;margin-left:5px;vertical-align:middle;
     border:1px solid var(--color-line,rgb(255 255 255 / 0.07));color:var(--color-ink-faint,#5a6172)}
.tag.man{color:var(--color-warn,#fbbf24);border-color:var(--color-warn,#fbbf24)}
.tag.port{color:var(--color-warn,#fbbf24);border-color:var(--color-warn,#fbbf24)}
.tag.absent{border-style:dashed}
`;

const HOUSE = `
.house{flex:1 1 auto;min-height:0;overflow-y:auto;margin:0 -22px;padding:0 22px}
.legend{font-size:11px;color:var(--color-ink-faint,#5a6172);margin:0 0 8px;flex:none}
.legend span{display:inline-block;margin-right:14px}
.sw{display:inline-block;width:12px;height:12px;border-radius:3px;vertical-align:middle;margin-right:5px}
.sw.dirty,.glyph.dirty,.tile.dirty{background:var(--color-bad,#f87171)}
.sw.clean,.glyph.clean,.tile.clean{background:var(--color-ok,#34d399)}
.sw.insp,.glyph.insp,.tile.insp{background:var(--color-brand,#818cf8)}
.sw.ring{border:2px solid var(--color-ink,#e8ebf4)}
.sw.pend{border:2px dotted var(--color-ink-faint,#5a6172)}
.sw.blk{border:1px solid var(--color-line,rgb(255 255 255 / 0.07));
      background:repeating-linear-gradient(45deg, transparent, transparent 2px, var(--color-line-strong,rgb(255 255 255 / 0.14)) 2px, var(--color-line-strong,rgb(255 255 255 / 0.14)) 4px)}
.grp{display:flex;align-items:center;gap:14px;font-size:12px;color:var(--color-ink-muted,#8b93a7);margin-top:12px;padding:6px 0;
     border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.grp b{color:var(--color-ink,#e8ebf4);font-size:13px}
.tilegrid{display:flex;flex-wrap:wrap;gap:5px;margin:6px 0 14px}
.tile{width:46px;height:34px;border-radius:5px;border:2px solid transparent;font:600 10px ui-monospace,Menlo,monospace;display:grid;place-items:center;
      position:relative;color:var(--color-ink-on-accent,#0b0d14);cursor:pointer;padding:0;line-height:inherit}
.tile.insp{color:var(--color-ink,#e8ebf4)}
.tile.run{border-color:var(--color-ink,#e8ebf4)}
.tile.pend{background:transparent;border:2px dotted var(--color-ink-faint,#5a6172);color:var(--color-ink-muted,#8b93a7)}
.tile.blocked{background:repeating-linear-gradient(45deg,var(--color-surface-raised,#11141f),var(--color-surface-raised,#11141f) 4px,var(--color-line,rgb(255 255 255 / 0.07)) 4px,var(--color-line,rgb(255 255 255 / 0.07)) 8px);
      color:var(--color-ink-muted,#8b93a7);border-color:var(--color-line,rgb(255 255 255 / 0.07))}
.tile.big{width:52px;height:38px}
.grp.mono b{font-family:ui-monospace,Menlo,monospace;font-size:12px}
.tile.done{opacity:.55}
.tile.dim{opacity:.25}
.tile.picked{outline:2px solid var(--color-ink,#e8ebf4);outline-offset:2px}
.tile i{position:absolute;top:-6px;right:-4px;font-style:normal;font-size:10px;background:var(--color-surface,#0b0d14);border-radius:8px;padding:0 3px;
        color:var(--color-ink,#e8ebf4);border:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.tile small{position:absolute;bottom:1px;right:3px;font-size:8px;opacity:.8}
.tile.chg:after{content:"\\270E";position:absolute;top:-7px;right:-4px;font-size:10px;background:var(--color-surface,#0b0d14);color:var(--color-warn,#fbbf24);
      border-radius:8px;padding:0 3px;border:1px solid var(--color-warn,#fbbf24)}
table.wall td{padding:5px 8px;font-size:12px;line-height:1.3;vertical-align:middle;white-space:nowrap}
table.wall th{padding:6px 8px;position:sticky;top:0;background:var(--color-surface,#0b0d14);z-index:2;white-space:nowrap}
table.wall .src{font-family:ui-monospace,Menlo,monospace;font-size:11px;color:var(--color-ink-muted,#8b93a7)}
table.wall tr.g td{background:var(--color-surface-raised,#11141f);color:var(--color-ink,#e8ebf4);font-weight:600;padding:7px 8px;position:sticky;top:29px;z-index:1;cursor:pointer}
table.wall tr.dim td{opacity:.3}
table.wall tr.chg td{background:var(--color-warn-soft,rgb(251 191 36 / 0.12))}
table.wall tr.conf td{background:var(--color-bad-soft,rgb(248 113 113 / 0.12))}
.glyph{display:inline-block;width:9px;height:9px;border-radius:50%;vertical-align:middle;margin-right:5px}
select.cell{border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:6px;padding:2px 7px;font:inherit;font-size:12px;
      color:var(--color-ink,#e8ebf4);background:var(--color-surface,#0b0d14);min-width:86px}
select.cell.chg,input.cell.chg{border-color:var(--color-warn,#fbbf24)}
input.cell{border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:6px;padding:2px 6px;font:inherit;font-size:12px;
      color:var(--color-ink,#e8ebf4);background:var(--color-surface,#0b0d14);width:70px}
.segs{display:inline-flex;border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:6px;overflow:hidden;vertical-align:middle}
.segs button{padding:2px 8px;font:inherit;font-size:11px;color:var(--color-ink-muted,#8b93a7);border:0;border-right:1px solid var(--color-line,rgb(255 255 255 / 0.07));
      background:none;cursor:pointer;line-height:inherit}
.segs button:last-child{border-right:0}
.segs button.on{background:var(--color-brand,#818cf8);color:var(--color-ink,#e8ebf4)}
.segs.bad button.on{background:var(--color-bad,#f87171);color:var(--color-ink-on-accent,#0b0d14)}
.segs.ok button.on{background:var(--color-ok,#34d399);color:var(--color-ink-on-accent,#0b0d14)}
.dock{border:1px solid var(--color-brand,#818cf8);border-radius:12px;padding:12px 16px;display:flex;gap:18px;align-items:center;flex-wrap:wrap;margin:12px 0 0;flex:none;
      background:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent)}
.dock > .dim{font-size:12px;letter-spacing:.02em}
.pairs{display:grid;grid-template-columns:1fr 1fr;gap:18px;align-items:start}
table.compact th,table.compact tr.g td{position:static}
`;

const OVERLAY = `
.scrim{position:absolute;inset:0;background:color-mix(in srgb, var(--color-surface,#0b0d14) 55%, transparent);display:flex;justify-content:flex-end;z-index:10}
.scrim.mid{justify-content:center;align-items:center}
.sheet{width:440px;height:100%;border-left:1px solid var(--color-line,rgb(255 255 255 / 0.07));background:var(--color-surface-raised,#11141f);display:flex;flex-direction:column}
.dlg{width:520px;max-width:90%;border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:calc(var(--radius-panel,1rem) + 2px);
     background:var(--color-surface-raised,#11141f);display:flex;flex-direction:column}
.dh{padding:14px 18px;font-weight:600;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.db{padding:16px 18px;overflow:auto;flex:1 1 auto}
.df{padding:12px 18px;border-top:1px solid var(--color-line,rgb(255 255 255 / 0.07));display:flex;gap:8px;justify-content:flex-end}
`;

const SETUP = `
.cols3{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;align-items:start}
.cols,.cols3{flex:none}
.save{margin-top:16px;padding-top:12px;border-top:1px solid var(--color-line,rgb(255 255 255 / 0.07));display:flex;gap:8px;align-items:center;flex:none;flex-wrap:wrap}
.save .said{margin:0 0 0 8px}
.radio{display:block;margin:4px 0;font-size:13px}
.radio input{margin:0 6px 0 0;vertical-align:middle;accent-color:var(--color-brand,#818cf8)}
.radio .radio{display:inline;margin:0}
.row > .radio{margin:0 10px 0 0}
.radio b{color:var(--color-ink,#e8ebf4);font-weight:600}
input.tog{appearance:none;-webkit-appearance:none;width:34px;height:18px;border-radius:9px;background:var(--color-line-strong,rgb(255 255 255 / 0.14));
      position:relative;vertical-align:middle;margin:0 8px 0 0;cursor:pointer;flex:none}
input.tog::after{content:"";position:absolute;top:2px;left:2px;width:14px;height:14px;border-radius:50%;background:var(--color-ink,#e8ebf4)}
input.tog:checked{background:var(--color-ok,#34d399)}
input.tog:checked::after{left:18px}
input.inline,select.inline{border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:8px;padding:6px 10px;margin:2px 4px;font:inherit;font-size:13px;
      color:var(--color-ink,#e8ebf4);background:var(--color-surface,#0b0d14);min-width:60px;width:auto;vertical-align:middle}
input.inline[type=number]{width:72px}
input.inline[type=time]{width:112px}
.card .mono.aside{margin-top:8px}
.count{font-size:12px;color:var(--color-ink-faint,#5a6172);padding:10px 0 0;flex:none}
.dlg-note{border:1px solid var(--color-brand,#818cf8);border-radius:12px;padding:16px;margin-bottom:16px;flex:none;
      background:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent)}
.dlg-note .sect,.sect.first{margin-top:0}
.card.accent{border-color:var(--color-brand,#818cf8);background:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent)}
.card.accent > h3{font-size:11px;font-weight:400;letter-spacing:.12em;text-transform:uppercase;color:var(--color-ink-faint,#5a6172)}
.tabline{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:12px;flex:none}
`;
