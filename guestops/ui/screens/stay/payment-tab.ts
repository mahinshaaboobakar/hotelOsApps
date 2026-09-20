/**
 * The Payment tab — the terms, and the folio that is a reported finding. Frame 7.
 */

import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

import type { Payment, TermRow } from "../../book";
import { el, fill } from "../../chrome/element";
import { tags } from "../../chrome/marks";
import { card } from "../../chrome/panel";
import { instant } from "../../chrome/when";

/**
 * Draw the tab.
 *
 * **The first card is v1 and buildable today** (GUEST-Q6): what the stay was
 * sold on. What is real here is the part that would otherwise be lost — a
 * guarantee with its codes, a deposit deadline as an **offset from the booking
 * date**, a cancellation deadline as an **offset from arrival** plus a drop
 * time, and an amount with a basis, a night count and a currency (R18). The
 * system this replaces kept two pre-formatted human strings and discarded the
 * structure.
 *
 * **The second card is not ruled and nothing is built behind it.** It is drawn
 * because payment information was asked for, and reported as a finding rather
 * than proposed as a plan.
 *
 * @param payment the terms, and the folio's refusals
 * @param property whose locale and zone the counts and the deadline are drawn in
 * @returns the tab's contents
 */
export function paymentTab(payment: Payment, property: PropertyEnvironment): readonly HTMLElement[] {
  const cols = el("div", "cols");
  cols.append(terms(payment, property), folio(payment));
  return [cols];
}

/** What the stay was sold on. */
function terms(payment: Payment, property: PropertyEnvironment): HTMLElement {
  const { root, body } = card("The terms");
  const heading = root.querySelector(".ch");

  // It carried an "in v1" pill — a release label for the developer, removed
  // under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.
  void heading;

  for (const term of payment.terms) {
    body.append(row(term, property));
  }

  if (payment.note !== null) {
    const note = el("div", "note");
    const [lead, ...rest] = payment.note.split(". ");

    note.append(
      el("b", undefined, `${lead ?? ""}.`),
      document.createTextNode(` ${rest.join(". ")}`),
    );

    body.append(note);
  }

  return root;
}

/** One term. */
function row(term: TermRow, property: PropertyEnvironment): HTMLElement {
  const element = el("div", term.big === true ? "fr big" : "fr");
  const value = el("div", "v");

  if (term.value !== "") {
    value.append(document.createTextNode(term.value));
  }

  if (term.strong !== undefined) {
    value.append(el("b", undefined, term.strong));
  }

  // A count, formatted for the property and worded here; after an amount it is
  // a qualifier (" · 4 nights"), alone it is the value itself.
  if (term.count !== undefined && term.count !== null) {
    const { n, one, other } = term.count;
    const said = `${formatNumber(n, property, "whole")} ${n === 1 ? one : other}`;
    value.append(term.strong !== undefined
      ? document.createTextNode(` · ${said}`)
      : el("b", undefined, said));
  }

  // The instant the row names — the cancellation deadline — after an arrow.
  if (term.at !== undefined && term.at !== null) {
    value.append(document.createTextNode(" → "), el("b", undefined, instant(term.at, property, "weekday-time")));
  }

  if (term.tail !== undefined) {
    value.append(document.createTextNode(term.tail));
  }

  fill(value, ...tags(term.tags));
  element.append(el("div", "k", term.label), value);
  return element;
}

/**
 * What the platform cannot yet tell you.
 *
 * **Drawn as refusals rather than as zeroes.** A balance of `₹ 0.00` and a
 * balance nobody can compute look identical on a screen and mean opposite
 * things — one says the guest owes nothing and the other says we do not know.
 * Every line here names what it would take instead.
 */
function folio(payment: Payment): HTMLElement {
  const root = el("div", "card ghost");
  const heading = el("div", "ch no");

  heading.append(
    document.createTextNode("The folio"),
    fill(el("div", "grow"), el("span", "pill bad", "not available")),
  );

  const body = el("div", "cb");

  for (const line of payment.folio) {
    const element = el("div", "fr");
    const value = el("div", "v");

    value.append(el("span", "lock no", line.because));
    element.append(el("div", "k", line.label), value);
    body.append(element);
  }

  const note = el("div", "note no");
  const [lead, ...rest] = payment.folioNote.split(". ");

  note.append(
    el("b", undefined, `${lead ?? ""}.`),
    document.createTextNode(` ${rest.join(". ")}`),
  );

  body.append(note);
  root.append(heading, body);
  return root;
}
