/**
 * The decision panel — one waiting request, with what an approver decides on.
 *
 * Owner, 2026-09-20, on `64g` §4, option B: **a row opens a panel** carrying
 * the balance the decision would leave behind, who else is off across those
 * dates, and what the person wrote, with Approve and Decline at its foot. The
 * alternative — Approve and Decline on every row — was drawn beside it and
 * rejected: it is faster through a queue and decides without the facts.
 *
 * **On the page, not behind a scrim** (§9's third surface, `RC-Q6`): the
 * approver is reading the queue and this panel against each other, and a sheet
 * would hide the rows the decision is being weighed against.
 *
 * **The note is optional**, as the service has it — `DecideLeaveCommand.Note`
 * is nullable and declining without one is allowed. The frame said "required
 * to decline" until the service was read; whether it *should* be required is
 * the owner's, and is not decided here.
 */

import { formatNumber, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { days } from "../../chrome/dates";
import { foot } from "../../chrome/confirm";
import { control, el } from "../../chrome/element";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { Waiting } from "../../roster/leave";

/**
 * Draw the panel.
 *
 * @param host the bridge
 * @param item the waiting request
 * @param property whose locale the numbers and dates are written in
 * @param done called when the decision is made, to close and re-read
 * @returns the panel
 */
export function decision(
  host: HostApi,
  item: Waiting,
  property: PropertyEnvironment,
  done: () => void,
): HTMLElement {
  // `.swap`'s surface: this panel stands in the same column as the swap card
  // and is the same kind of thing — one decision, read on the page. A second
  // panel class would be a second look for one surface.
  const panel = el("div", "swap");
  const n = (value: number): string => formatNumber(value, property);

  const head = el("div");
  head.append(
    el("div", "ht", `${item.who} · ${item.type ?? "Leave"}`),
    el("div", "hsub", item.dates === undefined
      ? item.kind
      : `${days(item.dates, property)} · ${n(item.days ?? 0)} ${item.days === 1 ? "day" : "days"}`),
  );

  const facts = el("dl", "dfacts");

  // **Balance after, and it may be negative.** An approved overdraw is a real
  // state (`WF-Q5`), so the number is shown as the service sends it rather
  // than clamped at zero — a clamp would hide the decision being made.
  facts.append(el("dt", undefined, "Balance after"));
  facts.append(el("dd", undefined, item.after === undefined || item.after === null
    // Nothing has ever been posted for this person and type. Saying nothing is
    // the honest answer; a zero would claim a ledger row exists.
    ? "not recorded"
    : `${n(item.after)} of ${n((item.after ?? 0) + (item.days ?? 0))} days`));

  facts.append(el("dt", undefined, "Also off then"));
  facts.append(el("dd", undefined,
    item.alsoOff === undefined || item.alsoOff.length === 0
      ? "nobody else"
      : item.alsoOff.join(" · ")));

  facts.append(el("dt", undefined, "Note"));
  facts.append(el("dd", undefined, item.note ?? "none"));

  const note = el("textarea", "inp");
  note.setAttribute("placeholder", `A note for ${item.who} (optional)`);
  note.setAttribute("aria-label", `A note for ${item.who} (optional)`);

  const refusal = el("div", "note warn");

  async function decide(method: "approve" | "decline"): Promise<void> {
    refusal.replaceChildren();
    acts.working(true);

    try {
      const typed = (note as HTMLTextAreaElement).value.trim();

      await write(host, "leave.approve", method, {
        id: item.id,
        version: item.version,
        ...(typed === "" ? {} : { note: typed }),
      });

      done();
    } catch (error) {
      refusal.append(el("span", undefined,
        error instanceof WriteRefused ? error.message : UNKNOWN_OUTCOME));
      acts.working(false);
    }
  }

  // Approve is the confirm; Decline sits beside it and is not destructive —
  // it decides a request rather than removing anything, so §2's filled danger
  // control is not this.
  const acts = foot("Approve", "Approving…", () => { done(); });
  acts.onConfirm(() => { void decide("approve"); });

  const declineControl = control("btn", "Decline", () => { void decide("decline"); });
  acts.row.prepend(declineControl);

  panel.append(head, facts, note, refusal, acts.row);
  return panel;
}
