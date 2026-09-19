/**
 * Rename a team — the property's own word for it, changed.
 *
 * `posting.assign · rename` takes the team's id, the version the read sent and
 * the new name. The owner met Rename dead on 0.3.3; it was then drawn honestly
 * off, and is built now because everything it needs is served.
 *
 * A sheet, because a person composes a name here (§9). It opens holding the
 * current name — that is the team's value, not one the form chose — and the
 * confirm waits until there is a different, non-empty one to send.
 */

import type { HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { TeamDetail } from "../../roster/team";

/**
 * Build the sheet.
 *
 * @param host the bridge
 * @param open the team the pane has open — the one being renamed
 * @param close called when it is dismissed
 * @param done called after the rename lands, so the list is re-read
 * @returns the overlay
 */
export function renameTeam(
  host: HostApi, open: TeamDetail, close: () => void, done: () => void,
): HTMLElement {
  let name = open.team.name;

  const refusal = el("div", "note warn");
  const acts = foot("Rename", "Renaming…", close);

  function redraw(): void {
    acts.waitingFor(name.trim() === ""
      ? "Name the team"
      : name.trim() === open.team.name
        ? "Type the new name"
        : null);
  }

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "posting.assign", "rename", {
          id: open.team.id,
          version: open.team.version,
          name: name.trim(),
        });
        done();
      } catch (error) {
        // §9: a refusal keeps the sheet open — two live teams in one
        // department may not share a name, and the service says so.
        refusal.append(el("span", undefined,
          error instanceof WriteRefused ? error.message : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  const field = el("div", "fld");
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "text";
  input.name = "name";
  input.value = open.team.name;
  input.setAttribute("maxlength", "80");
  input.addEventListener("input", () => { name = input.value; redraw(); });

  field.append(
    el("div", "fld-label", "Name"),
    input,
    el("div", "note",
      "The property's own word. Two live teams in one department may not share one."),
  );

  redraw();

  return overlay("sheet", {
    head: [
      el("div", "ht", "Rename team"),
      el("div", "hsub", `${open.team.name} · ${open.team.departmentName}`),
    ],
    body: [field, refusal],
    foot: [acts.row],
  }, close);
}
