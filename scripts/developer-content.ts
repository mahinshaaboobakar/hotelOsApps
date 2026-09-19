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
 *
 * **The single copy** (architect, 2026-09-19): Workforce's and GuestOps' guards import this rather than keep their
 * own. Merged in then, so neither loses a catch by switching: "design page" (Workforce), an ADR with any run of
 * spaces or hyphens before its number, and `aria-description` among the attributes read (GuestOps' `8aae94e`).
 *
 * **The platform systems by name** (Master Data, Kernel, OpenFGA, Context Service, Integration Hub) joined this
 * list on 2026-09-19, taken from Workforce's local guard, once Room Care's three lines were cleared (the bar in
 * `7a2f013f`; Areas and Services with this change). Until then this paragraph said why they were not here yet:
 * those three lines, and Jobs' `board/recorded/settings.ts:79`.
 */
export const DEVELOPER_CONTENT: readonly (readonly [string, RegExp])[] = [
  ["a decision-register id", /\b[A-Z]+-Q[0-9]+[a-z]?\b/g],
  ["an ADR", /\bADR[\s-]*[0-9]+/g],
  ["a design page", /\bdesign page\b/gi],
  ["a section sign", /§/g],
  ["a design-section reference", /\(S[0-9]+\b[^)]*\)/g],
  ["a design-row reference", /\(row [0-9]+\)/g],
  ["a chapter reference", /\bChapter [0-9]+/g],
  ["a code identifier", /\b[a-z]+(?:_[a-z]+)+\b/g],
  ["a correlation id", /\bcorrelation ids?\b/gi],
  ["a platform system", /\b(?:Master Data|Kernel|OpenFGA|Context Service|Integration Hub)\b/g],
];

/** Every developer citation in `text`, each as "what it is: the text found". */
export function developerContent(text: string): string[] {
  return DEVELOPER_CONTENT.flatMap(([what, pattern]) => [...text.matchAll(pattern)].map((m) => `${what}: ${m[0]}`));
}

/**
 * What a person can read in `root`: its text without stylesheets, and the words the screen says through
 * attributes — a tooltip, a label read aloud, an accessible description, a field's placeholder.
 */
export function readableText(root: Element): string {
  const copy = root.cloneNode(true) as Element;
  for (const hidden of Array.from(copy.querySelectorAll("style, script"))) hidden.remove();
  const said = Array.from(copy.querySelectorAll("[title], [aria-label], [aria-description], [placeholder]"))
    .flatMap((e) => ["title", "aria-label", "aria-description", "placeholder"].map((a) => e.getAttribute(a) ?? ""));
  // Each text node on its own line, never `textContent`: that joins neighbouring elements with nothing between
  // them, so "gives" + "roomcare_manager" read as one word, and a register id glued to a letter loses the word
  // boundary its pattern needs — found by this guard's own first failing run.
  const texts: string[] = [];
  const walker = copy.ownerDocument.createTreeWalker(copy, 4 /* NodeFilter.SHOW_TEXT */);
  for (let node = walker.nextNode(); node !== null; node = walker.nextNode()) texts.push(node.nodeValue ?? "");
  return [...texts, ...said].join("\n");
}
