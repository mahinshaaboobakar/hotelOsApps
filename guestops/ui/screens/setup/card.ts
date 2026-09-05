/**
 * One card of settings — the shape every panel on frame 16 shares.
 */

import type { SettingBlock, SettingCard, SettingRow } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { card } from "../../chrome/panel";
import { tags } from "../../chrome/marks";

/**
 * Draw a settings card.
 *
 * @param panel its title, rows and whatever hangs in its header
 * @returns the card
 */
export function settings(panel: SettingCard): HTMLElement {
  const built = card(
    panel.title,
    panel.aside === null
      ? undefined
      : typeof panel.aside === "string"
        ? panel.aside
        : tags([panel.aside])[0],
  );

  fill(built.body, ...panel.rows.map(row), ...panel.blocks.map(block));

  return built.root;
}

/** One block under the rows, drawn where the design puts it. */
function block(one: SettingBlock): HTMLElement {
  if (one.kind === "actions") {
    const acts = el("div", "acts");

    for (const [index, label] of one.labels.entries()) {
      acts.append(control(index === 0 ? "btn sm pri" : "btn sm", label));
    }

    return acts;
  }

  return strong(one.text, one.kind === "hint" ? "hint" : "note");
}

/**
 * One setting.
 *
 * The four optional parts are the design's own emphasis, and they are separate
 * fields rather than one marked-up string because each says a different thing:
 * `strong` is the value that matters, `quiet` is a reason somebody typed,
 * `note` is who set it and when. Folding them into one would leave the screen
 * deciding which part of a sentence is a fact.
 */
export function row(setting: SettingRow): HTMLElement {
  const element = el("div", "fr");
  const value = el("div", "v");

  if (setting.value !== "") {
    value.append(document.createTextNode(setting.value));
  }

  if (setting.strong !== undefined) {
    value.append(el("b", undefined, setting.strong));
  }

  if (setting.tail !== undefined) {
    value.append(document.createTextNode(setting.tail));
  }

  if (setting.quiet !== undefined) {
    value.append(el("span", "un", setting.quiet));
  }

  fill(value, ...tags(setting.tags));

  if (setting.note !== undefined) {
    value.append(el("span", "hint", setting.note));
  }

  element.append(el("div", "k", setting.label), value);
  return element;
}

/** A statement, with its first sentence carrying the weight. */
function strong(text: string, className = "note"): HTMLElement {
  const element = el("div", className);
  const [lead, ...rest] = text.split(". ");

  element.append(
    el("b", undefined, `${lead ?? ""}.`),
    document.createTextNode(` ${rest.join(". ")}`),
  );

  return element;
}
