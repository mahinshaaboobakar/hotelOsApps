/**
 * Add a member — and the one person who cannot be added.
 *
 * # The refusal is shown, never filtered out
 *
 * The service refuses somebody who holds no posting in the team's department on
 * the day the membership starts. A picker that simply omitted them would leave
 * a supervisor scrolling for Joseph and concluding the screen is broken; drawn
 * and dashed with the reason beside him, the rule teaches itself once.
 *
 * # The day is the caller's, and it is checked against that day
 *
 * Next week's crew is formed against **next week's** postings. Checking today
 * would refuse the person who starts on Monday, which is exactly the crew a
 * supervisor sits down on Friday to build.
 */

import { formatDay, type HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el, fill } from "../../chrome/element";
import { write, WriteRefused } from "../../roster";
import type { Candidate, TeamDetail } from "../../roster/team";

/**
 * Build the dialog.
 *
 * @param host the bridge
 * @param onDate the day the membership starts, as the wire carries it
 * @param close called when it is dismissed
 * @param open the team the pane has open, from the loaded board - null when
 *   none is open, and then the dialog names no team rather than one it made up
 * @param done called after the member is added, so the roll is re-read
 * @returns the overlay
 */
export function addMember(
  host: HostApi,
  onDate: string,
  close: () => void,
  open: TeamDetail | null,
  done: () => void,
): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  const department = open?.team.departmentName ?? "its department";
  const candidates = open?.candidates ?? [];

  let chosen: Candidate | null = null;

  const head = el("div");
  head.append(
    el("div", "ht", "Add a member"),
    el("div", "hsub", `To ${open?.team.name ?? "this team"}, in ${department}.`));

  const refusal = el("div", "note warn");
  const acts = foot("Add to team", "Adding…", close);

  function waiting(): string | null {
    // The team is the pane's, so the only thing this dialog waits on is a
    // person - and it says so rather than greying out silently.
    if (open === null) return "No team is open";
    if (chosen === null) return "Choose somebody";
    return null;
  }

  async function submit(): Promise<void> {
    if (open === null || chosen === null) return;

    refusal.replaceChildren();
    acts.working(true);

    try {
      await write(host, "posting.assign", "addMember",
        { teamId: open.team.id, staffId: chosen.staffId, on: onDate });
      done();
    } catch (error) {
      // Section 9: a refusal keeps the overlay open, carrying the reason.
      refusal.append(el("span", undefined,
        error instanceof WriteRefused
          ? error.message
          : "That did not go through. Nothing was changed."));
      acts.working(false);

      if (!(error instanceof WriteRefused)) throw error;
    }
  }

  acts.onConfirm(() => { void submit(); });

  fill(
    dialog,
    head,
    from(onDate, host),
    who(candidates, (candidate) => { chosen = candidate; acts.waitingFor(waiting()); }),
    // The refusal explains itself only when there is one to explain.
    candidates.some((one) => one.refused !== null)
      ? why(candidates, department)
      : null,
    refusal,
    acts.row);

  acts.waitingFor(waiting());

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/**
 * The day the membership starts.
 *
 * **Rendered from the ISO day the read answered**, through the SDK against the
 * property's own locale. It used to be the literal `Thu 4 Sep 2026` - the
 * frame's date, on every property, forever - and it is also the date the write
 * sends, so the field and the payload cannot disagree.
 *
 * It is not editable yet, and that is deliberate rather than unfinished: the
 * day comes from the board the person is looking at, and a second date control
 * here would let a supervisor form next week's crew while reading this week's
 * candidates. Section 10's rule holds - it is drawn, because nothing behind it
 * accepts a different day.
 */
function from(onDate: string, host: HostApi): HTMLElement {
  const field = el("div", "fld");
  const input = el("div", "inp", formatDay(onDate, host.property, "day-month-year"));

  const note = el("div", "note");
  note.append(
    el("span", undefined, "The day the membership starts. Next week's crew is formed against "),
    el("b", undefined, "next week's"),
    el("span", undefined, " postings, not today's."));

  return fill(field, el("div", "fld-label", "From"), input, note);
}

/**
 * Everybody the picker offers, and the one it refuses.
 *
 * **Nothing is chosen when the dialog opens.** The first eligible candidate
 * used to arrive selected - `index === 0 ? " on" : ""` - which is the same
 * fault as a department picker that arrives reading *Front Office*: it supplies
 * an answer on the person's behalf, and the one it supplies is whoever the
 * service happened to list first.
 *
 * A refused row is a real `disabled` button rather than a div wearing
 * `aria-disabled`, so it cannot be clicked, cannot be tabbed into, and reports
 * itself honestly to a screen reader.
 */
function who(
  candidates: readonly Candidate[],
  pick: (candidate: Candidate) => void,
): HTMLElement {
  const field = el("div", "fld");
  const list = el("div", "tlist");

  for (const candidate of candidates) {
    const row = el("button", `tcand${candidate.refused === null ? "" : " no"}`);
    row.setAttribute("type", "button");

    if (candidate.refused !== null) {
      row.toggleAttribute("disabled", true);
    } else {
      row.addEventListener("click", () => {
        for (const other of Array.from(list.querySelectorAll(".tcand"))) {
          other.classList.remove("on");
        }
        row.classList.add("on");
        pick(candidate);
      });
    }

    const person = el("div");
    person.append(
      el("b", undefined, candidate.name),
      el("s", undefined, `${candidate.role} · ${candidate.department}`));

    fill(
      row,
      el("div", "av", initials(candidate.name)),
      person,
      el("div", "grow"),
      candidate.refused === null ? null : el("span", "pill bad", candidate.refused));

    list.append(row);
  }

  return fill(field, el("div", "fld-label", "Who"), list);
}

/**
 * Why a row is refused — the invariant, in a sentence, naming the person.
 *
 * The sentence is built from the candidates rather than written out, so a
 * screen showing two refusals cannot explain one of them.
 */
function why(candidates: readonly Candidate[], department: string): HTMLElement {
  const note = el("div", "note warn");
  const refused = candidates.filter((one) => one.refused !== null);
  const names = refused.map((one) => one.name.split(" ")[0]).join(" and ");

  note.append(el("span", undefined,
    `${names} holds no posting in ${department} on the day above. A team exists `
    + "to receive work in its own department, so a member who cannot be "
    + "assigned there is a row that lies."));

  return note;
}

/** Two letters from a name this module borrowed and does not keep. */
function initials(name: string): string {
  const parts = name.split(" ").filter((part) => part.length > 0);
  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? "" : "";

  return `${first}${last}`.toUpperCase();
}
