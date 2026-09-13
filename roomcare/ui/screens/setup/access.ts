/**
 * 7g · Property-wide access — the one grant that is not a posting (S6;
 * AUTHZ-Q25). Every other role comes from Workforce; this tab says so on its
 * face. Room Care announces the grant and writes no tuple; the Kernel folds it.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { day } from "../../chrome/instant";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, dialog, sheet } from "../../chrome/overlay";

interface Grants {
  grants: { userId: string; name: string; grantedAt: string; grantedBy: string | null }[];
}

export async function access(host: HostApi, body: HTMLElement, nav: Nav): Promise<void> {
  const read = el("section", "card");
  const kv = el("div", "kv");
  kv.append(
    el("div", "k", "Attendants"), el("div", undefined, "posted to Housekeeping by Workforce → their own assigned rooms. Nothing to set here."),
    el("div", "k", "Supervisors"), el("div", undefined, "posted as supervisor in Housekeeping → assign · amend. Nothing to set here."),
    el("div", "k", "Opening the app"), el("div", undefined, "Core Administration › Applications — the door, not a role."),
    el("div", "k", "This tab"), el("div", undefined, "one exception only: a person not posted in Housekeeping who must hold everything in Room Care at this property. The general manager grants it."),
  );
  read.append(el("h3", undefined, "Read this first — every role in Room Care comes from Workforce"), kv);

  const got = await load<Grants>(host, "grants");
  if (!got.ok) {
    body.append(read, failed("Property-wide access", got.because));
    return;
  }

  const table = el("table");
  table.style.marginTop = "14px";
  const head = el("tr");
  for (const name of ["Person", "Holds", "Since", "Granted by", ""]) head.append(el("th", undefined, name));
  table.append(head);
  for (const grant of got.value.grants) {
    const tr = el("tr");
    const revoke = el("td");
    revoke.append(control("btn sm danger", "Revoke…", () => confirmRevoke(host, nav, grant.userId, grant.name)));
    tr.append(el("td", undefined, grant.name), el("td", "mono", "roomcare_manager"), el("td", "num", day(host, grant.grantedAt.slice(0, 10))), el("td", undefined, grant.grantedBy ?? "—"), revoke);
    table.append(tr);
  }

  const gives = el("section", "card");
  gives.style.marginTop = "14px";
  const g = el("div", "kv");
  g.append(el("div", "k", "roomcare_manager"), el("div", undefined, "every Room Care capability, for every room at the property — read · assign · amend · configure · plan"),
    el("div", "k", "Not the grant"), el("div", undefined, "an attendant's done (rides the assignment) · an inspector's sign-off · placing a room out of order"));
  gives.append(el("h3", undefined, "What the grant gives"), g);

  const grantRow = el("div", "row");
  grantRow.style.marginTop = "10px";
  grantRow.append(control("btn", "Grant to a person…", () => grant(host, nav)));
  const count = got.value.grants.length;
  body.append(read, table, el("div", "legend", `${count} ${count === 1 ? "grant" : "grants"} at this property · granted and revoked by the general manager (S6; AUTHZ-Q25)`), grantRow, gives);
}

function grant(host: HostApi, nav: Nav): void {
  const overlay = sheet(nav.frame, "Grant property-wide Room Care access");
  const person = el("input", "field") as HTMLInputElement;
  person.setAttribute("aria-label", "Person");
  overlay.body.append(el("p", "dim", "Room Care keeps no directory of people; name the person by their login id, as Core Administration shows it."), el("label", "lbl", "Person (id)"), person);
  actions(overlay, "Grant", () => void (async () => {
    const done = await act(host, "roomcare.configure", "grantManager", { userId: person.value.trim() });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

function confirmRevoke(host: HostApi, nav: Nav, userId: string, name: string): void {
  const overlay = dialog(nav.frame, `Revoke ${name}'s roomcare_manager?`);
  overlay.body.append(el("p", undefined, "They keep whatever their Workforce posting gives them. Recorded: who, when."));
  actions(overlay, "Revoke", () => void (async () => {
    const done = await act(host, "roomcare.configure", "revokeManager", { userId });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })(), "btn danger confirm");
}
