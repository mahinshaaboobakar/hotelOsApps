/**
 * The Team Rota's own rules — the week grid, the chips and the duty ribbon.
 *
 * Its own file because it is the rota's design and nothing else's: the chrome
 * stylesheet is what every screen shares, and a screen's rules living there
 * would make it the file where anything visual lands.
 *
 * Published token names only, and a fallback on each — `SHELL-Q30`.
 */

export const ROTA_CSS = `
.rota{display:flex;flex-direction:column;gap:10px;min-width:0}

.rgrid{display:grid;grid-template-columns:230px repeat(7,1fr);gap:6px;min-width:0}
.rhd{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
     color:var(--color-ink-faint,#5a6172);padding:2px 0 4px}

.who{display:flex;gap:10px;align-items:center;min-width:0;padding:6px 0}
.av{width:26px;height:26px;border-radius:99px;display:grid;place-items:center;flex:0 0 auto;
    font-size:10.5px;font-weight:600;
    background:var(--color-surface-raised,#11141f);color:var(--color-ink-muted,#8b93a7);
    border:1px solid var(--color-line,rgb(255 255 255/.07))}
.wn{font-size:13px;display:flex;gap:6px;align-items:center;min-width:0}
.wn em{font-style:normal;font-size:10px;font-weight:600;padding:1px 6px;border-radius:99px;
       background:color-mix(in srgb, var(--color-warn) 13%, transparent);color:var(--color-warn,#fbbf24)}
.wr{font-size:11.5px;color:var(--color-ink-faint,#5a6172)}

/* A chip is data: the colour and the code are the catalogue's, never derived. */
.chip{border-radius:9px;padding:7px 8px;min-height:44px;
      display:flex;flex-direction:column;gap:2px;justify-content:center;
      border:1px solid var(--color-line,rgb(255 255 255/.07));
      background:var(--color-surface-raised,#11141f);cursor:pointer}
.chip b{font-size:12.5px;font-weight:600;letter-spacing:.02em}
.chip i{font-style:normal;font-size:10.5px;color:var(--color-ink-faint,#5a6172)}
.chip.brand{background:color-mix(in srgb, var(--color-brand) 13%, transparent);
            border-color:var(--color-brand,#818cf8);color:var(--color-brand,#818cf8)}
.chip.ok{background:color-mix(in srgb, var(--color-ok) 13%, transparent);color:var(--color-ok,#34d399)}
.chip.warn{background:color-mix(in srgb, var(--color-warn) 13%, transparent);color:var(--color-warn,#fbbf24)}
.chip.neutral{color:var(--color-ink-faint,#5a6172)}

/* The one-off span rides ON the chip — WF-Q17: a different fact, anchored to a
   catalogue entry, never a catalogue-less cell that has no colour or code. */
.chip u{text-decoration:none;font-size:10px;font-weight:600;
        color:var(--color-ink-on-accent,#0b0d14);
        background:var(--color-warn,#fbbf24);border-radius:4px;padding:0 4px;width:fit-content}

.away{border-radius:9px;min-height:44px;display:grid;place-items:center;
      font-size:11.5px;font-weight:600;
      background:color-mix(in srgb, var(--color-bad) 13%, transparent);color:var(--color-bad,#f87171)}
.gap{border-radius:9px;min-height:44px;display:grid;place-items:center;
     font-size:11px;cursor:pointer;
     border:1px dashed var(--color-warn,#fbbf24);color:var(--color-warn,#fbbf24);
     background:color-mix(in srgb, var(--color-warn) 13%, transparent)}
.empty{border-radius:9px;min-height:44px;display:grid;place-items:center;
       font-size:14px;cursor:pointer;
       border:1px dashed var(--color-line-strong,rgb(255 255 255/.14));
       color:var(--color-ink-faint,#5a6172)}

/* The MOD ribbon is a timeline, because a duty running 20:00→08:00 covers two
   dates and fits in no day cell — WF-Q8. */
.ribbon{display:grid;grid-template-columns:230px 1fr;gap:6px;align-items:center}
.rlab{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
      color:var(--color-warn,#fbbf24)}
.bars{position:relative;height:30px;border-radius:9px;
      background:var(--color-surface-raised,#11141f);
      border:1px solid var(--color-line,rgb(255 255 255/.07))}
.bar{position:absolute;top:3px;bottom:3px;border-radius:6px;display:flex;gap:5px;
     align-items:center;padding:0 8px;font-size:11px;white-space:nowrap;overflow:hidden;
     background:color-mix(in srgb, var(--color-warn) 13%, transparent);color:var(--color-warn,#fbbf24)}
.bar s{text-decoration:none;color:var(--color-ink-faint,#5a6172);font-size:10px}
.bar.none{background:transparent;color:var(--color-ink-faint,#5a6172);
          border:1px dashed var(--color-line-strong,rgb(255 255 255/.14))}

.pick{width:min(420px,100%);display:flex;flex-direction:column;gap:10px;padding:16px;
      background:var(--color-surface-raised,#11141f);
      border:1px solid var(--color-line-strong,rgb(255 255 255/.14));
      border-radius:var(--radius-panel,1rem)}
.picks{display:flex;flex-direction:column;gap:2px}
.pk{display:flex;gap:10px;align-items:center;padding:7px 9px;border-radius:8px;
    font-size:12.5px;cursor:pointer}
.pk:hover{background:var(--color-surface,#0b0d14)}
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
.custom{display:flex;gap:10px;align-items:baseline;padding:8px 9px;border-radius:8px;
        cursor:pointer;border:1px dashed var(--color-line-strong,rgb(255 255 255/.14))}
.custom b{font-size:12.5px;font-weight:600}
.custom s{text-decoration:none;font-size:11.5px;color:var(--color-ink-faint,#5a6172)}
`;