/** The Teams screen's own rules — the list, the split, and the two dialogs. */
export const TEAMS_CSS = `
/* A row that opens the team it names is a real button, so the reset is on the
   class — a UA border, centred text and a different font family arriving the
   day somebody made a row clickable is a defect only a capture can see. */
.tgrid{display:grid;grid-template-columns:1.4fr 120px 100px 140px 120px;gap:12px;
       align-items:flex-start;padding:11px 18px;width:100%;text-align:left;
       background:transparent;color:inherit;font-family:inherit;font-size:13px;
       border:0;border-bottom:1px solid var(--color-line,rgb(255 255 255/.07))}
/* The list, bare. The last row keeps its rule: with no card around it, that
   line is what closes the list. */
.list{display:flex;flex-direction:column}
button.tgrid,button.tnarrow{cursor:pointer}
button.tgrid:hover,button.tnarrow:hover{
  background:color-mix(in srgb, var(--color-brand) 6%, transparent)}
button.tgrid:focus-visible,button.tnarrow:focus-visible{
  outline:2px solid var(--color-brand,#818cf8);outline-offset:-2px}
.tgrid.hd{align-items:center;font-size:11px;font-weight:500;letter-spacing:.08em;
          text-transform:uppercase;color:var(--color-ink-faint,#5a6172);padding:8px 10px}
/* **A tint and nothing else** — §64 §4, which names the 2.5px inset brand bar
   as GuestOps' own invention. This module had reproduced it, twice. */
.tgrid.sel{background:color-mix(in srgb, var(--color-brand) 8%, transparent)}
/* A team stood down reads one step quieter, which is the whole difference
   between "not offered" and "gone". */
.tgrid.down{color:var(--color-ink-faint,#5a6172)}
.tgrid.down b{color:var(--color-ink-faint,#5a6172)}
.tgrid b{font-weight:600}
.tgrid s{text-decoration:none;display:block;font-size:11.5px;
         color:var(--color-ink-faint,#5a6172);margin-top:2px}

/* The list beside the detail. The list is narrower, because the detail is what
   somebody came to read. */
.tsplit{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1.15fr);gap:12px;
        align-items:start}
.tnarrow{display:grid;grid-template-columns:1fr 60px 70px;gap:10px;align-items:flex-start;
         padding:11px 16px;width:100%;text-align:left;background:transparent;
         color:inherit;font-family:inherit;font-size:13px;border:0;
         border-bottom:1px solid var(--color-line,rgb(255 255 255/.07))}
.tnarrow.hd{align-items:center;font-size:11px;font-weight:500;letter-spacing:.08em;
            text-transform:uppercase;color:var(--color-ink-faint,#5a6172);padding:8px 10px}
/* The formed date. Tabular, and one step quieter than the name beside it. */
.tm{font-variant-numeric:tabular-nums;color:var(--color-ink-muted,#8b93a7)}
.tnarrow.sel{background:color-mix(in srgb, var(--color-brand) 8%, transparent)}
.tnarrow.down b{color:var(--color-ink-faint,#5a6172)}

.tdetail{display:flex;flex-direction:column;gap:11px}
.thead{display:flex;align-items:center;gap:8px}
.thead b{font-size:15px;font-weight:600}
.thead s{text-decoration:none;display:block;font-size:11.5px;
         color:var(--color-ink-faint,#5a6172);margin-top:2px}
.tkv{display:flex;justify-content:space-between;align-items:center;font-size:12.5px;
     padding:7px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255/.07))}
.tkv em{font-style:normal;color:var(--color-ink-muted,#8b93a7)}
.tsec{font-size:11px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;
      color:var(--color-ink-muted,#8b93a7)}

.tmem{display:flex;gap:10px;align-items:center;padding:8px 10px;border-radius:9px;
      border:1px solid var(--color-line,rgb(255 255 255/.07))}
.tmem b{font-weight:600;font-size:13px}
.tmem s{text-decoration:none;display:block;font-size:11px;
        color:var(--color-ink-faint,#5a6172)}
/* Somebody the picker may not add. Dashed rather than hidden — a supervisor who
   cannot see them wonders where they went. */
.tmem.no{opacity:.55;border-style:dashed}
/* on, not pick: the Rota owns .pick for its 420px column panel, and every
   screen's rules compose into ONE stylesheet — so this row inherited
   flex-direction:column and drew its avatar centred above the name. Found by a
   capture; the drawing's own word for a chosen row is on. */
.tmem.on{border-color:var(--color-brand,#818cf8);
         background:color-mix(in srgb, var(--color-brand) 9%, transparent)}
.tlist{display:flex;flex-direction:column;gap:6px}

/* A note that is a refusal rather than an explanation. The shared note is the
   quiet voice; this is the same shape in the warn tone, so the two read as one
   family and a person can still tell them apart. */
.twarn{color:var(--color-warn,#fbbf24)}
.twarn b{color:var(--color-warn,#fbbf24)}


/* Frame 5's toggle. */
.tog{display:flex;justify-content:space-between;align-items:center;gap:12px;
     padding:11px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255/.07));
     font-size:13px}
.tog s{text-decoration:none;display:block;font-size:11.5px;
       color:var(--color-ink-faint,#5a6172);margin-top:2px}
/* tsw, not sw: Policy already owns .sw for a colour swatch, and every
   screen's rules compose into ONE stylesheet — so a second .sw would turn
   every swatch in the shift dialog into a toggle. That is the .over collision
   the module's guard exists to catch, avoided rather than discovered. */
.tsw{width:34px;height:19px;border-radius:99px;flex:0 0 auto;position:relative;
     background:var(--color-line-strong,rgb(255 255 255/.14));border:0;cursor:pointer}
.tsw.on{background:var(--color-brand,#818cf8)}
.tsw::after{content:"";position:absolute;top:2px;left:2px;width:15px;height:15px;
            border-radius:50%;background:var(--color-ink,#e8ebf4)}
.tsw.on::after{left:17px}
.tsw:focus-visible{outline:2px solid var(--color-brand,#818cf8);outline-offset:2px}

/* Frame 7. The empty state names the consequence, not the button. */
.tvoid{margin:auto;max-width:420px;display:flex;flex-direction:column;gap:12px;
       align-items:center;text-align:center;padding:40px 0}
.tvoid .big{font-size:30px;color:var(--color-brand,#818cf8)}
.tvoid p{font-size:13px;line-height:1.6;color:var(--color-ink-muted,#8b93a7);margin:0}
.tvoid p b{color:var(--color-ink,#e8ebf4)}
.tvoid p.quiet{color:var(--color-ink-faint,#5a6172)}
`;
