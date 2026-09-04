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

/** Append the children that exist; nulls are skipped so callers can be conditional inline. */
export function fill(parent: HTMLElement, ...children: readonly (Node | string | null)[]): HTMLElement {
  for (const child of children) {
    if (child === null) continue;
    parent.append(typeof child === "string" ? document.createTextNode(child) : child);
  }
  return parent;
}
