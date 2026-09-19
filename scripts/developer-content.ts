/**
 * What a mock's developer notes look like, when they reach a screen — one list for every application.
 *
 * Owner ruling, 2026-09-19: a mock carries two kinds of content, the screen (what a hotel person sees and does)
 * and notes for the developer (where a figure comes from, why a choice was made, which ruling governs, what the
 * backend sends). The second is never built as UI. This extends Workforce's register-id check (b001bfac, GG) from
 * one pattern to the citation shapes found across the four applications, so each application's rendered-text walk
 * imports this list instead of keeping a private copy that drifts.
 *
 * Every pattern is a citation or a code identifier: a shape that has no reading for a person at the property. It
 * cannot find a service's name or a data source written in plain words ("from Master Data's location tree") —
 * those are found by reading, and each application's ledger records what its reading found. A failure state's own
 * reason is not exempt: the screen's sentence for a person cites no document either.
 */
export const DEVELOPER_CONTENT: readonly (readonly [string, RegExp])[] = [
  ["a decision-register id", /\b[A-Z]+-Q[0-9]+[a-z]?\b/g],
  ["an ADR", /\bADR[ -]?[0-9]+/g],
  ["a section sign", /§/g],
  ["a design-section reference", /\(S[0-9]+\b[^)]*\)/g],
  ["a design-row reference", /\(row [0-9]+\)/g],
  ["a chapter reference", /\bChapter [0-9]+/g],
  ["a code identifier", /\b[a-z]+(?:_[a-z]+)+\b/g],
  ["a correlation id", /\bcorrelation ids?\b/gi],
];

/** Every developer citation in `text`, each as "what it is: the text found". */
export function developerContent(text: string): string[] {
  return DEVELOPER_CONTENT.flatMap(([what, pattern]) => [...text.matchAll(pattern)].map((m) => `${what}: ${m[0]}`));
}

/**
 * What a person can read in `root`: its text without stylesheets, and the words the screen says through
 * attributes — a tooltip, a label read aloud, a field's placeholder.
 */
export function readableText(root: Element): string {
  const copy = root.cloneNode(true) as Element;
  for (const hidden of Array.from(copy.querySelectorAll("style, script"))) hidden.remove();
  const said = Array.from(copy.querySelectorAll("[title], [aria-label], [placeholder]"))
    .flatMap((e) => ["title", "aria-label", "placeholder"].map((a) => e.getAttribute(a) ?? ""));
  // Each text node on its own line, never `textContent`: that joins neighbouring elements with nothing between
  // them, so "gives" + "roomcare_manager" read as one word, and a register id glued to a letter loses the word
  // boundary its pattern needs — found by this guard's own first failing run.
  const texts: string[] = [];
  const walker = copy.ownerDocument.createTreeWalker(copy, 4 /* NodeFilter.SHOW_TEXT */);
  for (let node = walker.nextNode(); node !== null; node = walker.nextNode()) texts.push(node.nodeValue ?? "");
  return [...texts, ...said].join("\n");
}
