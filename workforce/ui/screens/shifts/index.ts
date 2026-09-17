/**
 * Shifts — the property's catalogue, on a screen of its own.
 *
 * # Why this is not the Policy screen
 *
 * Frame 8 shows the catalogue as one section among four that a property
 * configures. Frame 9 shows **Shifts**: its own screen, its own header
 * (*"6 shifts · shared by every department"*), and a fuller table carrying how
 * many assignments reference each entry. They are two views of one catalogue,
 * and the second exists because adding a shift is the operation the whole rota
 * rests on — it earns a screen rather than a row in a settings page.
 *
 * The rail keeps **Policy** lit, because Shifts is reached from it and belongs
 * to it — the way back is the way in.
 */

import { type HostApi, load } from "@hotelos/sdk";

import { span } from "../../chrome/clock";
import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { codeChip, colourDot } from "../../chrome/code";
import { failureScreen } from "../../chrome/failure";
import { newShift } from "../policy/dialog";
import { type CatalogueRow, type Policy } from "../../roster/policy";

/** One table cell holding an element rather than text. */
function cell(child: HTMLElement, className?: string): HTMLElement {
  const box = el("div", className);
  box.append(child);
  return box;
}

/** The published tone a property's colour name maps onto. */
function swatch(colour: string): string {
  if (colour === "Cyan" || colour === "Indigo" || colour === "Violet") return "brand";
  if (colour === "Emerald") return "ok";
  if (colour === "Amber") return "warn";
  return "neutral";
}

const COLUMNS = "1.5fr 110px 160px 1fr 150px";

/** Draw the screen. */
export async function shifts(
  host: HostApi,
  main: HTMLElement,
  dialog = false,
  open: () => void = () => {},
  close: () => void = () => {},
): Promise<void> {
  const got = await load<Policy>(host, ROSTER_READ, "policy");

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Policy", got.failure, { the: "the shift catalogue" },
      () => void shifts(host, main, dialog, open, close));
    return;
  }

  const catalogue = got.value.catalogue;

  const body = el("div", "body");
  body.append(table(catalogue, host), note());

  main.replaceChildren(header(catalogue, open), body);

  if (dialog) main.append(newShift(close));
}

function header(catalogue: readonly CatalogueRow[], open: () => void): HTMLElement {
  const head = el("div", "title");
  const title = el("div");

  title.append(
    el("div", "ht", "Shifts"),
    el("div", "hsub",
      `Kochi Beach Resort · ${catalogue.length} shifts · shared by every department`),
  );

  const add = el("div", "btn pri", "＋ New shift");
  add.addEventListener("click", open);

  head.append(title, el("div", "grow"), add);
  return head;
}

function table(rows: readonly CatalogueRow[], host: HostApi): HTMLElement {
  const list = el("div", "rows");

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = COLUMNS;
  for (const label of ["Shift", "Short code", "Times", "Colour", "In use"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = COLUMNS;

    const name = el("div");
    name.append(el("b", undefined, row.name));

    // An off entry says so beside its name, because "no times" is the whole of
    // what makes it one — WF-Q12.
    if (row.kind === "off") {
      name.append(el("span", "pill neu", "off"));
    }

    item.append(
      name,
      cell(codeChip(row.code, swatch(row.colour))),
      el("div", "quiet", [span(row.hours, host.property), span(row.second, host.property)]
        .filter((one) => one !== null).join(", ") || "—"),
      cell(colourDot(row.colour, swatch(row.colour)), "quiet"),
      el("div", "quiet", row.inUse),
    );

    list.append(item);
  }

  return list;
}

/** One catalogue, and nothing preset beyond the starting template. */
function note(): HTMLElement {
  const panel = el("div", "panel");
  const text = el("div", "note");

  text.append(
    el("span", undefined,
      "One catalogue, used by every department at this property. Nothing is preset "
      + "beyond the starting template — "),
    el("b", undefined, "the property invents the shifts it actually runs."),
  );

  panel.append(text);
  return panel;
}
