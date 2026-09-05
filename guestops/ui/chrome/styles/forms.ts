/**
 * Fields — the label, the box and the two-up row a sheet is built from.
 */

/**
 * The drawing's form vocabulary, on published names.
 *
 * `.inp` is drawn as a box and is **not** an `<input>`: every field in the
 * approved frames shows a value the desk has already chosen, and the frames
 * carry no keyboard state, no validation and no focus ring. Drawing it as a
 * control would put an editable box on screen that accepts typing and saves
 * nothing — a screen that lies about what it does. What the frames draw is a
 * value with an affordance beside it, and that is what this styles.
 *
 * The field wash is `--color-ink` at 2%, which is the drawing's
 * `rgba(255,255,255,.02)` expressed so it follows the theme: in a light
 * property the ink is dark and the field reads as a slightly recessed box
 * rather than as a dark-theme decision frozen into the module (§1).
 */
export const FORMS = `
.go{--go-field:color-mix(in srgb, var(--color-ink,#e8ebf4) 2%, transparent)}
.fld{display:flex;flex-direction:column;gap:6px}
.fld label{font-size:11px;text-transform:uppercase;letter-spacing:.07em;
  color:var(--color-ink-faint,#5a6172)}
.inp{display:flex;align-items:center;gap:8px;padding:9px 12px;font-size:13px;
  border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));border-radius:10px;
  color:var(--color-ink,#e8ebf4);background:var(--go-field)}
/* A value nobody has supplied yet, drawn as the prompt it is. */
.inp.ph{color:var(--color-ink-faint,#5a6172)}
/* ...and the prompt inside a box that also holds something else. */
.inp .ph{color:var(--color-ink-faint,#5a6172)}
.inp .grow{margin-left:auto;color:var(--color-ink-faint,#5a6172);font-size:11.5px}
.inp b{font-weight:600}
/* A field holding prose rather than a value — frame 5's detail box. */
.inp.tall{min-height:64px;align-items:flex-start}
.row2{display:grid;grid-template-columns:1fr 1fr;gap:10px}
/* The filter row — frame 2. The search takes twice the width of a select,
   because what a guest at the counter says is longer than any of the three
   things being filtered on. */
.fltr{display:flex;gap:10px;align-items:center}
.fltr .inp{flex:1;min-width:0}
.fltr .inp.q{flex:2}
`;
