/** The Leave & Requests screen's own rules — balances, the queue, the swap card. */
export const LEAVE_CSS = `

.bals{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}
.bal{background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line,rgb(255 255 255/.07));
     border-radius:var(--radius-panel,1rem);padding:12px 14px}
.bal b{font-size:20px;font-weight:600;letter-spacing:-.02em}
/* A negative balance is a fact, not a failure — WF-Q5. It is coloured as a
   warning rather than a refusal, because somebody already approved it.

   Named "negative" and not "over": every screen's rules land in ONE stylesheet,
   so a class name is module-wide. ".bal.over" collided with the dialog's
   full-screen scrim, and the Earned card became an overlay that dimmed the
   whole screen — visible in a capture, invisible everywhere else.
   (And no backticks in here: this CSS lives in a template literal.) */
.bal.negative b{color:var(--color-warn,#fbbf24)}
.bal div{font-size:12px;color:var(--color-ink-muted,#8b93a7);margin-top:2px}
.bal s{text-decoration:none;display:block;font-size:11px;
       color:var(--color-ink-faint,#5a6172);margin-top:3px}

/* The queue beside the decision. The detail is what somebody came to read,
   so it takes the wider column. */
.asplit{display:grid;grid-template-columns:minmax(0,0.85fr) minmax(0,1.15fr);gap:14px;
        align-items:start;min-height:0}
.swap{background:var(--color-surface-raised,#11141f);
      border:1px solid var(--color-line,rgb(255 255 255/.07));
      border-radius:var(--radius-panel,1rem);padding:16px;display:flex;
      flex-direction:column;gap:12px}
.steps{display:flex;gap:8px;align-items:center;font-size:11.5px;
       color:var(--color-ink-faint,#5a6172)}
.steps em{font-style:normal;padding:2px 9px;border-radius:99px;
          background:color-mix(in srgb, var(--color-ok) 13%, transparent);
          color:var(--color-ok,#34d399);font-weight:600}
.steps em.now{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
              color:var(--color-brand,#818cf8)}
.pair{display:grid;grid-template-columns:1fr 40px 1fr;gap:10px;align-items:center}
.side{display:flex;flex-direction:column;gap:4px}
.side u{text-decoration:none;font-size:13px;font-weight:600}
.side s{text-decoration:none;font-size:11.5px;color:var(--color-ink-faint,#5a6172)}
.move{display:flex;gap:8px;align-items:center;font-size:12.5px;
      color:var(--color-ink-muted,#8b93a7);margin-top:4px}
.arrow{display:grid;place-items:center;font-size:16px;color:var(--color-ink-faint,#5a6172)}

.warnrow{display:flex;gap:8px;align-items:flex-start;padding:8px 10px;border-radius:8px;
         font-size:12px;line-height:1.6;
         background:color-mix(in srgb, var(--color-warn) 10%, transparent);
         color:var(--color-warn,#fbbf24)}

.after{border-top:1px solid var(--color-line,rgb(255 255 255/.07));padding-top:12px}
.agrid{display:grid;grid-template-columns:130px repeat(4,1fr);gap:6px;align-items:center}
.alab{font-size:11.5px;color:var(--color-ink-muted,#8b93a7)}
.acell{padding:6px 9px;border-radius:8px;font-size:12px;
       background:var(--color-surface,#0b0d14);
       border:1px solid var(--color-line,rgb(255 255 255/.07))}
.acell.dim{color:var(--color-ink-faint,#5a6172);background:transparent;border-style:dashed}
`;