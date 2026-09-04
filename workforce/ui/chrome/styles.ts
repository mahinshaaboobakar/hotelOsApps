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
/* **Every control in this module is a real button**, and a button carries the
   UA's own ground, border, centred text and font family. So the reset is here,
   once, rather than on each class that a button might one day wear: three
   classes had already been drawn as divs and became buttons the day they had
   to open something, and each arrived with a light grey fill nothing in the
   stylesheet asked for. A class rule below still overrides this:
   .btn.go and .tsw set their own ground and keep it. */
button{background:transparent;color:inherit;font:inherit;border:0;
       text-align:inherit;padding:0}
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
button.btn{justify-content:center}
.btn.go{background:var(--color-brand,#818cf8);color:var(--color-ink-on-accent,#0b0d14);
        font-weight:600;border-color:transparent}
/* The destructive twin of .go. Here in the chrome rather than in a screen,
   because a confirm that looked different on two screens would teach a person
   two things — and the second one they meet is the one they misread. */
.btn.danger{background:var(--color-bad,#f87171);color:var(--color-ink-on-accent,#0b0d14);
            font-weight:600;border-color:transparent}
/* The table row, used by six screens. A row that opens something is a real
   <button>, so the reset is on the class rather than on a second one: a
   button carries the UA's border, centred text and its own font family, and a
   row that acquired all three the day somebody made it clickable is a defect
   only a capture can see. */
.rows{display:flex;flex-direction:column;gap:1px}
.row{display:grid;gap:12px;align-items:center;padding:10px 12px;border-radius:10px;
     background:var(--color-surface-raised,#11141f);font-size:12.5px;
     border:0;width:100%;text-align:left;color:inherit;font-family:inherit}
.row.hd{background:transparent;font-size:11px;font-weight:600;letter-spacing:.04em;
        text-transform:uppercase;color:var(--color-ink-faint,#5a6172);padding:2px 12px}
.row b{font-weight:600}
.row s{text-decoration:none;display:block;font-size:11.5px;
       color:var(--color-ink-faint,#5a6172);margin-top:2px}
button.row{cursor:pointer}
button.row:hover{background:color-mix(in srgb, var(--color-brand) 7%, var(--color-surface-raised))}
button.row:focus-visible{outline:2px solid var(--color-brand,#818cf8);outline-offset:-2px}

/* The dialog, and the fields inside one. Here rather than in a screen because
   three screens open one — Policy, Leave and Teams — and the shape a person
   meets on the third has to be the shape they learned on the first. */
.scrim{position:absolute;inset:0;display:grid;place-items:center;padding:24px;
      background:color-mix(in srgb, var(--color-surface) 45%, transparent);
      backdrop-filter:blur(1.5px)}
/* 440 and 22/24 are the drawing's own numbers, not a taste: every dialog in
   both mockups is one rule, and the built one was 560 wide with 20 of padding
   — near enough to read as right beside a frame, and wrong in every capture. */
.dlg{width:min(440px,100%);max-height:100%;overflow:auto;display:flex;
     flex-direction:column;gap:14px;padding:22px 24px;border-radius:16px;
     background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
     box-shadow:0 24px 60px rgb(0 0 0/.45)}
/* The right-hand sheet. A form that is a place rather than a question: it holds
   the screen's edge, keeps its full height, and does not cover the list the
   name is being checked against. Frame 3 draws Form a team this way. */
.scrim.edge{place-items:stretch;padding:10px}
.dlg.edge{width:390px;max-width:100%;margin-left:auto;border-radius:16px}
.fld{display:flex;flex-direction:column;gap:5px}
.flab{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
      color:var(--color-ink-faint,#5a6172)}
.finput{border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
        border-radius:8px;padding:7px 11px;font-size:13px;
        background:var(--color-surface,#0b0d14)}
.acts{display:flex;gap:8px;justify-content:flex-end;align-items:center}

.sel{display:flex;gap:8px;align-items:center;justify-content:space-between;
     background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line,rgb(255 255 255/.07));
     border-radius:9px;padding:5px 11px;font-size:12.5px;min-width:150px}
.sel i{color:var(--color-ink-faint,#5a6172);font-style:normal}
/* The avatar. Four screens draw one — the rota, leave, teams and the member
   picker — and it was styled inside the Rota's rules because the rota drew one
   first. A primitive several screens use is the chrome's, or the next screen
   to want one silently depends on another screen's file. */
.av{width:26px;height:26px;border-radius:99px;display:grid;place-items:center;flex:0 0 auto;
    font-size:10.5px;font-weight:600;
    background:var(--color-surface-raised,#11141f);color:var(--color-ink-muted,#8b93a7);
    border:1px solid var(--color-line,rgb(255 255 255/.07))}

/* A picker may carry an avatar. The rule lives here because the picker does:
   a screen restyling shared chrome changes it for every other screen, and the
   next one to put an avatar in a picker would inherit a size nobody chose. */
.sel .av{width:20px;height:20px;font-size:9px}

.pill{padding:3px 11px;border-radius:99px;font-size:11px;font-weight:600;
      width:fit-content;white-space:nowrap}
.pill.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent);color:var(--color-ok,#34d399)}
.pill.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent);color:var(--color-warn,#fbbf24)}
.pill.bad{background:color-mix(in srgb, var(--color-bad) 13%, transparent);color:var(--color-bad,#f87171)}
/* A TRANSLUCENT ground, never the raised surface. The raised one is the ground
   a card already has, so a neutral pill on a card renders as the plain text it
   was written to replace — the zone-chip defect, and only a capture can see
   it. The drawing has always used an overlay here; the build had not. */
.pill.neu{background:color-mix(in srgb, var(--color-ink) 6%, transparent);
          color:var(--color-ink-muted,#8b93a7)}
/* The tone a standing assignment reads in — a zone on a posting. The neutral
   tone takes its ground from the raised surface, so on a raised row it
   disappears and reads as the plain text it was written to replace. */
.pill.acc{background:color-mix(in srgb, var(--color-brand) 15%, transparent);
          color:var(--color-brand,#818cf8)}

.panel{background:var(--color-surface-raised,#11141f);
       border:1px solid var(--color-line,rgb(255 255 255/.07));
       border-radius:var(--radius-panel,1rem);padding:14px 16px}
/* A quieter run of text. Three screens use it — attendance, people, teams —
   and it was styled inside Attendance under the name dim, where Leave's own
   .acell.dim met it in the one stylesheet they share. */
.quiet{color:var(--color-ink-muted,#8b93a7)}
.note{font-size:12px;line-height:1.6;color:var(--color-ink-muted,#8b93a7)}
.note b{color:var(--color-ink,#e8ebf4)}

.legend{display:flex;gap:16px;align-items:center;flex-wrap:wrap;
        background:var(--color-surface-raised,#11141f);
        border:1px solid var(--color-line,rgb(255 255 255/.07));
        border-radius:var(--radius-panel,1rem);padding:10px 14px;font-size:11.5px}
.llab{font-size:10.5px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;
      color:var(--color-ink-faint,#5a6172)}
.lent{display:flex;gap:7px;align-items:center}
.lent s{text-decoration:none;color:var(--color-ink-faint,#5a6172)}
.legend .code{min-width:30px;text-align:center;border-radius:5px;padding:1px 6px;
              font-size:11px;font-weight:600}
.legend .code.brand{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
                    color:var(--color-brand,#818cf8)}
.legend .code.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent);
                 color:var(--color-ok,#34d399)}
.legend .code.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent);
                   color:var(--color-warn,#fbbf24)}
.legend .code.neutral{color:var(--color-ink-faint,#5a6172)}
.lnote{margin-left:auto;color:var(--color-ink-faint,#5a6172)}

/* The short code, drawn once for every screen that shows one. */
.code{display:inline-block;min-width:30px;text-align:center;border-radius:5px;
      padding:2px 6px;font-size:11.5px;font-weight:600;letter-spacing:.02em}
.code.brand{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
            color:var(--color-brand,#818cf8)}
.code.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent);
         color:var(--color-ok,#34d399)}
.code.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent);
           color:var(--color-warn,#fbbf24)}
.code.bad{background:color-mix(in srgb, var(--color-bad) 13%, transparent);
          color:var(--color-bad,#f87171)}
/* Same defect, same fix: the department chip sits on a card. */
.code.neutral{background:color-mix(in srgb, var(--color-ink) 7%, transparent);
              color:var(--color-ink-muted,#8b93a7);font-size:10.5px;font-weight:700;
              letter-spacing:.06em;padding:2px 7px;border-radius:5px;min-width:0}
.swatch-row{display:inline-flex;gap:7px;align-items:center}
.dot{width:8px;height:8px;border-radius:99px;display:inline-block;flex:0 0 auto}
.dot.brand{background:var(--color-brand,#818cf8)}
.dot.ok{background:var(--color-ok,#34d399)}
.dot.warn{background:var(--color-warn,#fbbf24)}
.dot.bad{background:var(--color-bad,#f87171)}
.dot.neutral{background:var(--color-ink-faint,#5a6172)}
`;