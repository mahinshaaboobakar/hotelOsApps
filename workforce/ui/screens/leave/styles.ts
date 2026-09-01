/** The Leave & Requests screen's own rules — balances, the queue, the swap card. */
export const LEAVE_CSS = `
.tabs{display:flex;gap:4px;padding:0 24px 10px}
.tab{padding:5px 12px;border-radius:8px;font-size:12.5px;cursor:pointer;
     color:var(--color-ink-muted,#8b93a7)}
.tab.on{background:var(--color-surface-raised,#11141f);color:var(--color-ink,#e8ebf4)}
.tab s{text-decoration:none;margin-left:6px;font-size:10.5px;
       color:var(--color-ink-faint,#5a6172)}

.bals{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}
.bal{background:var(--color-surface-raised,#11141f);
     border:1px solid var(--color-line,rgb(255 255 255/.07));
     border-radius:var(--radius-panel,1rem);padding:12px 14px}
.bal b{font-size:20px;font-weight:600;letter-spacing:-.02em}
/* A negative balance is a fact, not a failure — WF-Q5. It is coloured as a
   warning rather than a refusal, because somebody already approved it. */
.bal.over b{color:var(--color-warn,#fbbf24)}
.bal div{font-size:12px;color:var(--color-ink-muted,#8b93a7);margin-top:2px}
.bal s{text-decoration:none;display:block;font-size:11px;
       color:var(--color-ink-faint,#5a6172);margin-top:3px}

.rows{display:flex;flex-direction:column;gap:1px}
.row{display:grid;gap:12px;align-items:center;padding:10px 12px;border-radius:10px;
     background:var(--color-surface-raised,#11141f);font-size:12.5px}
.row.hd{background:transparent;font-size:11px;font-weight:600;letter-spacing:.04em;
        text-transform:uppercase;color:var(--color-ink-faint,#5a6172);padding:2px 12px}
.row b{font-weight:600}
.row s{text-decoration:none;display:block;font-size:11.5px;
       color:var(--color-ink-faint,#5a6172);margin-top:2px}

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
.move{font-size:13px;font-weight:600;color:var(--color-brand,#818cf8)}
.arrow{display:grid;place-items:center;font-size:16px;color:var(--color-ink-faint,#5a6172)}
.acts{display:flex;gap:8px;justify-content:flex-end}
`;
