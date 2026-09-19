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

/** A real button, so a keyboard reaches it, with an optional click. */
export function control(className: string, text: string, onClick?: () => void): HTMLElement {
  const button = el("button", className, text);
  button.setAttribute("type", "button");
  if (onClick !== undefined) button.addEventListener("click", onClick);
  return button;
}

/**
 * A row's key text as a real button — APPS-Q50: a row that opens something keeps its click for a pointer, and its
 * main cell holds this button for a keyboard and a screen reader. It has no handler of its own: its click bubbles
 * to the row's, so a key and a pointer cannot do two different things.
 */
export function opener(...content: readonly (Node | string)[]): HTMLElement {
  const button = el("button", "opener");
  button.setAttribute("type", "button");
  button.append(...content);
  return button;
}

/** A select's option — made by the document, not the `Option` global a realm need not carry. */
export function option(label: string, value: string, selected = false): HTMLOptionElement {
  const choice = document.createElement("option");
  choice.textContent = label;
  choice.value = value;
  choice.selected = selected;
  return choice;
}

/** Append the children that exist; nulls are skipped so callers can be conditional inline. */
export function fill(parent: HTMLElement, ...children: readonly (Node | string | null)[]): HTMLElement {
  for (const child of children) {
    if (child === null) continue;
    parent.append(typeof child === "string" ? document.createTextNode(child) : child);
  }
  return parent;
}
