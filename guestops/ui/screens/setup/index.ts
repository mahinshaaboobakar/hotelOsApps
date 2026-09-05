/**
 * Setup — the screen a general manager fills in once. Gold frame 16.
 *
 * **A property capability, not a country's law compiled into the product.** A
 * property with no reporting obligation turns it off and never sees the tab,
 * the flag or the list. The home country is a setting, both required field sets
 * are the property's, and **no country is written into this screen** — which is
 * what lets one product serve a hotel in Kochi and a hotel in Dubai.
 *
 * **Every deadline is an offset, never a date** (R18). *24 hours after arrival*
 * moves when the arrival moves; a stored date silently stops matching.
 *
 * **And the honest limit is on the screen rather than in a document**: HotelOS
 * does not submit anything. Sending a filing automatically is an integration,
 * every integration on this platform is a connector, and that connector does
 * not exist — so it is stated, not drawn as a button that would not work.
 *
 * This file composes. The card is `card.ts`, because a settings card is its own
 * subject and four of them appear here in three different arrangements.
 */

import type { HostApi } from "@hotelos/sdk";

import { load, recordedSetup, type Setup } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { standIn } from "../../chrome/marks";
import { card } from "../../chrome/panel";
import { row, settings } from "./card";

/**
 * Render the screen.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param section which section is showing
 * @param go what to do when another section is chosen
 */
export async function setup(
  host: HostApi,
  into: HTMLElement,
  section: string,
  go: (section: string) => void,
): Promise<void> {
  const loaded = await load(host, "desk.configure", "setup", recordedSetup);
  const config = loaded.value;

  const body = el("div", "body");

  fill(
    body,
    loaded.live ? null : standIn(loaded.because),
    sections(config.sections, section, go),
    settings(config.lead),
    pair(config),
    required(config),
  );

  into.replaceChildren(body);
}

/**
 * The section switcher, with the two actions that apply to all of them.
 *
 * Save and Discard sit on the switcher for the same reason the list screens'
 * actions do (docs/working/64 §3): with the section named in the bar there is
 * no page header left to put them in. They belong to the screen rather than to
 * a section, because a manager changes the reporting policy and the card series
 * in one sitting and saves once.
 */
function sections(
  list: readonly { label: string; on: boolean }[],
  showing: string,
  go: (section: string) => void,
): HTMLElement {
  const bar = el("div", "tabs");

  for (const one of list) {
    bar.append(control(one.label === showing ? "tab on" : "tab", one.label, () => go(one.label)));
  }

  bar.append(el("div", "grow"), control("btn", "Discard"), control("btn pri", "Save"));
  return bar;
}

/** The two cards side by side — the policy, and what it currently owes. */
function pair(config: Setup): HTMLElement {
  const cols = el("div", "cols even");
  cols.append(settings(config.pair[0]), settings(config.pair[1]));
  return cols;
}

/**
 * The card whose body is two columns.
 *
 * The required sets are drawn side by side because they are a **comparison** —
 * *the above, plus* — and a single column would make them read as a sequence,
 * as though a foreign guest were asked the second list instead of both.
 */
function required(config: Setup): HTMLElement {
  const built = card(config.card.title);
  const cols = el("div", "cols even");

  const left = el("div", "stack");
  fill(left, ...config.card.left.map(row));

  const right = el("div", "stack");
  fill(right, ...config.card.right.map(row), el("div", "hint", config.card.hint));

  cols.append(left, right);
  built.body.append(cols);
  return built.root;
}
