/**
 * The Notes & photos tab — frame 2d: the notes as a timeline with the guest's
 * raising text first, the add field, the photo panel.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { when } from "../../chrome/instant";
import type { JobDetail } from "../../board";

export function notes(host: HostApi, d: JobDetail): HTMLElement {
  const grid = el("div", "cols");
  grid.style.gridTemplateColumns = "2fr 1fr";

  const left = el("div");
  const line = el("div", "tl");
  for (const note of d.notes) {
    const ev = el("div", "ev");
    ev.append(el("b", undefined, `${note.who} · ${when(host, note.at)}`), el("span", undefined, note.text));
    if (note.raising === true) ev.append(el("i", "dim", " (the raising text)"));
    line.append(ev);
  }
  left.append(line, el("div", "field ph", "Write a note…"));
  const row = el("div", "row");
  row.append(control("btn pri", "Add note"), control("btn", "Attach photo"));
  left.append(row);

  const photos = d.notes.filter((n) => n.photo !== null);
  const right = el("div", "card");
  right.append(el("h3", undefined, `Photos · ${String(photos.length)}`));
  for (const p of photos) {
    const frame = el("div", "field ph", p.photo ?? "");
    frame.style.height = "120px";
    frame.style.justifyContent = "center";
    frame.style.alignItems = "center";
    right.append(frame, el("div", "mono", `${p.who} · ${when(host, p.at)}`));
  }

  return fill(grid, left, right);
}
