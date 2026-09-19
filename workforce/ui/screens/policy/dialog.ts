/**
 * Creating a shift — the page the whole catalogue rests on.
 *
 * # Every field here carries a rule the backend enforces
 *
 * The **short code is typed, never derived**: *Morning* and *Mid-shift* would
 * both want "M", and two shifts that look identical on a photocopy is the
 * mistake this prevents. The **kind** is expressed by the absence of times — an
 * off shift has none and counts no hours, which is what Week-off is. A **second
 * span** makes it a split shift. A span **ending before it starts crosses
 * midnight**. And the **colour is the shift's own attribute**, not a
 * consequence of its code — the code is what survives when colour is lost.
 *
 * # It defines the shift now
 *
 * `roster.configure · defineShift` takes all of it, and the owner met this
 * sheet dead on 0.3.3 — the name box would not take typing and Create did
 * nothing. Every box is a real control; the confirm waits, with the reason the
 * service would otherwise refuse for, until the shift is one it will accept.
 *
 * **"Available from" is not in the drawn frame.** The service requires the
 * first day a shift may be used, and defaulting it to today would be the form
 * answering for the person. It is asked for, and goes on the owner's list of
 * things to draw.
 */

import type { HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";

/**
 * The palette a shift is drawn in, and the colour name each one sends.
 *
 * `Wording.Tone` maps a stored colour name to a tone; these are names it maps
 * to each tone the screen draws, so a shift reads in the colour its swatch
 * showed. `Slate` maps to nothing, which is neutral — deliberately.
 */
const PALETTE = [
  { tone: "brand", name: "Cyan" },
  { tone: "ok", name: "Emerald" },
  { tone: "warn", name: "Amber" },
  { tone: "bad", name: "Rose" },
  { tone: "neutral", name: "Slate" },
] as const;

/** What the sheet holds, and what the write carries. */
interface Draft {
  name: string;
  code: string;
  working: boolean | null;
  startsAt: string;
  endsAt: string;
  secondStartsAt: string;
  secondEndsAt: string;
  colour: string | null;
  from: string;
}

type TimeField = "startsAt" | "endsAt" | "secondStartsAt" | "secondEndsAt";

/**
 * The sheet — a person composes a shift here (§9).
 *
 * @param host the bridge
 * @param close called when it is dismissed
 * @param done called after the shift is defined, so the catalogue is re-read
 * @returns the overlay
 */
export function newShift(host: HostApi, close: () => void, done: () => void): HTMLElement {
  const draft: Draft = {
    name: "", code: "", working: null, startsAt: "", endsAt: "",
    secondStartsAt: "", secondEndsAt: "", colour: null, from: "",
  };

  const refusal = el("div", "note warn");
  const acts = foot("Create shift", "Creating…", close);
  const spans = times((which, value) => { draft[which] = value; redraw(); });

  function redraw(): void {
    // The times belong to a working shift; an off shift has none (WF-Q17).
    for (const input of Array.from(spans.querySelectorAll("input"))) {
      input.disabled = draft.working !== true;
    }
    acts.waitingFor(waiting(draft));
  }

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "roster.configure", "defineShift", payload(draft));
        done();
      } catch (error) {
        refusal.append(el("span", undefined,
          error instanceof WriteRefused ? error.message : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  const sheet = overlay("sheet", {
    head: [
      el("div", "ht", "New shift"),
      el("div", "hsub", "It appears in the rota picker the moment it is saved"),
    ],
    body: [
      text("Name", "name", 80, "What people read. Any length.",
        (value) => { draft.name = value; redraw(); }),
      text("Short code", "code", 3,
        "Two or three characters — what fits a rota cell and survives a "
        + "black-and-white photocopy. You choose it, because Morning and Mid-shift "
        + "would both want “M”, and two shifts that look identical on paper is "
        + "the mistake this prevents.",
        (value) => { draft.code = value; redraw(); }),
      kind((working) => { draft.working = working; redraw(); }),
      spans,
      colour((name) => { draft.colour = name; redraw(); }),
      from((value) => { draft.from = value; redraw(); }),
      refusal,
    ],
    foot: [acts.row],
  }, close);

  redraw();
  return sheet;
}

/** What the confirm waits for, in the order a person fills the sheet. */
function waiting(draft: Draft): string | null {
  if (draft.name.trim() === "") return "Name the shift";
  if (draft.code.trim() === "") return "Give it a short code";
  if (draft.working === null) return "Choose working or off";

  if (draft.working) {
    if (draft.startsAt === "" || draft.endsAt === "") return "Set when it starts and ends";
    if (draft.startsAt === draft.endsAt) return "A shift cannot end when it starts";

    const second = [draft.secondStartsAt, draft.secondEndsAt];
    if (second.filter((one) => one !== "").length === 1) return "Set both ends of the second span";
    if (second[0] !== "" && second[0] === second[1]) return "The second span cannot end when it starts";
  }

  if (draft.colour === null) return "Choose a colour";
  if (draft.from === "") return "Choose the first day it can be used";
  return null;
}

/** The write's parameters — no times at all for an off shift. */
function payload(draft: Draft): Record<string, string> {
  const shift: Record<string, string> = {
    name: draft.name.trim(),
    code: draft.code.trim(),
    colour: draft.colour ?? "",
    from: draft.from,
  };

  if (draft.working === true) {
    shift["startsAt"] = draft.startsAt;
    shift["endsAt"] = draft.endsAt;
    if (draft.secondStartsAt !== "") {
      shift["secondStartsAt"] = draft.secondStartsAt;
      shift["secondEndsAt"] = draft.secondEndsAt;
    }
  }

  return shift;
}

/** A labelled text field, and the sentence that says why it is asked for. */
function text(
  label: string, name: string, length: number, note: string, set: (value: string) => void,
): HTMLElement {
  const row = el("div", "fld");
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "text";
  input.name = name;
  input.setAttribute("maxlength", String(length));
  input.addEventListener("input", () => { set(input.value); });

  row.append(el("div", "fld-label", label), input, el("div", "note", note));
  return row;
}

/** Working or off — and off is the absence of times, not a separate concept. */
function kind(set: (working: boolean) => void): HTMLElement {
  const row = el("div", "fld");
  const choices = el("div", "choices");

  // Neither chosen: the frame showed *Working* ticked, which is a choice nobody
  // made.
  const options = [["Working", true], ["Off", false]] as const;
  for (const [label, working] of options) {
    const choice = el("button", "choice", label);
    choice.setAttribute("type", "button");
    choice.setAttribute("aria-pressed", "false");
    choice.addEventListener("click", () => {
      for (const other of Array.from(choices.querySelectorAll(".choice"))) {
        other.classList.remove("on");
        other.setAttribute("aria-pressed", "false");
      }
      choice.classList.add("on");
      choice.setAttribute("aria-pressed", "true");
      set(working);
    });
    choices.append(choice);
  }

  row.append(
    el("div", "fld-label", "Kind"),
    choices,
    el("div", "note",
      "An off shift has no times and counts no hours — that is what Week-off is. "
      + "A rota marker, not a leave type: no request, no balance."),
  );

  return row;
}

/** Two spans, the second optional. */
function times(set: (which: TimeField, value: string) => void): HTMLElement {
  const row = el("div", "fld");
  // Four, said out loud. The row is two columns by default because two is
  // what every other dialog needs; a split shift is the exception and names
  // itself rather than making the default wrong for everyone else.
  const spans = el("div", "spans four");

  const fields: readonly [TimeField, string][] = [
    ["startsAt", "Starts"], ["endsAt", "Ends"],
    ["secondStartsAt", "Second span starts"], ["secondEndsAt", "Second span ends"],
  ];

  for (const [name, label] of fields) {
    const input = document.createElement("input");
    input.className = "inp";
    input.type = "time";
    input.name = name;
    input.setAttribute("aria-label", label);
    input.addEventListener("input", () => { set(name, input.value); });
    spans.append(input);
  }

  row.append(
    el("div", "fld-label", "Times"),
    spans,
    el("div", "note",
      "A second span makes it a split shift. A span ending before it starts "
      + "crosses midnight — Night is 23:00 → 07:00."),
  );

  return row;
}

/** The colour, chosen rather than derived. */
function colour(set: (name: string) => void): HTMLElement {
  const row = el("div", "fld");
  const swatches = el("div", "swatches");

  for (const { tone, name } of PALETTE) {
    // None chosen — the frame picked amber.
    const swatch = el("button", `sw ${tone}`);
    swatch.setAttribute("type", "button");
    swatch.setAttribute("aria-label", name);
    swatch.setAttribute("aria-pressed", "false");
    swatch.addEventListener("click", () => {
      for (const other of Array.from(swatches.querySelectorAll(".sw"))) {
        other.classList.remove("on");
        other.setAttribute("aria-pressed", "false");
      }
      swatch.classList.add("on");
      swatch.setAttribute("aria-pressed", "true");
      set(name);
    });
    swatches.append(swatch);
  }

  row.append(
    el("div", "fld-label", "Colour"),
    swatches,
    el("div", "note",
      "How the week reads at a glance. Colour is the shift's own attribute, not "
      + "a consequence of its code — and the short code is what survives when "
      + "colour is lost."),
  );

  return row;
}

/** The first day the shift may be used — required by the service, not drawn. */
function from(set: (value: string) => void): HTMLElement {
  const row = el("div", "fld");
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "date";
  input.name = "from";
  input.addEventListener("input", () => { set(input.value); });

  row.append(
    el("div", "fld-label", "Available from"),
    input,
    el("div", "note", "The first day it can be put on the rota."),
  );

  return row;
}
