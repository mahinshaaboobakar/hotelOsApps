/**
 * Making elements — the three calls every screen uses, so no screen touches
 * `document.createElement` with its own conventions.
 */

/** An element with a class and, optionally, text. */
export function el(tag: string, className?: string, text?: string): HTMLElement {
  const element = document.createElement(tag);
  if (className !== undefined) element.className = className;
  if (text !== undefined) element.textContent = text;
  return element;
}

/**
 * A real button, so a keyboard reaches it, with an optional click.
 *
 * `data-acts` marks a control that does something. A listener added with
 * `addEventListener` is invisible to the DOM, so without the mark no check can
 * tell a wired primary from a dead one — and Jobs shipped two dead primaries
 * (Engineering's clock's *Save*, the add-a-step box's *Add*) that nothing found
 * until the page-64 audit's C11 line looked (2026-09-19).
 */
export function control(className: string, text: string, onClick?: () => void): HTMLElement {
  const button = el("button", className, text);
  button.setAttribute("type", "button");
  if (onClick !== undefined) {
    button.addEventListener("click", onClick);
    button.setAttribute("data-acts", "");
  }
  return button;
}

/** Append the children that exist; nulls are skipped so callers can be conditional inline. */
export function fill(parent: HTMLElement, ...children: readonly (Node | string | null)[]): HTMLElement {
  for (const child of children) {
    if (child === null) continue;
    parent.append(typeof child === "string" ? document.createTextNode(child) : child);
  }
  return parent;
}

/**
 * A primary action that cannot act yet, drawn `off` with its reason beside it.
 *
 * Standard §2 (checklist C11): *"A primary action with nothing to send is drawn
 * `off`, with the reason beside it — never live-and-refusing."* Three primaries
 * in Settings were live and did nothing — Engineering's clock's *Save*, the
 * add-a-step box's *Add*, and *Save policy*, which navigated back to the list
 * and saved nothing. A disabled button with no reason is a puzzle, so the
 * reason is always given.
 */
/**
 * A secondary control with nothing behind it yet: drawn `off`, disabled, its
 * reason carried in its title (and said beside it by the caller where a row has
 * room). Never live-and-inert — the owner found such buttons on 2026-09-19.
 */
export function off(className: string, label: string, reason: string): HTMLButtonElement {
  const button = el("button", `${className} off`, label) as HTMLButtonElement;
  button.type = "button";
  button.disabled = true;
  button.title = reason;
  return button;
}

export function unavailable(label: string, reason: string): HTMLElement {
  const button = control("btn pri off", label);
  button.setAttribute("disabled", "true");
  return fill(el("span", "row"), button, el("span", "mono", reason));
}
