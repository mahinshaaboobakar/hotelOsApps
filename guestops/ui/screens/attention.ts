/**
 * Attention — the two things a person, and only a person, finishes.
 *
 * Gold frames 12–13. A disagreement is where the desk and the PMS say different
 * things about one stay; a candidate is where a stay this property created may
 * be the stay the PMS has just sent. Neither is resolved automatically, and the
 * reason is the same in both cases: the wrong answer is silent and expensive.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, standIn } from "../chrome";
import { load, recordedAttention, type Attention } from "../book";

/**
 * Render the attention list.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 */
export async function attention(host: HostApi, into: HTMLElement): Promise<void> {
  const loaded = await load(host, "reservation.read", "attention", recordedAttention);

  const head = el("div", "head");
  const title = el("div");
  title.append(
    el("div", "ht", "Attention"),
    el("div", "hsub", `${loaded.value.length} to resolve · nothing here resolves itself`),
  );
  head.append(title);

  const body = el("div", "body");
  if (!loaded.live) body.append(standIn(loaded.because));

  if (loaded.value.length === 0) {
    const clear = el("div", "card");
    clear.append(el("p", undefined, "Nothing needs a person."));
    body.append(clear);
  }

  for (const item of loaded.value) {
    body.append(item.kind === "disagreement" ? disagreement(item) : candidate(item));
  }

  into.replaceChildren(head, body);
}

/**
 * One disagreement.
 *
 * **Both values stay on the row, whichever side wins.** A decision that
 * discarded the losing value could not explain itself later — and the property
 * is the party that has to explain it.
 *
 * Clearing takes the stay's own write permission, never one of its own:
 * author-only fails across shifts and supervisor-only escalates a routine
 * reconciliation, and GUEST-Q3 refused both by name.
 */
function disagreement(item: Attention): HTMLElement {
  const card = el("div", "card");
  card.append(el("h3", undefined, `${item.stay} — the desk and Opera disagree`));
  card.append(el("p", undefined, item.detail));

  const pair = el("div", "pair");
  pair.append(side("Ours", item.ours), side("Opera", item.theirs));
  card.append(pair);

  const acts = el("div", "acts");
  acts.append(el("button", "btn go", "Keep ours"), el("button", "btn", "Take Opera's"));
  card.append(acts);

  return card;
}

/**
 * One candidate link.
 *
 * **Same room and overlapping dates is the whole test.** Names rank the list and
 * never link it — the system this replaces matched on surname and arrival date,
 * and a wrong match silently merges two guests' histories, which is worse than
 * a duplicate.
 *
 * Rejecting is a real answer: two stays honestly, and a double-booked room that
 * is then the truth rather than an artefact.
 */
function candidate(item: Attention): HTMLElement {
  const card = el("div", "card");
  card.append(el("h3", undefined, `${item.stay} — is this the same stay?`));
  card.append(el("p", undefined, item.detail));

  const pair = el("div", "pair");
  pair.append(side("Here", item.ours), side("Opera", item.theirs));
  card.append(pair);

  const acts = el("div", "acts");
  acts.append(
    el("button", "btn go", "Same stay"),
    el("button", "btn", "Different — keep both"),
  );
  card.append(acts);

  return card;
}

function side(name: string, value: string): HTMLElement {
  const box = el("div");
  box.append(el("span", undefined, name), el("div", undefined, value));
  return box;
}
