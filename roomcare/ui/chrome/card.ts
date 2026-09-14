/**
 * The inline card — the surface a person reads on the page (page 64 §9, the
 * third surface: reading is a card, composing is a sheet, confirming is a
 * dialog). One heading and what it holds; every screen's card is this one.
 */

import { el, fill } from "./element";

/**
 * A card with its heading.
 *
 * @param title the heading — words, or words with a quieter aside built by the caller
 * @param content what the card holds; nulls are skipped so a caller can be conditional inline
 * @returns the card, to append to or to place
 */
export function card(title: string | Node, ...content: readonly (Node | null)[]): HTMLElement {
  const root = el("section", "card");
  const head = el("h3");
  head.append(title);
  root.append(head);
  return fill(root, ...content);
}
