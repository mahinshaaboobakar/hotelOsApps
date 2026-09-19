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
 * **What it does is required.** It used to be optional — *"omitted while the
 * action is not yet wired"* — and sixteen controls on the owner's platform
 * looked live and did nothing (capability ledger, 2026-09-19). A control that
 * cannot act is now {@link unavailable}, which says why; a live-looking button
 * with no action can no longer be written.
 *
 * @param className the design's control class
 * @param text the label
 * @param onClick what it does
 * @returns the control
 */
export function control(className: string, text: string, onClick: () => void): HTMLElement {
  const button = el("button", className, text);
  button.setAttribute("type", "button");
  button.addEventListener("click", onClick);
  return button;
}

/**
 * A control drawn where the design puts it, that GuestOps cannot perform yet.
 *
 * Disabled and dashed, so it does not look pressable, and it carries its reason
 * in words a person at the desk understands — as its accessible description and
 * as the tooltip a pointer finds. Where a screen has room for the reason as a
 * visible line, the screen draws that line too.
 *
 * @param className the design's control class; `off` is added here
 * @param text the label
 * @param why why it cannot be used, for a person — never a document or a code
 * @returns the control
 */
export function unavailable(className: string, text: string, why: string): HTMLElement {
  const button = el("button", `${className} off`, text) as HTMLButtonElement;
  button.setAttribute("type", "button");
  button.disabled = true;
  button.title = why;
  button.setAttribute("aria-description", why);
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

/**
 * A row's opener — §2, C8: the row's key text as a real button a keyboard
 * reaches.
 *
 * The page-64 audit found every row that opens something drawn as a `div` with
 * a click listener, unreachable without a pointer (2026-09-19). A whole-row
 * `<button>` would nest a row's own link inside it, so this follows Jobs' board:
 * the row keeps its click for a pointer, and its key text is the button. The
 * button stops its own click there, so one press opens once.
 *
 * @param label what the opener shows — the row's name, drawn as it was
 * @param open what opening the row does
 * @returns the button
 */
export function opener(label: HTMLElement, open: () => void): HTMLElement {
  const button = el("button", "opener");
  button.setAttribute("type", "button");
  button.append(label);
  button.addEventListener("click", (event) => {
    event.stopPropagation();
    open();
  });
  return button;
}
