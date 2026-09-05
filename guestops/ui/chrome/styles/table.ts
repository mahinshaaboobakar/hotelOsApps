/**
 * The lists — bare on the page, and their pager.
 */

/** The day's table — bare on the page, and its pager. */
export const TABLE = `
/* NO CARD — docs/working/64 §4. This was a filled, bordered, 14px-radius box
   with the header row filled again inside it. Jobs' board sits bare and
   separates rows with one rule, and two applications drawing a list two ways
   is more visible than two buttons, because a list is most of what an
   operator looks at. */
.tbl{font-size:13px}
.tr{display:grid;grid-template-columns:1.5fr .9fr .8fr .7fr .8fr 1.5fr;
  border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
/* The last row keeps its rule: with no card around the list, that final line
   is what closes it. */
.tr>div{padding:6px 10px;display:flex;align-items:flex-start;gap:7px;min-width:0}
.tr.hd>div{align-items:center;padding:8px 10px;font-size:11px;font-weight:500;
  text-transform:uppercase;letter-spacing:.08em;color:var(--color-ink-faint,#5a6172)}
.tr.act{cursor:pointer}
.tr.act:hover{background:var(--go-row-hover)}
.tr .nm{display:flex;flex-direction:column;gap:0;align-items:flex-start;line-height:1.25}
.tr .nm b{font-weight:600}
.tr .nm span{font-size:10.5px;color:var(--color-ink-faint,#5a6172)}
/* The LIST proportions — frames 2, 8 and 9. The day's table and the booking
   list are the same table with different columns, so this modifies the grid
   and nothing else: two \`.tr\` rules with two sets of padding is how two
   lists in one application stop lining up. */
.tr.list{grid-template-columns:1.5fr 1fr .9fr 1fr .8fr 1.3fr}
/* The row a dialog is about, still readable behind the scrim. */
.tr.sel{background:color-mix(in srgb, var(--color-brand,#818cf8) 8%, transparent)}
/* The one number on an availability row nobody stores — frame 14 sets it 15px
   and colours it by whether there is anything to sell. */
.tr .n{font-size:15px}
.tr .n.ok{color:var(--color-ok,#34d399)}
.tr .n.none{color:var(--color-ink-faint,#5a6172)}
/* The pager — §6, numbered because the wire now carries a total.
   \`ListStays\` pages on \`PagedRequest\`/\`PagedResponse\`, so an ordinal and a
   count both exist and "showing 1-25 of 47" is something the service can
   actually answer.
   It MATCHES components/design/pager.tsx rather than importing it: a hosted
   module is styled by tokens and never by importing components across a
   realm, so the match is a rendering obligation, not a dependency. */
.pager{display:flex;justify-content:space-between;align-items:center;gap:9px;
  padding:10px 4px 0;font-size:12px;color:var(--color-ink-faint,#5a6172)}
.pager .pnav{display:flex;align-items:center}
.pager .pg{display:inline-block;padding:2px 8px;margin-left:4px;border-radius:6px;
  border:1px solid var(--color-line,rgba(255,255,255,.08));font-family:inherit;
  font-size:12px;background:none;cursor:pointer;color:var(--color-ink-muted,#8b93a7)}
.pager .pg.on{color:var(--color-ink,#e8ebf4);border-color:var(--color-brand,#818cf8)}
.pager .pg[disabled]{color:var(--color-ink-faint,#5a6172);opacity:.45;cursor:default}
.pager .gap{margin-left:4px;color:var(--color-ink-faint,#5a6172)}
`;
