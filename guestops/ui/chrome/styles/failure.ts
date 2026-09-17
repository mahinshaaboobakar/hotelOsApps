/**
 * When a screen cannot read — page 64 §13, ruled 2026-09-17, drawn in `64b`.
 *
 * **These rules did not exist.** `failed()` has emitted `.fail`, `.fh`, `.fb`
 * and `.fw` since it replaced the stand-in banner, and no style file defined any
 * of them — so every failure this application has ever shown a property was
 * unstyled text on the module's ground. That is what the owner saw and called
 * not good: the content was right and there was no design to object to.
 *
 * The shape is the same for all three states, because a person should learn it
 * once. What differs is the mark's colour, the words, and **the affordance** —
 * a retry only where waiting could work.
 */
export const FAILURE = `
.fail{max-width:560px;padding:34px 0;margin:0 auto}

/* The mark, drawn from the SDK's geometry. Its colour is the state's and is set
   per cause below, so the three are distinguishable before a word is read. */
.fg{width:26px;height:26px;display:block;margin-bottom:12px;color:var(--color-ink-muted,#8b93a7)}
.fail.wait .fg{color:var(--color-warn,#fbbf24)}
.fail.fault .fg{color:var(--color-bad,#f87171)}

/* The state, in two or three words. Quiet, because the sentence under it is what
   a person actually reads. */
.fl{font-size:11px;letter-spacing:.1em;text-transform:uppercase;
  color:var(--color-ink-faint,#5a6172);margin-bottom:6px}

.fh{font-size:19px;font-weight:600;letter-spacing:-.01em;line-height:1.4;margin-bottom:8px}
.fb{color:var(--color-ink-muted,#8b93a7);font-size:14px;max-width:52ch;margin-bottom:18px}

/* What to do, and what to say beside it — the loudest thing after the sentence.
   A refusal has no control here and the note carries it alone, which is the
   whole distinction between waiting and needing a grant. */
.fd{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-bottom:22px}
.fn{color:var(--color-ink-muted,#8b93a7);font-size:13px}

/* The four facts. A grid, never a dotted line: the same content set as a log
   entry is what made this surface unreadable in the first place. */
.fp{border-top:1px solid var(--color-line,rgb(255 255 255/.07));padding-top:13px;margin:0;
  display:grid;grid-template-columns:auto 1fr;gap:3px 16px;font-size:12px}
.fp dt{font-size:10.5px;letter-spacing:.08em;text-transform:uppercase;
  color:var(--color-ink-faint,#5a6172)}
.fp dd{margin:0;color:var(--color-ink-muted,#8b93a7);font-variant-numeric:tabular-nums}
`;
