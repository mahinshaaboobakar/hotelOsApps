/**
 * The Notes & photos tab — frame 2d: the notes as a timeline with the guest's
 * raising text first, the add field, the photo panel.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { when } from "../../chrome/instant";
import { lines, saying, values } from "../../chrome/form";
import { JOB_AMEND } from "../../chrome/permissions";
import { act, may, type JobDetail } from "../../board";

export function notes(host: HostApi, d: JobDetail, onChanged: () => void): HTMLElement {
  const grid = el("div", "cols");
  grid.style.gridTemplateColumns = "2fr 1fr";

  const left = el("div", "stack");
  const line = el("div", "tl");
  for (const note of d.notes) {
    const ev = el("div", "ev");
    ev.append(el("b", undefined, `${note.who} · ${when(host, note.at)}`), el("span", undefined, note.text));
    if (note.raising === true) ev.append(el("i", "dim", " (the raising text)"));
    line.append(ev);
  }
  const writing = el("div");
  writing.append(lines(null, "text", "Write a note…"));
  const said = saying();
  const row = el("div", "row");
  if (may(host, JOB_AMEND)) {
    row.append(control("btn pri", "Add note", () => {
      const text = String(values(writing).text ?? "");
      if (text.length === 0) {
        said.say("a note needs words");
        return;
      }

      void act(host, JOB_AMEND, "note", { id: d.row.id, text }).then((done) => {
        if (done.ok) onChanged();
        else said.say(done.refused ?? "the note was not added");
      });
    }));
  }

  // A photo needs the media service, which no application client reaches yet —
  // drawn and disabled rather than drawn and inert, so the screen does not
  // promise something that silently does nothing.
  const photo = control("btn", "Attach photo");
  photo.setAttribute("disabled", "true");
  photo.title = "photos wait for a media client";
  row.append(photo);
  left.append(line, writing, row, said.line);

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
