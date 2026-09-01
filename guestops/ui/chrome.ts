/**
 * The application's chrome — the rail, the header, and the four marks.
 *
 * # Styled by the platform's tokens, never by a theme of its own
 *
 * A hosted module runs in its own realm, so it cannot render the shell's
 * components. What crosses the boundary is the **token set**, written into this
 * document as CSS custom properties, and that is what makes an installed
 * application look like HotelOS — bound 1 as it can actually be enforced across
 * a realm (`sdk-typescript/src/module.ts`). Every colour below is a `var()`.
 *
 * **Every token carries a fallback, and that is not defensive habit.** The
 * realm's own base stylesheet references `--color-text` and `--font-sans`,
 * while the shell's stylesheet defines `--color-ink` and `--color-surface`;
 * the two vocabularies do not currently meet, and no caller supplies a token
 * set yet because nothing renders `ModuleFrame`. A module that assumed either
 * name would render unreadable text against a transparent body the first time
 * it mounted for real. Reported rather than resolved here — naming the token
 * set a hosted module may rely on is the shell's to rule.
 */

/** Make an element with a class and optional text. */
export function el(tag: string, className?: string, text?: string): HTMLElement {
  const element = document.createElement(tag);
  if (className !== undefined) element.className = className;
  if (text !== undefined) element.textContent = text;
  return element;
}

/**
 * The stylesheet this module writes into its own document.
 *
 * One block rather than per-element styles: a module owns its document, and a
 * stylesheet is how a document is styled. Scrollbars are absent on purpose —
 * ADR 0111 styles them once, and the realm reasserts that rule itself.
 */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");

  style.textContent = `
    .go{display:flex;height:100vh;font-size:13px;color:var(--color-ink,var(--color-text,#e8e8ea))}
    .rail{width:210px;flex:none;padding:14px 10px;display:flex;flex-direction:column;gap:2px;
      background:var(--color-surface-sunken,rgba(255,255,255,.03));
      border-right:1px solid var(--color-line,rgba(255,255,255,.08))}
    .app{display:flex;align-items:center;gap:9px;padding:6px 8px 14px;font-weight:600}
    .mark{width:24px;height:24px;border-radius:7px;display:grid;place-items:center;font-size:10px;
      color:var(--color-ink-on-accent,#fff);background:var(--color-brand,#4f7cff)}
    .ri{display:flex;align-items:center;justify-content:space-between;padding:7px 9px;border-radius:8px;
      cursor:pointer;color:var(--color-ink-muted,rgba(232,232,234,.72))}
    .ri:hover{background:var(--color-surface-raised,rgba(255,255,255,.05))}
    .ri.on{background:var(--color-surface-raised,rgba(255,255,255,.07));color:var(--color-ink,inherit);font-weight:600}
    .cnt{font-size:11px;color:var(--color-ink-faint,rgba(232,232,234,.45))}
    .cnt.att{color:var(--color-warn,#e9a13b);font-weight:600}
    .main{flex:1;min-width:0;display:flex;flex-direction:column}
    .head{display:flex;align-items:center;gap:12px;padding:16px 20px;
      border-bottom:1px solid var(--color-line,rgba(255,255,255,.08))}
    .ht{font-size:17px;font-weight:600}
    .hsub{font-size:11.5px;color:var(--color-ink-faint,rgba(232,232,234,.45));margin-top:3px}
    .body{flex:1;overflow:auto;padding:16px 20px;display:flex;flex-direction:column;gap:16px}
    .strip{display:flex;gap:10px}
    .stat{flex:1;padding:11px 13px;border-radius:var(--radius-md,11px);
      border:1px solid var(--color-line,rgba(255,255,255,.08));
      background:var(--color-surface,rgba(255,255,255,.02))}
    .stat b{display:block;font-size:21px;font-weight:600}
    .stat span{font-size:11px;color:var(--color-ink-faint,rgba(232,232,234,.45))}
    .sec{font-size:11px;letter-spacing:.07em;text-transform:uppercase;
      color:var(--color-ink-faint,rgba(232,232,234,.45))}
    .row{display:grid;grid-template-columns:1fr 88px 128px 118px 132px;gap:10px;align-items:center;
      padding:10px 13px;border-radius:var(--radius-md,11px);
      border:1px solid var(--color-line,rgba(255,255,255,.08));
      background:var(--color-surface,rgba(255,255,255,.02))}
    .row + .row{margin-top:6px}
    .row.act{cursor:pointer}
    .row.act:hover{border-color:var(--color-line-strong,rgba(255,255,255,.16))}
    .who{font-weight:600}
    .thin{font-size:11.5px;color:var(--color-ink-muted,rgba(232,232,234,.6))}
    .sh{display:inline-flex;align-items:center;gap:5px;padding:2px 7px;border-radius:6px;font-size:10.5px}
    .sh.pms{background:var(--color-brand,#4f7cff);color:var(--color-ink-on-accent,#fff)}
    .sh.override{background:var(--color-warn-soft,rgba(233,161,59,.16));color:var(--color-warn,#e9a13b)}
    .sh.disagrees{background:var(--color-bad-soft,rgba(248,113,113,.14));color:var(--color-bad,#f87171)}
    .sh.missing{border:1px dashed var(--color-line-strong,rgba(255,255,255,.22));
      color:var(--color-ink-faint,rgba(232,232,234,.45))}
    .card{padding:14px 16px;border-radius:var(--radius-panel,14px);
      border:1px solid var(--color-line,rgba(255,255,255,.08));
      background:var(--color-surface,rgba(255,255,255,.02));display:flex;flex-direction:column;gap:9px}
    .card h3{margin:0;font-size:13px;font-weight:600}
    .card p{margin:0;font-size:12px;line-height:1.7;color:var(--color-ink-muted,rgba(232,232,234,.6))}
    .pair{display:flex;gap:10px}
    .pair > div{flex:1;padding:9px 11px;border-radius:9px;
      border:1px solid var(--color-line,rgba(255,255,255,.08))}
    .pair span{display:block;font-size:10.5px;letter-spacing:.06em;text-transform:uppercase;
      color:var(--color-ink-faint,rgba(232,232,234,.45));margin-bottom:3px}
    .acts{display:flex;gap:8px}
    .btn{padding:6px 12px;border-radius:8px;font-size:12px;cursor:pointer;
      border:1px solid var(--color-line-strong,rgba(255,255,255,.16));
      background:var(--color-surface-raised,rgba(255,255,255,.05));color:inherit}
    .btn.go{padding:6px 12px;height:auto;display:inline-block;
      background:var(--color-brand,#4f7cff);border-color:transparent;
      color:var(--color-ink-on-accent,#fff)}
    .stand{display:flex;align-items:center;gap:8px;padding:8px 12px;border-radius:9px;font-size:11.5px;
      border:1px dashed var(--color-line-strong,rgba(255,255,255,.22));
      color:var(--color-ink-faint,rgba(232,232,234,.45))}
  `;

  return style;
}

/** The application rail — the four places this application has. */
export function rail(current: string, counts: Readonly<Record<string, string>>,
  go: (screen: string) => void): HTMLElement {
  const element = el("div", "rail");

  const app = el("div", "app");
  app.append(el("div", "mark", "GO"), document.createTextNode("GuestOps"));
  element.append(app);

  for (const name of ["Today", "Bookings", "Guests", "Attention"]) {
    const item = el("div", name === current ? "ri on" : "ri", name);
    const count = el("span", name === "Attention" ? "cnt att" : "cnt", counts[name] ?? "");
    item.append(count);
    item.addEventListener("click", () => go(name));
    element.append(item);
  }

  return element;
}

/** One of the four marks that carry the whole application. */
export function shield(mark: string, text: string): HTMLElement {
  return el("span", `sh ${mark}`, text);
}

/**
 * The banner shown when a screen is not reading the property's own data.
 *
 * <b>Always rendered when the data is recorded.</b> A person looking at a stay
 * must be able to tell whether they are seeing their hotel; a module that hid
 * the difference is one somebody eventually acts on.
 */
export function standIn(because: string | null): HTMLElement {
  return el(
    "div",
    "stand",
    because ??
      "Recorded example data — the desktop has no GuestOps client yet, so nothing here is this property's.",
  );
}
