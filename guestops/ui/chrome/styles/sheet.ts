/**
 * The scrim, and the two surfaces that sit on it — a side sheet and a dialog.
 */

/**
 * One overlay vocabulary, two placements.
 *
 * The approved frames use both and they mean different things: a **sheet**
 * enters from the right and is where a person *composes* something (frame 10's
 * walk-in), a **dialog** sits in the middle and is where a person *confirms*
 * something (frame 8's cancellation). Same head, body and foot; the placement
 * is the whole difference, which is why `.mid` modifies the scrim rather than
 * either surface having its own layout.
 *
 * `.dh` / `.db` / `.df` rather than the drawing's `.sh_h` / `.sh_b` / `.sh_f`:
 * in this module `.sh` already means a source mark, and a reader meeting
 * `.sh_h` beside `.sh.pms` has to work out that the two `sh`s are different
 * words. The computed values are the drawing's.
 *
 * **The scrim is `position:absolute`, so the module's root is its container.**
 * A `fixed` overlay would escape the realm's own box and cover the desktop's
 * chrome from inside an iframe — the module would be dimming a shell it does
 * not own.
 *
 * The dialog's radius is derived from the one published radius rather than
 * written as 16px: `radius-panel` is the only shape token the contract carries
 * (§1), and a literal would stop following a host that changes it.
 *
 * # The shadow is a reported contract gap, not a worked-around one
 *
 * The frames give both surfaces a black 50% shadow, and **the contract
 * publishes no shadow, no elevation and no neutral black**. A literal
 * `rgba(0,0,0,.5)` is what the token guard exists to catch: it is a dark-theme
 * decision frozen into a module that a light property will also run.
 *
 * So it is mixed from `--color-surface`, which is the only token that is
 * *behind* things by definition. In a dark property that resolves to very
 * nearly the drawn shadow. In a light one it resolves to a pale shadow that is
 * effectively invisible — the surface is then bounded by its border and its
 * scrim instead, which is a **weaker** rendering rather than a wrong one. That
 * asymmetry is the gap, and it is named here rather than papered over: a
 * published elevation token would close it in one line.
 */
export const SHEET = `
.go{--go-shadow:color-mix(in srgb, var(--color-surface,#0b0d14) 50%, transparent)}
.scrim{position:absolute;inset:0;display:flex;align-items:flex-start;justify-content:flex-end;
  background:color-mix(in srgb, var(--color-surface,#0b0d14) 55%, transparent)}
.scrim.mid{align-items:center;justify-content:center}
.sheet{width:440px;height:100%;display:flex;flex-direction:column;
  background:var(--color-surface-raised,#11141f);
  border-left:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));
  box-shadow:-30px 0 70px var(--go-shadow)}
.dlg{width:520px;display:flex;flex-direction:column;overflow:hidden;
  background:var(--color-surface-raised,#11141f);
  border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));
  border-radius:calc(var(--radius-panel,14px) + 2px);
  box-shadow:0 24px 60px var(--go-shadow)}
.dh{padding:18px 20px 12px;border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
.dh b{font-size:15.5px;font-weight:600}
.dh span{display:block;margin-top:3px;font-size:11.5px;color:var(--color-ink-faint,#5a6172)}
.db{padding:16px 20px;display:flex;flex-direction:column;gap:14px;overflow:auto}
.df{margin-top:auto;padding:14px 20px;display:flex;gap:10px;align-items:center;
  border-top:1px solid var(--color-line,rgba(255,255,255,.08))}
.df .grow{margin-left:auto}
`;
