/**
 * Constructing elements — the primitive every other file here builds on.
 *
 * Named for what it makes rather than for its role: this is not a `helpers` or
 * a `utils` (ADR 0038 forbids both by name, and for the reason on show here —
 * such a file becomes wherever anything homeless lands). What lives in this one
 * is element construction and nothing else, and a function that is not that
 * does not belong in it.
 */

/**
 * Make an element.
 *
 * @param tag the tag name
 * @param className optional class list
 * @param text optional text content
 * @returns the element
 */
export function el(tag: string, className?: string, text?: string): HTMLElement {
  const element = document.createElement(tag);
  if (className !== undefined) element.className = className;
  if (text !== undefined) element.textContent = text;
  return element;
}

/**
 * Make a button that looks like the design's control and behaves like a button.
 *
 * The gold mockup draws its controls as `<div>`s, which is right for a picture
 * and wrong for a product: a div is not focusable, not announced, and not
 * operable from a keyboard. The class is the mockup's; the element is a button.
 *
 * @param className the design's control class
 * @param text the label
 * @param onClick what it does; omitted while the action is not yet wired
 * @returns the control
 */
export function control(className: string, text: string, onClick?: () => void): HTMLElement {
  const button = el("button", className, text);
  button.setAttribute("type", "button");
  if (onClick !== undefined) button.addEventListener("click", onClick);
  return button;
}

/**
 * Append a run of children, skipping the ones that turned out to be absent.
 *
 * Screens assemble from optional parts — a banner that may not exist, a chip
 * list that may be empty — and a null check at every call site is how one gets
 * forgotten.
 *
 * @param parent what to append to
 * @param children the children, nulls ignored
 * @returns the parent, for chaining
 */
export function fill(parent: HTMLElement, ...children: readonly (Node | null)[]): HTMLElement {
  for (const child of children) {
    if (child !== null) parent.append(child);
  }

  return parent;
}
