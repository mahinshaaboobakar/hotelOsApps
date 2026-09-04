/**
 * A titled card, its label–value rows, and the tab bar above them.
 *
 * These three appear on all three frames, which is what makes them shared
 * rather than a screen's own: the stay page's THE STAY panel, Attention's four
 * cards and both frames' tab bars are the same drawing with different content.
 */

import type { DetailRow, Tab } from "../book/model";
import { control, el, fill } from "./element";
import { tags } from "./marks";

/**
 * A card with a header band.
 *
 * @param title the band's label, set uppercase by the stylesheet
 * @param aside what sits at the right of the band — a chip, or plain text
 * @returns the card, and the body to fill
 */
export function card(title: string, aside?: Node | string): { root: HTMLElement; body: HTMLElement } {
  const root = el("div", "card");
  const head = el("div", "ch", title);

  if (aside !== undefined) {
    const grow = el("div", "grow");
    grow.append(typeof aside === "string" ? document.createTextNode(aside) : aside);
    head.append(grow);
  }

  const body = el("div", "cb");
  root.append(head, body);
  return { root, body };
}

/**
 * One label–value row.
 *
 * The value is assembled in the design's order — plain text, then the part set
 * bold, then the tail, then whatever the value carries. Splitting it that way
 * rather than taking one string is what lets `214` be bold inside
 * `room — you: 214 · Opera: 208` without the caller writing markup.
 *
 * @param row the row to draw
 * @returns the row element
 */
export function detail(row: DetailRow): HTMLElement {
  const element = el("div", "fr");
  const value = el("div", "v");

  if (row.value !== "") {
    value.append(
      row.quiet === true
        ? el("span", "un", row.value)
        : document.createTextNode(row.value),
    );
  }

  if (row.strong !== undefined) value.append(el("b", undefined, row.strong));
  if (row.tail !== undefined) value.append(document.createTextNode(row.tail));

  fill(value, ...tags(row.tags));
  element.append(el("div", "k", row.label), value);
  return element;
}

/**
 * A tab bar.
 *
 * @param list the tabs, with the counts the design shows
 * @param current the label of the selected tab
 * @param go what to do when one is chosen
 * @returns the bar
 */
export function tabs(
  list: readonly Tab[],
  current: string,
  go: (label: string) => void,
): HTMLElement {
  const bar = el("div", "tabs");

  for (const tab of list) {
    const button = control(
      tab.label === current ? "tab on" : "tab",
      tab.label,
      () => go(tab.label),
    );

    if (tab.count !== undefined) button.append(el("span", "n", tab.count));
    bar.append(button);
  }

  return bar;
}

/**
 * A row of controls, the first of them primary.
 *
 * @param labels the actions, in the design's order
 * @returns the row, or null when there are none
 */
export function actions(labels: readonly string[]): HTMLElement | null {
  if (labels.length === 0) return null;

  const row = el("div", "acts");

  labels.forEach((label, index) => {
    row.append(control(index === 0 ? "btn sm pri" : "btn sm", label));
  });

  return row;
}
