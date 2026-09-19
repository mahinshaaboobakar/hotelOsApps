/**
 * 7g · Property-wide access — the one grant that is not a posting (S6;
 * AUTHZ-Q25). Every other role comes from Workforce; this tab says so on its
 * face. Room Care announces the grant and writes no tuple; the Kernel folds it.
 * A person's posting is Workforce's, read through Context, which apps cannot
 * call yet (PKG-Q8). The column draws a dash: it used to say "read through
 * Context" with the register id, developer content the owner ruled off the
 * screen (2026-09-19). Whether the column stays is queued for the owner.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { failed } from "../../chrome/failure";
import { control, el } from "../../chrome/element";
import { day } from "../../chrome/instant";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { actions, dialog, readyWhen, sheet } from "../../chrome/overlay";

interface Grants {
  grants: { userId: string; name: string; grantedAt: string; grantedBy: string | null }[];
}

export async function access(host: HostApi, body: HTMLElement, nav: Nav): Promise<void> {
  const read = el("div", "dlg-note");
  const kv = el("div", "kv");
  const tab = el("div");
  tab.append(el("b", undefined, "one exception only:"), document.createTextNode(" a person not posted as head of Housekeeping who must hold everything in Room Care at this property — a rooms-division manager, a GM's deputy. "),
    el("b", undefined, "The general manager grants it."));
  kv.append(
    el("div", "k", "Attendants"), el("div", undefined, "posted to Housekeeping by Workforce → their own assigned rooms. Nothing to set here."),
    el("div", "k", "Supervisors"), el("div", undefined, "posted as supervisor in Housekeeping → assign · amend. Nothing to set here."),
    el("div", "k", "Head of Housekeeping"), el("div", undefined, "posted as head of Housekeeping → configure · plan. Nothing to set here."),
    el("div", "k", "Opening the app"), el("div", undefined, "Core Administration › Applications — the door, not a role."),
    el("div", "k", "This tab"), tab,
  );
  read.append(el("div", "sect", "Read this first — every role in Room Care comes from Workforce"), kv);

  const got = await load<Grants>(host, READ, "grants");
  if (!got.ok) {
    body.append(read, failed(host, got.failure, "the property-wide grants", nav.show));
    return;
  }

  const table = el("table");
  const head = el("tr");
  for (const name of ["Person", "Posting (Workforce)", "Holds", "Since", "Granted by", ""]) head.append(el("th", undefined, name));
  table.append(head);
  for (const grant of got.value.grants) {
    const tr = el("tr");
    const posting = el("td", "dim");
    posting.append(document.createTextNode("—"));
    const holds = el("td");
    holds.append(el("span", "pill ok", "property-wide access"));
    const revoke = el("td");
    revoke.append(control("btn sm danger", "Revoke…", () => confirmRevoke(host, nav, grant.userId, grant.name)));
    tr.append(el("td", undefined, grant.name), posting, holds, el("td", undefined, day(host, grant.grantedAt.slice(0, 10))), el("td", undefined, grant.grantedBy ?? "—"), revoke);
    table.append(tr);
  }
  const n = got.value.grants.length;
  const count = el("div", "count", `${n === 0 ? "no grants" : n === 1 ? "1 grant" : `${whole(host, n)} grants`} at this property · granted and revoked by the general manager only · postings come from Workforce and cannot be edited here`);
  const grantRow = el("div", "row");
  grantRow.style.marginTop = "12px";
  grantRow.append(control("btn pri", "Grant to a person…", () => grant(host, nav)));

  const gives = el("div", "kv");
  gives.append(el("div", "k", "Property-wide access"), el("div", undefined, "every Room Care capability, for every room at the property — read · assign · amend · configure · plan"),
    el("div", "k", "Not the grant"), el("div", undefined, "an attendant's done · an inspector's sign-off · placing a room out of order"));
  const others = el("div", "kv");
  others.append(el("div", "k", "Floor supervisors"), el("div", undefined, "assign · amend — because Workforce posts them as supervisor in Housekeeping"),
    el("div", "k", "Attendants"), el("div", undefined, "their own rooms — because they are assigned them"),
    el("div", "k", "The desk"), el("div", undefined, "read — the board and a room's day; nothing to grant here"));
  const cols = el("div", "cols");
  cols.style.marginTop = "16px";
  cols.append(card("What the grant gives", gives), card("Who else may do what — from postings, not from this tab", others));
  body.append(read, table, count, grantRow, cols);
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
  readyWhen(overlay, () => (person.value.trim() === "" ? "name the person by their login id" : null));
}

function confirmRevoke(host: HostApi, nav: Nav, userId: string, name: string): void {
  const overlay = dialog(nav.frame, `Revoke ${name}'s property-wide access?`);
  overlay.body.append(el("p", undefined, "They keep whatever their Workforce posting gives them. Recorded: who, when."));
  actions(overlay, "Revoke", () => void (async () => {
    const done = await act(host, "roomcare.configure", "revokeManager", { userId });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })(), "btn danger confirm");
}
