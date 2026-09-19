/**
 * What a person can read on an element — its text, and what it says through attributes.
 *
 * **Each text node on its own line, never `textContent`.** `textContent` joins neighbouring
 * elements with nothing between them, so `"gives"` + `"roomcare_manager"` read as one word and a
 * register id glued to a letter loses the word boundary its pattern needs (found by the shared
 * guard's own first failing run, 15e2654). GuestOps' reader used `textContent`; this is the wider
 * of the two.
 *
 * **Four attributes, the union of what the three guards read**: a tooltip (`title`), a label read
 * aloud (`aria-label`), an accessible description (`aria-description`, which GuestOps carries its
 * reasons in) and a field's `placeholder`.
 *
 * Stylesheets and scripts are removed first: they are inside the element and nobody reads them.
 */

const SAID = ["title", "aria-label", "aria-description", "placeholder"] as const;

/**
 * The text a person can read in `root`, one text node or attribute per line.
 *
 * @param root the rendered surface — a screen, a dialog, a widget
 * @returns everything readable, newline-joined
 */
export function readableText(root: Element): string {
  const copy = root.cloneNode(true) as Element;
  for (const hidden of Array.from(copy.querySelectorAll("style, script"))) hidden.remove();

  const said = Array.from(copy.querySelectorAll(SAID.map((name) => `[${name}]`).join(", ")))
    .flatMap((element) => SAID.map((name) => element.getAttribute(name) ?? ""))
    .filter((text) => text !== "");

  const texts: string[] = [];
  const walker = copy.ownerDocument.createTreeWalker(copy, 4 /* NodeFilter.SHOW_TEXT */);
  for (let node = walker.nextNode(); node !== null; node = walker.nextNode()) {
    texts.push(node.nodeValue ?? "");
  }

  return [...texts, ...said].join("\n");
}
