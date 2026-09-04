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

/* A colour the published names do not carry is DERIVED, never declared — the
   accent's stops are the ones Jobs and GuestOps reached independently, down to
   the 62%, at the settled 135 degrees. A derived colour still follows the
   theme; a hex stops following it the day the theme changes.

   **On the root, not on .wf.** It was declared on the module's own frame, and
   the printed week REPLACES that frame with a sheet — so on the one screen
   without a .wf, every .btn.pri lost its ground and drew dark text on
   nothing. The Print button was in the DOM, 72px wide, and invisible. A derived
   value belongs where the whole realm can see it. */
:root{--accent:linear-gradient(135deg, var(--color-brand,#818cf8),
                               color-mix(in srgb, var(--color-brand,#818cf8) 62%,
                                         var(--color-bad,#f87171)))}
.wf{height:100vh;display:flex;flex-direction:column;
    background:var(--color-surface,#0b0d14);color:var(--color-ink,#e8ebf4);
    font:13.5px/1.55 var(--font-sans,"Segoe UI",system-ui,sans-serif);
    font-variant-numeric:tabular-nums}

/* # The app bar — an installed application navigates from the top
   The platform's own four keep the left rail, because they are the desktop's
   own furniture. An installed application is a guest in that shell, and a guest
   that draws its own 240px rail competes with the desktop's chrome for the same
   edge of the same screen. Jobs' geometry: 56px, one bottom rule, the mark and
   name first, sections as tabs with a 2px brand underline, the person right. */
.head{display:flex;align-items:center;gap:22px;padding:0 22px;height:56px;
      border-bottom:1px solid var(--color-line,rgb(255 255 255/.07));flex:0 0 auto}
.app{display:flex;gap:10px;align-items:center;font-weight:600;margin-right:14px}
.mark{width:22px;height:22px;border-radius:6px;background:var(--accent);
      display:grid;place-items:center;font-size:11px;font-weight:700;
      color:var(--color-ink-on-accent,#0b0d14)}
/* Scoped to the bar, because .tab also means the body's view switcher.
   Specificity is what keeps the two meanings apart — the alternative was two
   names for one idea, which is how a vocabulary splits. */
.head .tab{display:flex;align-items:center;gap:7px;padding:19px 2px;font-size:13px;
           color:var(--color-ink-muted,#8b93a7);border-bottom:2px solid transparent;
           font-weight:400;cursor:pointer;background:none}
.head .tab.on{color:var(--color-ink,#e8ebf4);border-bottom-color:var(--color-brand,#818cf8)}
.head .tab .n{font-size:11px;color:var(--color-ink-faint,#5a6172)}
.who{margin-left:auto;color:var(--color-ink-faint,#5a6172);font-size:12px;white-space:nowrap}

/* The body's view switcher — the second of the two levels. The bar carries
   sections; a choice WITHIN one stays here. */
.tabs{display:flex;gap:4px;border-bottom:1px solid var(--color-line,rgb(255 255 255/.07));
      padding:0 26px;flex:0 0 auto}
.tab{padding:8px 4px;margin-right:18px;color:var(--color-ink-muted,#8b93a7);font-size:13px;
     border-bottom:2px solid transparent;cursor:pointer;background:none}
.tab.on{color:var(--color-ink,#e8ebf4);border-color:var(--color-brand,#818cf8)}
.tab .cnt{margin-left:6px;font-size:10.5px;padding:1px 7px;border-radius:99px;
          background:color-mix(in srgb, var(--color-brand) 18%, transparent);
          color:var(--color-brand,#818cf8)}

.main{position:relative;overflow:hidden;display:flex;flex-direction:column;min-width:0;
      flex:1 1 auto}
/* Not a title row. A screen does not print its own section name — the bar
   already says it — so what survives is the context line and the screen's
   controls, which is the row Jobs draws above its table. */
.tools{display:flex;align-items:center;gap:10px;padding:22px 26px 0}
/* Kept for the one screen that still names something the bar cannot say. */
.title{display:flex;align-items:center;gap:10px;padding:22px 26px 14px}
.ht{font-size:19px;font-weight:600;letter-spacing:-.01em}
.hsub{color:var(--color-ink-faint,#5a6172);font-size:12px;margin-top:2px}
.grow{margin-left:auto}
/* Padded on all four sides. It was 0 24px 20px — a deliberate zero on top,
   because the title row above supplied it — so removing that row leaves the
   first element flush against the bar's rule. */
.body{padding:22px 26px;overflow:auto;display:flex;flex-direction:column;gap:12px}
/* ...unless a switcher sits above and has already paid for it. */
.tabs + .main .body{padding-top:14px}

/* ONE control vocabulary, at Jobs' geometry. Not .btn2, not .create, and no
   longer .go: a second base class is a second geometry within a week, and three
   applications proved it. This was 9px / 5px 11px / 12px on ink-muted.

   font:inherit is load-bearing rather than tidy — without it a control leaves
   --font-sans and picks up the UA's, which is invisible until a capture sits
   beside a frame. */
.btn{display:inline-flex;gap:8px;align-items:center;justify-content:center;
     white-space:nowrap;cursor:pointer;background:none;
     border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
     border-radius:8px;padding:7px 14px;font:inherit;font-size:13px;
     color:var(--color-ink,#e8ebf4)}
.btn.pri{border-color:transparent;color:var(--color-ink-on-accent,#0b0d14);
         font-weight:600;background:var(--accent)}
/* Unavailable, and saying so — dashed rather than hidden, for the same reason a
   refused candidate is drawn rather than filtered out. */
.btn.off{color:var(--color-ink-faint,#5a6172);border-style:dashed;cursor:default}
.btn.sm{padding:2px 8px;font-size:11px;gap:6px}
/* The destructive twin, and it SPLITS — the standard moved on this round's own
   redline, 2026-09-04. An outline danger button sitting where a person has
   already decided to delete something is quieter than the Cancel beside it,
   which inverts the weight of the choice. So:

     inline affordance   outline   a row's remove, a cancel-this link
     the confirm step    FILLED    the button in the dialog that does it

   Filling every destructive control would shout on a screen where deletion is
   one affordance among many; leaving the confirm an outline whispers at the
   exact moment weight is wanted. This module drew the confirm filled from the
   start and the standard came to it. Both live in the chrome rather than in a
   screen, because a confirm that looked different on two screens would teach a
   person two things, and the second one they meet is the one they misread. */
.btn.danger{color:var(--color-bad,#f87171);background:none;
            border-color:color-mix(in srgb, var(--color-bad) 45%, transparent)}
.btn.danger.confirm{background:var(--color-bad,#f87171);
                    color:var(--color-ink-on-accent,#0b0d14);
                    border-color:transparent;font-weight:600}
/* The table row, used by six screens. A row that opens something is a real
   <button>, so the reset is on the class rather than on a second one: a
   button carries the UA's border, centred text and its own font family, and a
   row that acquired all three the day somebody made it clickable is a defect
   only a capture can see. */
/* A list sits BARE on the page — no wrapper, no fill, no radius, rows separated
   by a single rule. Each row carried its own raised fill and a 10px radius,
   which is a card per row: a card is for a thing you are looking at, a row is
   for one of many you are looking through, and the difference is how many fit.

   align-items:flex-start is vertical-align:top in its flex spelling, and it
   is not a detail — a first cell of two lines otherwise pushes every other cell
   in the row down past the name it belongs beside. */
.rows{display:flex;flex-direction:column}
.row{display:grid;gap:12px;align-items:flex-start;padding:10px;font-size:13px;
     border:0;border-bottom:1px solid var(--color-line,rgb(255 255 255/.07));
     background:none;width:100%;text-align:left;color:inherit;font-family:inherit}
/* The last row keeps its rule: with no card around the list, that line is what
   closes it. */
.row.hd{font-size:11px;font-weight:500;letter-spacing:.08em;align-items:center;
        text-transform:uppercase;color:var(--color-ink-faint,#5a6172);padding:8px 10px}
.row b{font-weight:600}
.row s{text-decoration:none;display:block;font-size:11.5px;
       color:var(--color-ink-faint,#5a6172);margin-top:2px}
button.row{cursor:pointer}
button.row:hover{background:color-mix(in srgb, var(--color-brand) 6%, transparent)}
button.row:focus-visible{outline:2px solid var(--color-brand,#818cf8);outline-offset:-2px}

/* # Primitives more than one screen draws
   Each of these lived in the file of whichever screen was written first, and
   the guard could not see it: a screen may reach into another screen's
   vocabulary from its MARKUP, and a check that compares stylesheets against
   stylesheets is blind to exactly that. Four screens wore the rota's column
   heading; the duty dialog wore the rota's picker; People wore the rota's name
   cell; the leave form wore Policy's four-column field row.

   A primitive several screens draw belongs to none of them. */

.rhd{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
     color:var(--color-ink-faint,#5a6172);padding:2px 0 4px}

.wn{font-size:13px;display:flex;gap:6px;align-items:center;min-width:0}
.wn em{font-style:normal;font-size:10px;font-weight:600;padding:1px 6px;border-radius:99px;
       background:color-mix(in srgb, var(--color-warn) 13%, transparent);color:var(--color-warn,#fbbf24)}

.rlab{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
      color:var(--color-warn,#fbbf24)}

.picks{display:flex;flex-direction:column;gap:2px}

.pk{display:flex;gap:10px;align-items:center;padding:7px 9px;border-radius:8px;
    font-size:12.5px;cursor:pointer}
.pk:hover{background:var(--color-surface,#0b0d14)}
.pk.on{background:color-mix(in srgb, var(--color-brand) 10%, transparent);
       box-shadow:inset 0 0 0 1px var(--color-brand,#818cf8)}
.pk s{text-decoration:none;margin-left:auto;font-size:11.5px;
      color:var(--color-ink-faint,#5a6172)}
.pk .code{min-width:34px;text-align:center;border-radius:6px;padding:2px 6px;
          font-size:11.5px;font-weight:600}
.pk .code.brand{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
                color:var(--color-brand,#818cf8)}
.pk .code.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent);
             color:var(--color-ok,#34d399)}
.pk .code.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent);
               color:var(--color-warn,#fbbf24)}
.pk .code.neutral{color:var(--color-ink-faint,#5a6172)}

/* The field row inside a dialog. Two columns by default; four where a split
   shift needs them. It was Policy's four-column grid and the leave form wore
   it for two dates, so each got a quarter of the width and one instant broke
   over two lines. The count is now said out loud at the call site. */
.spans{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.spans.four{grid-template-columns:repeat(4,1fr);gap:6px}
.spans .finput{white-space:nowrap}
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

/* The published tints, with the old local mix kept as the fallback — aliased
   rather than replaced, because a host that has not published the tones yet
   still has to render. §1, amended 2026-09-04: every application was mixing
   its own tint of the same colour, and a published one is the shell's to
   change once. */
.pill{padding:3px 11px;border-radius:99px;font-size:11px;font-weight:600;
      width:fit-content;white-space:nowrap}
.pill.ok{background:var(--color-ok-soft,color-mix(in srgb, var(--color-ok) 13%, transparent));color:var(--color-ok,#34d399)}
.pill.warn{background:var(--color-warn-soft,color-mix(in srgb, var(--color-warn) 13%, transparent));color:var(--color-warn,#fbbf24)}
.pill.bad{background:var(--color-bad-soft,color-mix(in srgb, var(--color-bad) 13%, transparent));color:var(--color-bad,#f87171)}
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
.legend .code.ok{background:var(--color-ok-soft,color-mix(in srgb, var(--color-ok) 13%, transparent));
                 color:var(--color-ok,#34d399)}
.legend .code.warn{background:var(--color-warn-soft,color-mix(in srgb, var(--color-warn) 13%, transparent));
                   color:var(--color-warn,#fbbf24)}
.legend .code.neutral{color:var(--color-ink-faint,#5a6172)}
.lnote{margin-left:auto;color:var(--color-ink-faint,#5a6172)}

/* The short code, drawn once for every screen that shows one. */
.code{display:inline-block;min-width:30px;text-align:center;border-radius:5px;
      padding:2px 6px;font-size:11.5px;font-weight:600;letter-spacing:.02em}
.code.brand{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
            color:var(--color-brand,#818cf8)}
.code.ok{background:var(--color-ok-soft,color-mix(in srgb, var(--color-ok) 13%, transparent));
         color:var(--color-ok,#34d399)}
.code.warn{background:var(--color-warn-soft,color-mix(in srgb, var(--color-warn) 13%, transparent));
           color:var(--color-warn,#fbbf24)}
.code.bad{background:var(--color-bad-soft,color-mix(in srgb, var(--color-bad) 13%, transparent));
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