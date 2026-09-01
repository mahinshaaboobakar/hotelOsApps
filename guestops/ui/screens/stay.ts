/**
 * One stay — the page the front desk works from.
 *
 * Gold frames 5–8. What this screen owns and what it only *shows* is the whole
 * design: the stay, its terms, its registration, its requests and notes are
 * GuestOps's; the jobs raised from it and the servicing across it are read
 * through the Context Service and marked as another application's.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, shield, standIn } from "../chrome";
import { load, type Stay } from "../book";

/**
 * Render one stay.
 *
 * @param host the bridge — the only route out of this realm
 * @param stay the stay a person picked
 * @param into the element this screen owns
 * @param back what to do when they leave
 */
export async function stay(host: HostApi, chosen: Stay, into: HTMLElement,
  back: () => void): Promise<void> {
  const loaded = await load(host, "reservation.read", "stay", chosen);
  const it = loaded.value;

  const head = el("div", "head");
  const title = el("div");
  title.append(
    el("div", "ht", it.guest),
    el("div", "hsub", `${it.room ?? "no room yet"} · ${it.roomType} · ${it.lifecycle}`),
  );

  const leave = el("button", "btn", "← Today");
  leave.addEventListener("click", back);

  const grow = el("div");
  grow.style.flex = "1";
  head.append(title, grow, leave);

  const body = el("div", "body");
  if (!loaded.live) body.append(standIn(loaded.because));

  body.append(dates(it), registration(), requests(), neighbours());
  into.replaceChildren(head, body);
}

/**
 * The two moments, and what is honestly missing.
 *
 * **Expected and actual are different facts** (R12–R14), so a stay that has not
 * arrived shows an expectation and never a time that looks observed. An absence
 * is drawn as an absence — R25: neither dropped nor invented.
 */
function dates(it: Stay): HTMLElement {
  const card = el("div", "card");
  card.append(el("h3", undefined, "The stay"));

  const pair = el("div", "pair");
  pair.append(field("Arrival", it.arrival), field("Departure", it.departure));
  card.append(pair);

  return card;
}

function field(name: string, value: string | null): HTMLElement {
  const box = el("div");
  box.append(el("span", undefined, name));
  box.append(value === null ? shield("missing", "not recorded") : el("div", undefined, value));
  return box;
}

/**
 * The registration card, and the filing obligation beside it.
 *
 * **Neither gates anything.** An incomplete card and an outstanding filing both
 * let the guest check in — so this card prompts and never blocks, which is the
 * screen's half of S19b's rule.
 */
function registration(): HTMLElement {
  const card = el("div", "card");
  card.append(el("h3", undefined, "Registration"));
  card.append(
    el(
      "p",
      undefined,
      "The card is the property's, and which fields it requires is the property's " +
        "configuration — set separately for home-country guests and guests from outside. " +
        "Nothing here is a legal minimum the product decided.",
    ),
  );

  const missing = el("div", "acts");
  missing.append(shield("missing", "id_type"), shield("missing", "id_number"));
  card.append(missing);

  const acts = el("div", "acts");
  acts.append(el("button", "btn go", "Capture card"), el("button", "btn", "Record a filing"));
  card.append(acts);

  return card;
}

/**
 * Requests and notes.
 *
 * **A request handed to Jobs shows no job status**, only whether a job exists
 * yet. What the job is *doing* is Jobs' and is a Context question — and an
 * uninstalled Jobs looks exactly like a request that has not become work, which
 * is APPS-Q2 drawn rather than described.
 */
function requests(): HTMLElement {
  const card = el("div", "card");
  card.append(el("h3", undefined, "Requests and notes"));
  card.append(
    el("p", undefined, "An extra pillow — handed to Jobs, no job yet."),
    el("p", undefined, "A late checkout — answered at the desk; not every request becomes work."),
  );

  const acts = el("div", "acts");
  acts.append(el("button", "btn", "Log a request"), el("button", "btn", "Add a note"));
  card.append(acts);

  return card;
}

/**
 * What other applications know about this stay.
 *
 * **Drawn, not built** — ratified with GUEST-Q6. The jobs raised from a stay and
 * the servicing across it belong to Jobs and Room Care; their Context read-views
 * are their rounds', and reading those schemas directly was refused as the one
 * rule modularity rests on. So this panel says what it would show and does not
 * pretend to have it.
 */
function neighbours(): HTMLElement {
  const card = el("div", "card");
  card.append(el("h3", undefined, "Across this stay"));
  card.append(
    el(
      "p",
      undefined,
      "Jobs raised from this stay, and the servicing across its nights, are read " +
        "through the Context Service when those applications ship their views. " +
        "GuestOps does not read another application's tables.",
    ),
  );

  const marks = el("div", "acts");
  marks.append(shield("missing", "Jobs — not installed"), shield("missing", "Room Care — not installed"));
  card.append(marks);

  return card;
}
