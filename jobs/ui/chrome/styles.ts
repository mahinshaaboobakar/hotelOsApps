/**
 * The module's chrome, as a stylesheet — the current era: one window, top
 * tabs, no left rail (mockup 01, the intro). Every colour is a `var()` on a
 * token the shell publishes (ADR 0106); the fallbacks are for the capture
 * harness only and match the approved dark frames.
 */

import { FAILURE_CSS } from "./failure";

/** One `<style>` holding the chrome, the failure surface and the screens' own sheets. */
export function stylesheet(parts: readonly string[] = []): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [CHROME, FAILURE_CSS, ...parts].join("\n");
  return style;
}

const CHROME = `
::-webkit-scrollbar{width:6px;height:6px}
::-webkit-scrollbar-track{background:transparent}
::-webkit-scrollbar-thumb{background:color-mix(in srgb, var(--color-ink-faint) 60%, transparent);border-radius:3px}
.jb{height:100vh;display:flex;flex-direction:column;min-width:0;
    background:var(--color-surface,#0b0d14);color:var(--color-ink,#e8ebf4);
    font:14px/1.5 var(--font-sans,system-ui, -apple-system, "Segoe UI", sans-serif);font-variant-numeric:tabular-nums;
    /* Standard §2 (C2): the primary fill is "135deg ... written once, as --accent,
       and derived — never a literal". It was written out twice, on the mark and
       on .btn.pri; an app-local name derived from published tokens, which P1
       allows. */
    --accent:linear-gradient(135deg, var(--color-brand,#818cf8),
             color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171)))}
.head{display:flex;align-items:center;gap:22px;padding:0 22px;height:56px;flex:none;
      border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.app{display:flex;align-items:center;gap:10px;font-weight:600;margin-right:14px}
.mark{width:22px;height:22px;border-radius:6px;display:grid;place-items:center;font-size:12px;
      color:var(--color-ink-on-accent,#0b0d14);background:var(--accent)}
.tab{background:none;border:0;border-bottom:2px solid transparent;color:var(--color-ink-muted,#8b93a7);
     padding:19px 2px;font:inherit;line-height:inherit;font-size:13px;cursor:pointer}
.tab.on{color:var(--color-ink,#e8ebf4);border-bottom-color:var(--color-brand,#818cf8)}
.who{color:var(--color-ink-muted,#8b93a7);font-size:12px}
.who .unset{font-style:italic}
/* A row's opener — standard §2's reset on the class (checklist C8): a real
   button that draws as the text it replaces, so the number reads as before and
   a keyboard reaches it. */
.opener{background:none;border:0;padding:0;margin:0;font:inherit;line-height:inherit;
        color:inherit;text-align:left;cursor:pointer}
/* APPS-Q50: the button's hit area is stretched over its row, so a person can
   press anywhere on it; the row is the containing block, and never the control. */
.opener::after{content:"";position:absolute;inset:0}
.body{padding:22px;overflow:auto;min-height:0}
.subnav{display:flex;gap:4px;margin-bottom:16px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
.subnav .tab{padding:8px 12px;font-size:12px;margin-bottom:-1px}
.strip{display:flex;gap:26px;font-size:12px;color:var(--color-ink-muted,#8b93a7);padding:8px 12px;margin-bottom:12px;
       border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:8px}
.strip b{color:var(--color-ink,#e8ebf4);font-size:14px;margin-right:4px}
.strip .end{margin-left:auto}
.chips{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-bottom:12px}
/* A filter chip and a pager button are .btn, modified — standard §2's one
   control vocabulary. They used to re-declare background, border, font and
   cursor for themselves, which is a second geometry however carefully it is
   copied: the day .btn changes, they do not. (No backticks in this file: the
   sheet is a template literal, and one would end it mid-rule.) */
.btn.chip{border-radius:8px;padding:6px 10px;
      font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.btn.chip.on{border-color:var(--color-brand,#818cf8);color:var(--color-ink,#e8ebf4)}
.grow{margin-left:auto}
.btn{background:none;border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:8px;padding:7px 14px;
     font:inherit;line-height:inherit;font-size:13px;color:var(--color-ink,#e8ebf4);cursor:pointer;text-align:start}
.btn.pri{border-color:transparent;color:var(--color-ink-on-accent,#0b0d14);text-align:start;background:var(--accent)}
.btn.off{color:var(--color-ink-faint,#5a6172);border-style:dashed}
/* An unavailable PRIMARY — standard §2, C11: a primary with nothing to send is
   drawn off, with the reason beside it. .btn.off alone left the brand gradient
   under a primary, so an off primary still looked like the one thing to press. */
.btn.pri.off{background:none;border-color:var(--color-line-strong,rgb(255 255 255 / 0.14));cursor:default}
/* **Destructive: outline inline, filled at the confirm** — standard §2, amended
   2026-09-04 on GG's finding. Conforming to the ordinary outline made the most
   consequential control on the screen the quietest; filling every one of them
   would shout on a screen where deletion is one affordance among many. One base
   class modified, and the pair lives here rather than in a screen, because a
   confirm that looks different on two screens teaches a person two things. */
.btn.danger{color:var(--color-bad,#f87171);
            border-color:color-mix(in srgb, var(--color-bad,#f87171) 45%, transparent)}
.btn.danger.confirm{border-color:transparent;background:var(--color-bad,#f87171);
                    color:var(--color-ink-on-accent,#0b0d14);font-weight:600}
.btn.sm{padding:2px 8px;font-size:11px}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.row.act{margin:10px 0 14px}
.stack>*+*{margin-top:14px}
.cols+.cols,.cols3+.cols,.cols+table{margin-top:14px}
table{width:100%;border-collapse:collapse;font-size:13px}
th{text-align:left;color:var(--color-ink-faint,#5a6172);font-weight:500;font-size:11px;letter-spacing:.08em;
   text-transform:uppercase;padding:8px 10px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07))}
td{padding:10px;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));vertical-align:top}
tr.pick{cursor:pointer;position:relative}
/* The row you came back from, tinted as frame 1 tints it — a person returning
   to the board should not have to find their place again. Mixed from the
   published brand rather than written as a colour: a module may not invent
   one. Measured finding, 2026-09-05. */
tr.sel td{background:color-mix(in srgb, var(--color-brand,#818cf8) 8%, transparent)}
.num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7);white-space:nowrap}
/* D3 — text that explains is 12px on 19.8px (64a, APPS-Q35). .mono and .hint
   carry Jobs' explanatory sentences; their line-height inherited 18px. */
.mono{font-family:ui-monospace,Menlo,monospace;font-size:12px;line-height:19.8px;color:var(--color-ink-muted,#8b93a7)}
/* D4 — quiet text a person still reads is --color-ink-muted (64a); this was faint. */
.dim{color:var(--color-ink-muted,#8b93a7)}
table+.mono,.kv+.mono{margin-top:8px}
/* Four classes the build emitted that no rule defined and no frame ever drew —
   found 2026-09-17 by tests/styled.test.ts, written after FF's GuestOps finding.
   They are the build's own names, so there is no drawing to derive them from;
   each takes the treatment its neighbours already use rather than a new one.

   .main held its layout in inline styles, which is the same defect from the
   other side: the rule was empty because the element carried its own. */
.main{display:flex;flex-direction:column;min-height:0;flex:1 1 auto}
/* The body fills the main area, so a list inside it has a floor to reach.
   .main took the column's free height only from 2026-09-19: until then it was
   as tall as its content — 250px of a 900px window on an empty Board — so no
   rule below could put the pager anywhere but straight under the headings. */
.main > .body{flex:1 1 auto}
.hint{margin-top:4px;font-size:12px;line-height:19.8px}
.title{font-weight:600;color:var(--color-ink,#e8ebf4)}
/* The pager is the list's floor, and ONLY THE LIST SCROLLS — the standard's
   §6 as it stands: the owner's rulings of 2026-09-05 (the list takes the free
   space; the pager draws even on one page) and CORE-Q28 of 2026-09-09, which
   "replaces the mechanism below, not its intent". Page 64's snippet, applied:

     .body:has(.pager){overflow:hidden}
     .tbl:has(~ .pager){flex:1 1 auto;min-height:0;overflow-y:auto; … }
     .pager{flex:0 0 auto}          no sticky: nothing scrolls past it now

   The list is a WRAPPER (.tbl) round the table, not the table: a table cannot
   be a scroll container, so overflow on it does nothing and the rows render
   through. min-height:0 is back and is the point — with overflow-y:auto the box
   shrinks and its rows scroll INSIDE it; page 64 records that deleting it on
   the strength of the older paragraph makes the list push the pager off the
   frame. Scoped to a body that has a pager, as page 64 measured it must be.

   SUPERSEDED, kept so the change can be read: this block said "the pager
   sticks so a full page does not hide it behind a scroll", with the body as
   the scroll container, position:sticky;bottom:0 and negative margins. It was
   written 2026-09-05 and never moved to CORE-Q28 — and on an empty Board it
   drew the pager straight under the headings with the notice's first line
   under the sticky strip (owner's screenshot, 2026-09-19 12:07). */
.body:has(.pager){display:flex;flex-direction:column;overflow:hidden}
/* A paged list that sits one level in — Settings' Policies, inside its tab —
   is its own growing column, so the free height reaches the list. Page 64's
   selector is .body:has(.pager), a descendant; this read > .pager, a direct
   child, and so never reached Policies, whose pager sat under its last row. */
.list{display:flex;flex-direction:column;flex:1 1 auto;min-height:0}
.tbl:has(~ .pager){flex:1 1 auto;min-height:0;overflow-y:auto;margin:0 -22px;padding:0 22px}
.pager{display:flex;justify-content:space-between;align-items:center;font-size:12px;flex:0 0 auto;
       color:var(--color-ink-faint,#5a6172);padding-top:10px}
.btn.pg{border-radius:6px;padding:2px 8px;margin-left:4px;
     font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.btn.pg.on{color:var(--color-ink,#e8ebf4);border-color:var(--color-brand,#818cf8)}
.btn.pg[disabled]{color:var(--color-ink-faint,#5a6172);cursor:default;opacity:.5}
.pg-gap{margin-left:4px;font-size:12px;color:var(--color-ink-faint,#5a6172)}
.cols{display:grid;grid-template-columns:1fr 1fr;gap:18px}
.cols3{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}
.card{border:1px solid var(--color-line,rgb(255 255 255 / 0.07));border-radius:var(--radius-panel,1rem);padding:16px;
      background:var(--color-surface-raised,#11141f)}
.card h3{margin:0 0 10px;font-size:13px;font-weight:600;display:flex;gap:8px;align-items:center}
.kv{display:grid;grid-template-columns:150px 1fr;gap:6px 14px;font-size:13px}
.kv .k{color:var(--color-ink-faint,#5a6172)}
/* Standard §10's five values, adopted 2026-09-09 on the owner's ruling that the
   written standard governs (APPS-Q27, reversing the planner's park). This box
   drew radius 8, 8px padding, --color-line and the surface ground until then —
   what mockups 01 and 02 draw, and what Part A measured. The mockups are the
   stale side now and are amended to match; the certificate is re-run. */
.field{border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:10px;padding:9px 12px;font-size:13px;
       background:color-mix(in srgb, var(--color-ink,#e8ebf4) 2%, transparent);
       color:var(--color-ink,#e8ebf4);margin:4px 0 12px;display:flex;gap:8px}
.field.ph{color:var(--color-ink-faint,#5a6172)}
/* A field a person types into is the same field, drawn: same border, same
   ground, same size — so a form that acts looks like the form that was
   approved rather than like the browser's idea of one. */
input.field,select.field,textarea.field{width:100%;box-sizing:border-box;display:block;font:inherit;line-height:inherit;font-size:13px;
       appearance:none;outline:none}
input.field:focus,select.field:focus,textarea.field:focus{border-color:var(--color-brand,#818cf8)}
textarea.field{resize:vertical;min-height:64px}
input.tog{width:16px;height:16px;appearance:auto;margin:0 8px 0 0;accent-color:var(--color-brand,#818cf8);position:static}
.said{margin:8px 0 0;font-size:12px}
.said.bad{color:var(--color-bad,#f87171)}
.said.ok{color:var(--color-ok,#34d399)}
.ask{border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:var(--radius-panel,1rem);
     padding:14px 16px;margin:10px 0 14px;background:var(--color-surface-raised,#11141f)}
.row>.field{margin:0}
.tl+.field{margin-top:12px}
label.lbl{font-size:11px;color:var(--color-ink-faint,#5a6172);letter-spacing:.07em;text-transform:uppercase;display:block}
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
/* The failure surface used to be written out here, and that was the defect:
   this sheet dresses the module realm, the six widgets are their own realm with
   their own sheet, and both draw the same surface. Its rules live with the
   surface now, in chrome/failure.ts, and both sheets take them from there.

   Removed with them: p.lede, which sat on the front of .gap's rule, welded to
   it by a comment that came between the selector and the brace. Nothing emits
   lede and nothing else defines it, so one rule dressed a class that does not
   exist and quietly widened to the one that does. It is FF's finding inverted —
   a class with no rule there, a rule with no class here — and both are
   invisible to a fidelity sweep, which compares two renderings and cannot see a
   selector that matches nothing. */
.note{border-left:3px solid var(--color-brand,#818cf8);padding:10px 16px;color:var(--color-ink-muted,#8b93a7);font-size:12px;line-height:19.8px;
      background:color-mix(in srgb, var(--color-brand,#818cf8) 5%, transparent)}
.note b{color:var(--color-ink,#e8ebf4)}
.stars{font-size:26px;letter-spacing:4px;color:var(--color-warn,#fbbf24)}
.sect{font-size:11px;letter-spacing:.04em;text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin:18px 0 8px}
`;
