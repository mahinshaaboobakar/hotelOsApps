/**
 * Setup's controls — the toggle, the radio line, the inline field and the save
 * line every tab ends with (mockup 02). A saved change is a new version, so the
 * save line always says which version is live and who changed it last.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, option } from "../../chrome/element";
import { when } from "../../chrome/instant";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import type { SetupData } from "./index";

/** A switch — a real checkbox, so a keyboard reaches it, drawn as the frame's toggle. */
export function toggle(checked: boolean, label?: string): HTMLInputElement {
  const box = el("input", "tog") as HTMLInputElement;
  box.type = "checkbox";
  box.checked = checked;
  if (label !== undefined) box.setAttribute("aria-label", label);
  return box;
}

/** One choice of a group: its word in bold, then what it means. */
export function radio(name: string, value: string, checked: boolean, bold: string, rest: string, onChoose?: (value: string) => void): HTMLElement {
  const line = el("label", "radio");
  const input = el("input") as HTMLInputElement;
  input.type = "radio";
  input.name = name;
  input.value = value;
  input.checked = checked;
  if (onChoose !== undefined) input.addEventListener("change", () => onChoose(value));
  line.append(input, el("b", undefined, bold));
  if (rest !== "") line.append(document.createTextNode(` ${rest}`));
  return line;
}

/** A number sat inside a sentence — "re-check every [60] min". */
export function inlineNumber(value: number | null, onChange?: (value: number) => void, step?: string): HTMLInputElement {
  const input = el("input", "inline") as HTMLInputElement;
  input.type = "number";
  input.value = value === null ? "" : String(value);
  if (step !== undefined) input.step = step;
  if (onChange !== undefined) input.addEventListener("change", () => onChange(Number(input.value)));
  return input;
}

/** A choice sat inside a sentence; a vocabulary of one word is drawn as that one word, never a pretend list. */
export function inlineSelect(choices: readonly (readonly [string, string])[], current: string, onChange?: (value: string) => void): HTMLSelectElement {
  const select = el("select", "inline") as HTMLSelectElement;
  for (const [value, label] of choices) select.append(option(label, value, value === current));
  if (onChange !== undefined) select.addEventListener("change", () => onChange(select.value));
  return select;
}

/** A sentence made of words and controls. */
export function sentence(className: string, ...parts: readonly (Node | string)[]): HTMLElement {
  const line = el("div", className);
  line.append(...parts);
  return line;
}

/** The line every tab ends with — Save, Discard, and which version is live. */
export function saveLine(host: HostApi, data: SetupData | null, save: () => void, discard: () => void): { line: HTMLElement; said: HTMLElement } {
  const line = el("div", "save");
  const said = el("span", "said");
  line.append(control("btn pri", "Save", save), control("btn", "Discard", discard));
  if (data !== null) line.append(versionLine(host, data));
  line.append(said);
  return { line, said };
}

/** A sheet that reorders a short list with ↑ and ↓; the tab's Save keeps the order as a new version. */
export function reorderSheet(nav: Nav, title: string, items: readonly string[], word: (item: string) => string, note: string,
  keep: (order: string[]) => string | null): void {
  const overlay = sheet(nav.frame, title);
  const order = [...items];
  const list = el("div");
  const swap = (a: number, b: number): void => {
    const held = order[a]!;
    order[a] = order[b]!;
    order[b] = held;
    draw();
  };
  const draw = (): void => {
    list.replaceChildren(...order.map((item, i) => {
      const line = el("div", "row");
      line.style.margin = "6px 0";
      const up = control("btn sm", "↑", () => swap(i, i - 1));
      const down = control("btn sm", "↓", () => swap(i, i + 1));
      if (i === 0) up.setAttribute("disabled", "");
      if (i === order.length - 1) down.setAttribute("disabled", "");
      line.append(up, down, el("span", undefined, `${i + 1} · ${word(item)}`));
      return line;
    }));
  };
  draw();
  overlay.body.append(list, el("p", "dim", note));
  actions(overlay, "Keep this order", () => {
    const refusal = keep(order);
    if (refusal !== null) return overlay.refuse(refusal);
    overlay.close();
  });
}

/** Say a refusal on the save line, never swallow it. */
export function refuse(said: HTMLElement, because: string): void {
  said.className = "said bad";
  said.textContent = because;
}

function versionLine(host: HostApi, data: SetupData): HTMLElement {
  const p = data.policy;
  return el("span", "mono", p.version === 0 ? "the property's defaults — no version saved yet" : `version ${p.version} · ${data.changedBy ?? "—"} · ${when(host, p.changedAt)}`);
}
