/**
 * The widget stylesheet — one sheet, five widgets, on published tokens.
 *
 * # Every class is prefixed `w`
 *
 * A widget bundle is its own document and never shares a stylesheet with the
 * module, so a name collision between the two is harmless *in fact* — and the
 * module's own collision guard cannot know that. Prefixing costs nothing and
 * keeps a reader from assuming `.row` means the same thing in both places,
 * which is the failure `.over` actually caused: one name, two meanings, and a
 * card that became a full-screen scrim.
 *
 * # Only published token names — `SHELL-Q30`
 *
 * The fourteen the shell injects, and a fallback on every one. A widget realm
 * is styled by whatever the host writes in; one that assumed a token was
 * present would render unreadable text on a transparent ground the first time
 * it was not.
 *
 * # The card's chrome is drawn here, and that is a question for the mechanism
 *
 * `56-app-widgets.md` says *the shell gives every widget the same frame*, and
 * the approved artboards draw a full card — surface, border, radius, shadow.
 * These rules follow the artboards, because the artboards are what the audit
 * compares against. If the popover supplies its own chrome then this draws a
 * second border inside the first, and the fix is to drop three declarations
 * here rather than to redraw anything.
 */

/** The whole sheet. */
export const WIDGET_CSS = `
*{box-sizing:border-box}
body{margin:0}

/* The card. 320x384 is the guaranteed popover, stated rather than inherited so
   the harness and the shell photograph the same rectangle. */
.wcard{width:320px;height:384px;display:flex;flex-direction:column;
       background:var(--color-surface-raised,#11141f);
       border:1px solid var(--color-line,rgb(255 255 255/.07));
       border-radius:var(--radius-panel,1rem);
       box-shadow:0 12px 32px -8px rgba(0,0,0,.32);
       overflow:hidden;
       font-family:var(--font-sans,system-ui,-apple-system,"Segoe UI",sans-serif);
       color:var(--color-ink,#e8ebf4)}

.whead{display:flex;align-items:center;gap:8px;padding:12px 14px 10px;
       border-bottom:1px solid var(--color-line,rgb(255 255 255/.07))}
.wtitle{font-size:12.5px;font-weight:600;letter-spacing:.01em}
.wapp{margin-left:auto;font-size:10.5px;color:var(--color-ink-faint,#5a6172)}

.wbody{flex-grow:1;padding:12px 14px;display:flex;flex-direction:column;gap:10px;
       overflow:hidden}

/* The headline figures. Two or four, and the grid is the same either way. */
.wfigures{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}
.wfigures.four{grid-template-columns:repeat(4,minmax(0,1fr))}
.wfigure{display:flex;flex-direction:column;gap:2px}
.wvalue{font-size:21px;font-weight:600;line-height:1.05;
        font-variant-numeric:tabular-nums}
.wlabel{font-size:10px;color:var(--color-ink-muted,#8b93a7);letter-spacing:.02em}

/* The proportion bar. Segments are sized from their counts, so the bar cannot
   disagree with the figures above it. */
.wbar{display:flex;height:8px;border-radius:4px;overflow:hidden;gap:2px}
.wbar span{opacity:.85}
.wbar .ok{background:var(--color-ok,#34d399)}
.wbar .bad{background:var(--color-bad,#f87171)}
.wbar .warn{background:var(--color-warn,#fbbf24)}
.wbar .muted{background:var(--color-ink-muted,#8b93a7)}
.wbar .ink{background:var(--color-ink,#e8ebf4)}

/* A section's name. Uppercase and small: it labels a list rather than heading
   a screen, and the frames give it no more weight than that. */
.wsection{font-size:10px;color:var(--color-ink-muted,#8b93a7);
          letter-spacing:.04em;text-transform:uppercase}

.wrows{display:flex;flex-direction:column}
/* A row is a button — the frames draw a div, which is right for a picture and
   wrong for a product: a div is not focusable, not announced and not operable
   from a keyboard. The look is the frame's; the element is a control. */
.wrow{display:flex;align-items:baseline;gap:8px;padding:7px 0;width:100%;
      background:none;border:0;border-top:1px solid var(--color-line,rgb(255 255 255/.07));
      font:inherit;text-align:left;cursor:pointer;color:inherit}
.wrow:first-child{border-top:0}
.wrow:focus-visible{outline:2px solid var(--color-brand,#818cf8);outline-offset:-2px;
                    border-radius:4px}
.wname{font-size:12px;flex-grow:1;white-space:nowrap;overflow:hidden;
       text-overflow:ellipsis}
.wmeta{font-size:11px;color:var(--color-ink-faint,#5a6172);
       font-variant-numeric:tabular-nums;white-space:nowrap}
.wfig{font-size:11.5px;font-variant-numeric:tabular-nums;min-width:46px;
      text-align:right}

/* The changeover block, which sits under a rule of its own. */
.wchange{border-top:1px solid var(--color-line,rgb(255 255 255/.07));padding-top:9px}
.wchange .wsection{margin-bottom:5px}
.wswitch{display:flex;gap:10px;font-size:11.5px}

/* The note. Pushed to the foot, because what a widget cannot answer belongs
   under what it can. */
.wnote{margin-top:auto;font-size:10px;color:var(--color-ink-faint,#5a6172);
       line-height:1.4}

/* The refusal a tap leaves behind. It exists only after a refused tap — the
   design's rule is that every element taps through, so a tap that silently
   does nothing is the one outcome worse than a tap that says why. */
.wrefusal{padding:6px 14px 10px;font-size:10px;line-height:1.4;
          color:var(--color-bad,#f87171)}

/* The tones, scoped under the card rather than written as bare classes. A tone
   class starting a line would be a module-wide name in the module's own
   collision guard, and the guard cannot know these rules live in a different
   document — the same reason the chrome writes its pill tones that way. */
.wcard .ink{color:var(--color-ink,#e8ebf4)}
.wcard .muted{color:var(--color-ink-muted,#8b93a7)}
.wcard .ok{color:var(--color-ok,#34d399)}
.wcard .warn{color:var(--color-warn,#fbbf24)}
.wcard .bad{color:var(--color-bad,#f87171)}
`;
