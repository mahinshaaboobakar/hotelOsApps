/**
 * The card every Workforce widget is drawn on.
 *
 * One size, one header, one row shape — `SHELL-Q35`'s size guarantee expressed
 * as a construction kit rather than as a convention. A widget that wanted a
 * different header would have to stop using this, which is the point: *a row of
 * tiles whose menus open to five different shapes reads as five different
 * products*.
 *
 * # Content that does not fit is cut here
 *
 * The shell does not resize to content, so a list shows what the card holds and
 * the figure above it counts the property. Shift Board draws four departments
 * and says six — that is the guarantee working, not a truncation defect.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { el, fill } from "../chrome/element";
import type { Figure, Segment, SummaryRow } from "../roster/widget";

import { WIDGET_CSS } from "./styles";

/** The application's name, in the header's right-hand slot. */
const APPLICATION = "Workforce";

/**
 * The capability a tap exercises — the widget entry contract's, not this
 * application's.
 *
 * A widget cannot navigate: it has no `window.parent`, no network and no route
 * to the shell's document. `shell.open` is the one channel, and the widget
 * **does not name an application** — the shell knows which package the widget
 * came from and opens that one.
 */
const SHELL_OPEN = "shell.open";

/**
 * Open this application at the screen a row names.
 *
 * A refusal is **shown**, never swallowed. The design's rule is that every
 * element taps through, so a tap that silently does nothing is the one outcome
 * worse than a tap that says why — and the line appears only after a refused
 * tap, so it costs the approved frame nothing.
 *
 * @param host the bridge
 * @param row the element tapped, so the refusal lands on its own card
 * @param destination this application's own word for a screen
 */
async function open(host: HostApi, row: HTMLElement, destination: string): Promise<void> {
  try {
    await host.call(SHELL_OPEN, "at", { destination });
  } catch (error) {
    const card = row.closest(".wcard");
    if (card === null) return;

    // ADR 0041, asked by the SDK so a package does not rediscover the rule:
    // `internal` and `forbidden` carry a message for a log, not for a screen.
    const because =
      error instanceof HostCallError && error.isForPeople ? error.message : null;

    card.querySelector(".wrefusal")?.remove();
    card.append(
      el(
        "div",
        "wrefusal",
        because === null ? "That screen could not be opened." : `Not opened — ${because}`,
      ),
    );
  }
}

/**
 * Build a widget's card.
 *
 * # The header says when the figures are not this property's
 *
 * **A deliberate divergence from the approved artboards**, and the smallest one
 * that keeps the widget honest. `56-app-widgets.md`'s own argument for
 * refreshing on open is that *a widget showing a figure from the last time it
 * was opened is worse than one showing nothing, because it looks current* — and
 * a card drawing another property's recorded numbers with nothing said is the
 * same failure with a longer stale window.
 *
 * It goes in the header's existing right-hand slot rather than as a line of its
 * own: the card is a fixed 320 x 384 and three of the five already spend their
 * foot on a caption, so a sixth part would push a row off the bottom of a
 * frame the owner approved.
 *
 * @param title what the header shows
 * @param live whether these figures came from the platform
 * @param body the card's contents, in order
 * @returns the card, ready to mount
 */
export function card(
  title: string,
  live: boolean,
  body: readonly (Node | null)[],
): HTMLElement {
  const root = el("div", "wcard");
  root.dataset["live"] = String(live);

  const head = el("div", "whead");
  head.append(
    el("span", "wtitle", title),
    el("span", "wapp", live ? APPLICATION : `${APPLICATION} · recorded`),
  );

  const contents = el("div", "wbody");
  fill(contents, ...body);

  return fill(root, head, contents);
}

/**
 * The stylesheet, as an element a widget mounts beside its card.
 *
 * Held here rather than appended by each widget: five copies of one line is
 * five places for the sheet to be forgotten, and a widget that forgot it draws
 * an unstyled column that a suite cannot see.
 */
export function stylesheet(): HTMLElement {
  const style = el("style");
  style.textContent = WIDGET_CSS;
  return style;
}

/**
 * The headline figures.
 *
 * @param figures two or four — the frames use both and nothing else
 * @returns the grid
 */
export function figures(values: readonly Figure[]): HTMLElement {
  const grid = el("div", values.length > 2 ? "wfigures four" : "wfigures");

  for (const figure of values) {
    const cell = el("div", "wfigure");
    cell.append(
      el("span", `wvalue ${figure.tone}`, figure.value),
      el("span", "wlabel", figure.label),
    );
    grid.append(cell);
  }

  return grid;
}

/**
 * A proportion bar, sized from the counts it is given.
 *
 * The width arithmetic is here rather than in the data, so the bar cannot
 * disagree with the figures above it: `flex-grow` on the count means the
 * segments are the counts, and a fixture carrying "89%" would be a number
 * nothing computed.
 *
 * @param segments the parts, in order
 * @returns the bar, or null when there is nothing to divide
 */
export function bar(segments: readonly Segment[]): HTMLElement | null {
  if (segments.every((segment) => segment.count === 0)) return null;

  const track = el("div", "wbar");

  for (const segment of segments) {
    const part = el("span", segment.tone);
    part.style.flexGrow = String(segment.count);
    track.append(part);
  }

  return track;
}

/**
 * A section's label.
 *
 * @param text the label, in the frame's own words
 * @returns the label
 */
export function section(text: string): HTMLElement {
  return el("div", "wsection", text);
}

/**
 * A list of rows.
 *
 * @param entries what to draw
 * @param host the bridge, because a tap is a capability call
 * @returns the list, or null when there is nothing to say
 */
export function rows(entries: readonly SummaryRow[], host: HostApi): HTMLElement | null {
  if (entries.length === 0) return null;

  const list = el("div", "wrows");

  for (const entry of entries) {
    const row = el("button", "wrow");
    row.setAttribute("type", "button");
    // The destination is on the element as well as in the handler, so a capture
    // and a test can both see where a row would go.
    row.dataset["opens"] = entry.opens;
    row.addEventListener("click", () => {
      void open(host, row, entry.opens);
    });

    fill(
      row,
      el("span", "wname", entry.name),
      entry.meta === null ? null : el("span", "wmeta", entry.meta),
      el("span", `wfig ${entry.tone}`, entry.value),
    );

    list.append(row);
  }

  return list;
}

/**
 * The note at the foot — what this widget does not answer, and why.
 *
 * @param text the sentence
 * @returns the note
 */
export function note(text: string): HTMLElement {
  return el("div", "wnote", text);
}
